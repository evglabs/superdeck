using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SuperDeck.Core.Models;
using SuperDeck.Core.Settings;

namespace SuperDeck.Server.Services;

public class JwtService
{
    private readonly JwtSettings _settings;
    private readonly SymmetricSecurityKey _securityKey;

    public JwtService(GameSettings gameSettings)
    {
        _settings = gameSettings.Auth.Jwt;

        if (string.IsNullOrEmpty(_settings.Secret) || _settings.Secret.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT Secret must be at least 32 characters. Configure 'GameSettings:Auth:Jwt:Secret' in appsettings.json");
        }

        _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
    }

    public string GenerateToken(Player player)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, player.Id),
            new(JwtRegisteredClaimNames.UniqueName, player.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("playerId", player.Id)
        };

        var credentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _securityKey,
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            return tokenHandler.ValidateToken(token, validationParameters, out _);
        }
        catch
        {
            return null;
        }
    }

    public string? GetPlayerIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        return principal?.FindFirst("playerId")?.Value;
    }

    public TokenValidationParameters GetValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _securityKey,
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }
}
