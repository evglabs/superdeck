using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SuperDeck.Core.Models;
using SuperDeck.Core.Models.Enums;

namespace SuperDeck.Client.Networking;

public class ApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _authToken;

    public string? CurrentPlayerId { get; private set; }
    public string? CurrentUsername { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(_authToken);

    public ApiClient(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    private void SetAuthToken(string token)
    {
        _authToken = token;
        _httpClient.DefaultRequestHeaders.Remove("Authorization");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }

    // ========================================
    // Auth Operations
    // ========================================

    public async Task<(bool success, string? error)> RegisterAsync(string username, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register",
                new { username, password }, _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(_jsonOptions);
                return (false, error?.Error ?? "Registration failed");
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
            if (result != null)
            {
                SetAuthToken(result.Token);
                CurrentPlayerId = result.PlayerId;
                CurrentUsername = result.Username;
            }
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool success, string? error)> LoginAsync(string username, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login",
                new { username, password }, _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(_jsonOptions);
                return (false, error?.Error ?? "Login failed");
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
            if (result != null)
            {
                SetAuthToken(result.Token);
                CurrentPlayerId = result.PlayerId;
                CurrentUsername = result.Username;
            }
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task LogoutAsync()
    {
        if (_authToken != null)
        {
            try
            {
                await _httpClient.PostAsync("/api/auth/logout", null);
            }
            catch { }
        }
        _authToken = null;
        CurrentPlayerId = null;
        CurrentUsername = null;
        _httpClient.DefaultRequestHeaders.Remove("Authorization");
    }

    public async Task<PlayerInfo?> GetCurrentPlayerAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<PlayerInfo>("/api/auth/me", _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    // ========================================
    // Character Operations
    // ========================================

    public async Task<IEnumerable<Character>> GetCharactersAsync(string? playerId = null)
    {
        var url = playerId != null ? $"/api/characters?playerId={playerId}" : "/api/characters";
        var response = await _httpClient.GetFromJsonAsync<IEnumerable<Character>>(url, _jsonOptions);
        return response ?? Enumerable.Empty<Character>();
    }

    public async Task<Character> CreateCharacterAsync(string name, Suit suitChoice, string? playerId = null)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/characters",
            new { name, suitChoice, playerId }, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Character>(_jsonOptions)
            ?? throw new InvalidOperationException("Failed to create character");
    }

    public async Task<Character?> GetCharacterAsync(string id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<Character>($"/api/characters/{id}", _jsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<Character> UpdateCharacterStatsAsync(string id, int attack, int defense, int speed, int bonusHP = 0)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/characters/{id}/stats",
            new { attack, defense, speed, bonusHP }, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Character>(_jsonOptions)
            ?? throw new InvalidOperationException("Failed to update character");
    }

    public async Task DeleteCharacterAsync(string id)
    {
        var response = await _httpClient.DeleteAsync($"/api/characters/{id}");
        response.EnsureSuccessStatusCode();
    }

    // ========================================
    // Card Operations
    // ========================================

    public async Task<IEnumerable<Card>> GetCardsAsync()
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<Card>>("/api/cards", _jsonOptions)
            ?? Enumerable.Empty<Card>();
    }

    public async Task<Card?> GetCardAsync(string id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<Card>($"/api/cards/{id}", _jsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<IEnumerable<Card>> GetStarterPackCardsAsync(Suit suit)
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<Card>>($"/api/cards/starterpack/{suit}", _jsonOptions)
            ?? Enumerable.Empty<Card>();
    }

    public async Task<Character> AddCardsToDeckAsync(string characterId, List<string> cardIds)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/characters/{characterId}/cards",
            new { cardIds }, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Character>(_jsonOptions)
            ?? throw new InvalidOperationException("Failed to add cards to deck");
    }

    public async Task<Character?> RemoveCardsFromDeckAsync(string characterId, List<string> cardIds)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/characters/{characterId}/cards")
        {
            Content = JsonContent.Create(new { cardIds }, options: _jsonOptions)
        };
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<Character>(_jsonOptions);
    }

    // ========================================
    // Battle Operations
    // ========================================

    public async Task<StartBattleResult> StartBattleAsync(string characterId)
    {
        return await StartBattleAsync(characterId, autoBattle: false);
    }

    public async Task<StartBattleResult> StartBattleAsync(
        string characterId,
        bool autoBattle,
        string autoBattleMode = "Watch",
        string? aiProfileId = null)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/battle/start",
            new { characterId, autoBattle, autoBattleMode, aiProfileId }, _jsonOptions);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        try
        {
            return JsonSerializer.Deserialize<StartBattleResult>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to start battle");
        }
        catch (JsonException ex)
        {
            // Log the problematic JSON for debugging
            var debugPath = Path.Combine(Environment.CurrentDirectory, "battle_debug.json");
            try
            {
                File.WriteAllText(debugPath, json);
            }
            catch
            {
                debugPath = "(failed to write debug file)";
            }

            // Find the problematic area around position 8109
            var errorPos = 8109;
            var contextStart = Math.Max(0, errorPos - 100);
            var contextEnd = Math.Min(json.Length, errorPos + 100);
            var context = json.Length > contextStart ? json[contextStart..contextEnd] : json;

            throw new InvalidOperationException(
                $"JSON parse error: {ex.Message}\n" +
                $"Debug file: {debugPath}\n" +
                $"JSON around error position ({errorPos}): ...{context}...");
        }
    }

    public async Task<ActionResult> SubmitActionAsync(string battleId, PlayerAction action)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/battle/{battleId}/action",
            action, _jsonOptions);
        return await response.Content.ReadFromJsonAsync<ActionResult>(_jsonOptions)
            ?? new ActionResult { Valid = false, Message = "Failed to process action" };
    }

    public async Task<BattleState?> GetBattleStateAsync(string battleId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<BattleState>($"/api/battle/{battleId}/state", _jsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<BattleState> ForfeitBattleAsync(string battleId)
    {
        var response = await _httpClient.PostAsync($"/api/battle/{battleId}/forfeit", null);
        return await response.Content.ReadFromJsonAsync<BattleState>(_jsonOptions) ?? new();
    }

    public async Task<BattleResult?> FinalizeBattleAsync(string battleId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/battle/{battleId}/finalize", null);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<BattleResult>(_jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<BattleState?> ToggleAutoBattleAsync(string battleId, bool enabled, string? aiProfileId = null)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/battle/{battleId}/auto-battle",
            new { enabled, aiProfileId }, _jsonOptions);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<BattleState>(_jsonOptions);
    }

    public async Task<ActionResult> AutoQueueCardsAsync(string battleId)
    {
        var response = await _httpClient.PostAsync($"/api/battle/{battleId}/auto-queue", null);
        return await response.Content.ReadFromJsonAsync<ActionResult>(_jsonOptions)
            ?? new ActionResult { Valid = false, Message = "Failed to auto-queue cards" };
    }

    public async Task<InstantBattleResult?> RunInstantBattleAsync(string characterId, string? aiProfileId = null)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/battle/instant",
                new { characterId, autoBattle = true, autoBattleMode = "Instant", aiProfileId }, _jsonOptions);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<InstantBattleResult>(_jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    // ========================================
    // Booster Pack Operations
    // ========================================

    public async Task<BoosterPack?> GeneratePackAsync(string characterId)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/packs/generate",
            new { characterId }, _jsonOptions);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<BoosterPack>(_jsonOptions);
    }

    // ========================================
    // Server Info
    // ========================================

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ServerInfo?> GetServerInfoAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ServerInfo>("/api/info", _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}

