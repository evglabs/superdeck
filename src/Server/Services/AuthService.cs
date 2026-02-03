using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using SuperDeck.Core.Data.Repositories;
using SuperDeck.Core.Models;
using SuperDeck.Core.Settings;

namespace SuperDeck.Server.Services;

public class AuthService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly GameSettings _settings;
    private readonly JwtService? _jwtService;
    private readonly ConcurrentDictionary<string, AuthSession> _sessions = new();

    public AuthService(IPlayerRepository playerRepository, GameSettings settings, JwtService? jwtService = null)
    {
        _playerRepository = playerRepository;
        _settings = settings;
        _jwtService = settings.Auth.UseJwt ? jwtService : null;
    }

    public async Task<(bool success, string? token, string? error, Player? player)> RegisterAsync(
        string username, string password)
    {
        // Validate username
        if (string.IsNullOrWhiteSpace(username) ||
            username.Length < _settings.Auth.UsernameMinLength ||
            username.Length > _settings.Auth.UsernameMaxLength)
        {
            return (false, null, $"Username must be {_settings.Auth.UsernameMinLength}-{_settings.Auth.UsernameMaxLength} characters", null);
        }

        if (!username.All(c => char.IsLetterOrDigit(c) || c == '_'))
        {
            return (false, null, "Username can only contain letters, numbers, and underscores", null);
        }

        // Validate password
        if (string.IsNullOrWhiteSpace(password) || password.Length < _settings.Auth.PasswordMinLength)
        {
            return (false, null, $"Password must be at least {_settings.Auth.PasswordMinLength} characters", null);
        }

        // Check if username exists
        if (await _playerRepository.UsernameExistsAsync(username))
        {
            return (false, null, "Username already taken", null);
        }

        // Create player
        var salt = GenerateSalt();
        var passwordHash = HashPassword(password, salt);

        var player = new Player
        {
            Username = username,
            PasswordHash = passwordHash,
            Salt = salt
        };

        await _playerRepository.CreateAsync(player);

        // Create token (JWT or session-based)
        string token;
        if (_jwtService != null)
        {
            token = _jwtService.GenerateToken(player);
        }
        else
        {
            token = GenerateSessionToken();
            _sessions[token] = new AuthSession
            {
                PlayerId = player.Id,
                Username = player.Username,
                CreatedAt = DateTime.UtcNow
            };
        }

        return (true, token, null, player);
    }

    public async Task<(bool success, string? token, string? error, Player? player)> LoginAsync(
        string username, string password)
    {
        var player = await _playerRepository.GetByUsernameAsync(username);
        if (player == null)
        {
            return (false, null, "Invalid username or password", null);
        }

        var passwordHash = HashPassword(password, player.Salt);
        if (passwordHash != player.PasswordHash)
        {
            return (false, null, "Invalid username or password", null);
        }

        // Update last login
        player.LastLoginAt = DateTime.UtcNow;
        await _playerRepository.UpdateAsync(player);

        // Create token (JWT or session-based)
        string token;
        if (_jwtService != null)
        {
            token = _jwtService.GenerateToken(player);
        }
        else
        {
            token = GenerateSessionToken();
            _sessions[token] = new AuthSession
            {
                PlayerId = player.Id,
                Username = player.Username,
                CreatedAt = DateTime.UtcNow
            };
        }

        return (true, token, null, player);
    }

    public bool Logout(string token)
    {
        return _sessions.TryRemove(token, out _);
    }

    public AuthSession? ValidateToken(string token)
    {
        // JWT validation
        if (_jwtService != null)
        {
            var playerId = _jwtService.GetPlayerIdFromToken(token);
            if (playerId != null)
            {
                return new AuthSession
                {
                    PlayerId = playerId,
                    Username = string.Empty, // JWT doesn't store this in session
                    CreatedAt = DateTime.UtcNow
                };
            }
            return null;
        }

        // Session-based validation
        if (_sessions.TryGetValue(token, out var session))
        {
            // Check if session is expired
            if (DateTime.UtcNow - session.CreatedAt > TimeSpan.FromHours(_settings.Auth.SessionTimeoutHours))
            {
                _sessions.TryRemove(token, out _);
                return null;
            }
            return session;
        }
        return null;
    }

    public async Task<Player?> GetPlayerFromToken(string token)
    {
        var session = ValidateToken(token);
        if (session == null) return null;
        return await _playerRepository.GetByIdAsync(session.PlayerId);
    }

    private string GenerateSalt()
    {
        var saltBytes = new byte[_settings.Auth.SaltSizeBytes];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    private static string HashPassword(string password, string salt)
    {
        using var sha256 = SHA256.Create();
        var combined = Encoding.UTF8.GetBytes(password + salt);
        var hash = sha256.ComputeHash(combined);
        return Convert.ToBase64String(hash);
    }

    private string GenerateSessionToken()
    {
        var tokenBytes = new byte[_settings.Auth.TokenSizeBytes];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes);
    }

    public bool IsUsingJwt => _jwtService != null;
}

public class AuthSession
{
    public string PlayerId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