// Response DTOs
public class StartBattleResult
{
    public string BattleId { get; set; } = string.Empty;
    public BattleState BattleState { get; set; } = new();
}

public class ActionResult
{
    public bool Valid { get; set; }
    public string? Message { get; set; }
    public BattleState BattleState { get; set; } = new();
}

public class BattleResult
{
    public string BattleId { get; set; } = string.Empty;
    public string WinnerId { get; set; } = string.Empty;
    public bool PlayerWon { get; set; }
    public int XPGained { get; set; }
    public int MMRChange { get; set; }
    public int LevelsGained { get; set; }
    public int NewLevel { get; set; }
    public List<string> BattleLog { get; set; } = new();
}

public class BoosterPack
{
    public string Id { get; set; } = string.Empty;
    public string CharacterId { get; set; } = string.Empty;
    public List<Card> Cards { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

// Auth DTOs
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

public class ErrorResponse
{
    public string? Error { get; set; }
}

public class PlayerInfo
{
    public string PlayerId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int TotalWins { get; set; }
    public int TotalLosses { get; set; }
    public int HighestMMR { get; set; }
    public int TotalBattles { get; set; }
}

public class ServerInfo
{
    public string Version { get; set; } = string.Empty;
    public int CardCount { get; set; }
    public ServerSettings Settings { get; set; } = new();
}

public class ServerSettings
{
    public int BaseHP { get; set; } = 100;
    public int HpPerLevel { get; set; } = 10;
    public int MaxLevel { get; set; } = 10;
    public int BaseQueueSlots { get; set; } = 3;
    public int StatPointsPerLevel { get; set; } = 1;
    public int HpPerStatPoint { get; set; } = 5;
    public int AutoBattleWatchDelayMs { get; set; } = 500;
}

public class InstantBattleResult
{
    public string BattleId { get; set; } = string.Empty;
    public BattleResult Result { get; set; } = new();
    public List<string> BattleLog { get; set; } = new();
    public int TotalRounds { get; set; }
}
