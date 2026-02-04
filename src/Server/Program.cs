using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using SuperDeck.Core.Data.Repositories;
using SuperDeck.Core.Models;
using SuperDeck.Core.Models.Enums;
using SuperDeck.Core.Scripting;
using SuperDeck.Core.Settings;
using SuperDeck.Server.Data;
using SuperDeck.Server.Data.Repositories;
using SuperDeck.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Explicit URL configuration from environment variable (more reliable than automatic detection)
var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (!string.IsNullOrEmpty(urls))
{
    builder.WebHost.UseUrls(urls.Split(';'));
    Console.WriteLine($"Server configured to listen on: {urls}");
}
else
{
    Console.WriteLine("No ASPNETCORE_URLS set, using default: http://localhost:5000");
    builder.WebHost.UseUrls("http://localhost:5000");
}

// Game Settings
builder.Services.Configure<GameSettings>(builder.Configuration.GetSection("GameSettings"));
builder.Services.AddSingleton(sp =>
{
    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GameSettings>>().Value;

    // Load suit weights from separate file
    var suitWeightsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "suitweights.json");
    if (!File.Exists(suitWeightsPath))
    {
        suitWeightsPath = "suitweights.json";
    }
    if (File.Exists(suitWeightsPath))
    {
        var suitWeightsJson = File.ReadAllText(suitWeightsPath);
        settings.SuitWeights = JsonSerializer.Deserialize<Dictionary<string, double>>(suitWeightsJson) ?? new();
    }

    return settings;
});

// JSON options
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// Core services
builder.Services.AddSingleton<ScriptCompiler>();
builder.Services.AddSingleton<HookRegistry>();
builder.Services.AddSingleton<HookExecutor>();

// Repositories (SQLite or MariaDB based on config)
builder.Services.AddRepositories(builder.Configuration);

// Services
builder.Services.AddSingleton<CardService>();
builder.Services.AddSingleton<CharacterService>();
builder.Services.AddSingleton<AIBehaviorService>();
builder.Services.AddSingleton<BattleService>();
builder.Services.AddSingleton<BoosterPackService>();

// JWT Service (conditionally registered)
var gameSettings = builder.Configuration.GetSection("GameSettings").Get<GameSettings>() ?? new GameSettings();
if (gameSettings.Auth.UseJwt && !string.IsNullOrEmpty(gameSettings.Auth.Jwt.Secret))
{
    builder.Services.AddSingleton<JwtService>();

    // Configure JWT authentication
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(gameSettings.Auth.Jwt.Secret)),
                ValidateIssuer = true,
                ValidIssuer = gameSettings.Auth.Jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = gameSettings.Auth.Jwt.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();
}

builder.Services.AddSingleton<AuthService>();

// Rate Limiting (if enabled)
if (gameSettings.RateLimit.Enabled)
{
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Global rate limiter
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(clientId, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = gameSettings.RateLimit.GlobalPermitLimit,
                Window = TimeSpan.FromSeconds(gameSettings.RateLimit.GlobalWindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
        });

        // Auth endpoints policy (stricter)
        options.AddPolicy("auth", context =>
        {
            var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(clientId, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = gameSettings.RateLimit.Auth.PermitLimit,
                Window = TimeSpan.FromSeconds(gameSettings.RateLimit.Auth.WindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
        });

        // Battle endpoints policy (token bucket for burst handling)
        options.AddPolicy("battle", context =>
        {
            var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetTokenBucketLimiter(clientId, _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = gameSettings.RateLimit.Battle.TokenLimit,
                TokensPerPeriod = gameSettings.RateLimit.Battle.TokensPerPeriod,
                ReplenishmentPeriod = TimeSpan.FromSeconds(gameSettings.RateLimit.Battle.ReplenishmentPeriodSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
        });
    });
}

// CORS for client
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// Initialize card library
Console.WriteLine("Loading card library...");
var cardService = app.Services.GetRequiredService<CardService>();
await cardService.LoadCardsAsync();
Console.WriteLine("Card library loaded successfully.");

app.UseCors();

// Rate Limiting middleware (if enabled)
if (gameSettings.RateLimit.Enabled)
{
    app.UseRateLimiter();
}

// JWT Authentication middleware (if enabled)
if (gameSettings.Auth.UseJwt)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// ========================================
// Character Endpoints
// ========================================

app.MapGet("/api/characters", async (ICharacterRepository repo, string? playerId) =>
{
    var characters = await repo.GetByPlayerIdAsync(playerId);
    return Results.Ok(characters);
})
.WithName("GetCharacters")
.WithTags("Characters");

app.MapPost("/api/characters", async (CharacterService characterService, CreateCharacterRequest request) =>
{
    try
    {
        var character = await characterService.CreateCharacterAsync(request.Name, request.SuitChoice, request.PlayerId);
        return Results.Created($"/api/characters/{character.Id}", character);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("CreateCharacter")
.WithTags("Characters");

app.MapGet("/api/characters/{id}", async (ICharacterRepository repo, string id) =>
{
    var character = await repo.GetByIdAsync(id);
    return character is not null ? Results.Ok(character) : Results.NotFound();
})
.WithName("GetCharacter")
.WithTags("Characters");

app.MapPut("/api/characters/{id}/stats", async (CharacterService characterService, string id, UpdateStatsRequest request) =>
{
    try
    {
        var character = await characterService.AllocateStatsAsync(id, request.Attack, request.Defense, request.Speed, request.BonusHP);
        return character is not null ? Results.Ok(character) : Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("UpdateCharacterStats")
.WithTags("Characters");

app.MapDelete("/api/characters/{id}", async (ICharacterRepository repo, string id) =>
{
    var deleted = await repo.DeleteAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
})
.WithName("DeleteCharacter")
.WithTags("Characters");

app.MapPost("/api/characters/{id}/cards", async (CharacterService characterService, string id, AddCardsRequest request) =>
{
    try
    {
        var character = await characterService.AddCardsToDeckAsync(id, request.CardIds);
        return character is not null ? Results.Ok(character) : Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("AddCardsToDeck")
.WithTags("Characters");

app.MapDelete("/api/characters/{id}/cards", async (CharacterService characterService, string id, [Microsoft.AspNetCore.Mvc.FromBody] RemoveCardsRequest request) =>
{
    try
    {
        var character = await characterService.RemoveCardsFromDeckAsync(id, request.CardIds);
        return character is not null ? Results.Ok(character) : Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("RemoveCardsFromDeck")
.WithTags("Characters");

// ========================================
// Card Endpoints
// ========================================

app.MapGet("/api/cards", (CardService cardSvc) =>
{
    return Results.Ok(cardSvc.GetAllCards());
})
.WithName("GetCards")
.WithTags("Cards");

app.MapGet("/api/cards/{id}", (CardService cardSvc, string id) =>
{
    var card = cardSvc.GetCard(id);
    return card is not null ? Results.Ok(card) : Results.NotFound();
})
.WithName("GetCard")
.WithTags("Cards");

app.MapGet("/api/cards/suit/{suit}", (CardService cardSvc, Suit suit) =>
{
    return Results.Ok(cardSvc.GetCardsBySuit(suit));
})
.WithName("GetCardsBySuit")
.WithTags("Cards");

app.MapGet("/api/cards/starterpack/{suit}", (CardService cardSvc, Suit suit) =>
{
    return Results.Ok(cardSvc.GetStarterPackCards(suit));
})
.WithName("GetStarterPackCards")
.WithTags("Cards");

// ========================================
// Battle Endpoints
// ========================================

var battleStartEndpoint = app.MapPost("/api/battle/start", async (BattleService battleService, StartBattleRequest request) =>
{
    try
    {
        var autoBattleMode = Enum.TryParse<AutoBattleMode>(request.AutoBattleMode, true, out var mode)
            ? mode : AutoBattleMode.Watch;

        var battleState = await battleService.StartBattleAsync(
            request.CharacterId,
            request.Seed,
            request.AutoBattle,
            autoBattleMode,
            request.AIProfileId);

        var response = new StartBattleResponse
        {
            BattleId = battleState.BattleId,
            BattleState = battleState
        };

        // Debug: Test JSON serialization before sending
        Console.WriteLine($"=== Battle Start Debug ===");
        try
        {
            var testJson = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            });
            Console.WriteLine($"JSON serialization OK, length: {testJson.Length}");

            // Save debug file
            File.WriteAllText("battle_response_debug.json", testJson);
            Console.WriteLine("Saved to battle_response_debug.json");
        }
        catch (Exception serEx)
        {
            Console.WriteLine($"JSON SERIALIZATION ERROR: {serEx.Message}");

            // Try to find problematic card
            Console.WriteLine("Checking opponent hand cards:");
            for (int i = 0; i < battleState.OpponentHand.Count; i++)
            {
                var card = battleState.OpponentHand[i];
                try
                {
                    var cardJson = JsonSerializer.Serialize(card);
                    Console.WriteLine($"  [{i}] {card.Id}: OK");
                }
                catch (Exception cardEx)
                {
                    Console.WriteLine($"  [{i}] {card.Id}: FAILED - {cardEx.Message}");
                    Console.WriteLine($"      Script: {card.ImmediateEffect?.Script}");
                }
            }
        }

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Battle start error: {ex}");
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("StartBattle")
.WithTags("Battle");

var battleActionEndpoint = app.MapPost("/api/battle/{id}/action", async (BattleService battleService, string id, PlayerAction action) =>
{
    var (valid, message, state) = await battleService.ProcessActionAsync(id, action);
    return Results.Ok(new ActionResponse
    {
        Valid = valid,
        Message = message,
        BattleState = state
    });
})
.WithName("BattleAction")
.WithTags("Battle");

if (gameSettings.RateLimit.Enabled)
{
    battleStartEndpoint.RequireRateLimiting("battle");
    battleActionEndpoint.RequireRateLimiting("battle");
}

app.MapGet("/api/battle/{id}/state", (BattleService battleService, string id) =>
{
    var session = battleService.GetSession(id);
    return session is not null ? Results.Ok(session.State) : Results.NotFound();
})
.WithName("GetBattleState")
.WithTags("Battle");

app.MapPost("/api/battle/{id}/forfeit", async (BattleService battleService, string id) =>
{
    var (_, _, state) = await battleService.ProcessActionAsync(id, new PlayerAction { Action = "forfeit" });
    return Results.Ok(state);
})
.WithName("ForfeitBattle")
.WithTags("Battle");

app.MapPost("/api/battle/{id}/finalize", async (BattleService battleService, string id) =>
{
    var result = await battleService.FinalizeBattleAsync(id);
    return result is not null ? Results.Ok(result) : Results.NotFound();
})
.WithName("FinalizeBattle")
.WithTags("Battle");

// Toggle auto-battle mid-fight
app.MapPost("/api/battle/{id}/auto-battle", async (BattleService battleService, string id, ToggleAutoBattleRequest request) =>
{
    var (success, error, state) = await battleService.ToggleAutoBattleAsync(id, request.Enabled, request.AIProfileId);
    return success ? Results.Ok(state) : Results.BadRequest(new { error });
})
.WithName("ToggleAutoBattle")
.WithTags("Battle");

// Auto-queue action (for Watch mode)
app.MapPost("/api/battle/{id}/auto-queue", async (BattleService battleService, string id) =>
{
    var (valid, message, state) = await battleService.ProcessActionAsync(id, new PlayerAction { Action = "auto_queue" });
    return Results.Ok(new ActionResponse { Valid = valid, Message = message, BattleState = state });
})
.WithName("AutoQueue")
.WithTags("Battle");

// Instant battle (run entire battle server-side)
app.MapPost("/api/battle/instant", async (BattleService battleService, StartBattleRequest request) =>
{
    try
    {
        var autoBattleMode = Enum.TryParse<AutoBattleMode>(request.AutoBattleMode, true, out var mode)
            ? mode : AutoBattleMode.Instant;

        var state = await battleService.StartBattleAsync(
            request.CharacterId,
            request.Seed,
            autoBattle: true,
            autoBattleMode: AutoBattleMode.Instant,
            playerAIProfileId: request.AIProfileId);

        var session = battleService.GetSession(state.BattleId);
        if (session != null)
        {
            await battleService.RunInstantBattleAsync(session);
        }

        var result = await battleService.FinalizeBattleAsync(state.BattleId);
        return Results.Ok(new InstantBattleResponse
        {
            BattleId = state.BattleId,
            Result = result ?? new BattleResult(),
            BattleLog = state.BattleLog,
            TotalRounds = state.Round
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("InstantBattle")
.WithTags("Battle");

// ========================================
// Booster Pack Endpoints
// ========================================

app.MapPost("/api/packs/generate", async (BoosterPackService packService, ICharacterRepository repo, GeneratePackRequest request) =>
{
    var character = await repo.GetByIdAsync(request.CharacterId);
    if (character == null) return Results.NotFound();

    var pack = packService.GeneratePack(character);
    return Results.Ok(pack);
})
.WithName("GeneratePack")
.WithTags("BoosterPacks");

// ========================================
// Ghost Endpoints
// ========================================

app.MapGet("/api/ghosts/download", async (IGhostRepository repo) =>
{
    var ghosts = await repo.GetAllAsync();
    return Results.Ok(ghosts);
})
.WithName("DownloadGhosts")
.WithTags("Ghosts");

app.MapGet("/api/ghosts", async (IGhostRepository repo, int? minMMR, int? maxMMR, int? count) =>
{
    var ghosts = await repo.GetByMMRRangeAsync(
        minMMR ?? 0,
        maxMMR ?? 3000,
        count ?? 10);
    return Results.Ok(ghosts);
})
.WithName("GetGhosts")
.WithTags("Ghosts");

// ========================================
// Auth Endpoints
// ========================================

var registerEndpoint = app.MapPost("/api/auth/register", async (AuthService authService, RegisterRequest request) =>
{
    var (success, token, error, player) = await authService.RegisterAsync(request.Username, request.Password);
    if (!success)
    {
        return Results.BadRequest(new { error });
    }
    return Results.Ok(new AuthResponse
    {
        Token = token!,
        PlayerId = player!.Id,
        Username = player.Username
    });
})
.WithName("Register")
.WithTags("Auth");

var loginEndpoint = app.MapPost("/api/auth/login", async (AuthService authService, LoginRequest request) =>
{
    var (success, token, error, player) = await authService.LoginAsync(request.Username, request.Password);
    if (!success)
    {
        return Results.BadRequest(new { error });
    }
    return Results.Ok(new AuthResponse
    {
        Token = token!,
        PlayerId = player!.Id,
        Username = player.Username
    });
})
.WithName("Login")
.WithTags("Auth");

if (gameSettings.RateLimit.Enabled)
{
    registerEndpoint.RequireRateLimiting("auth");
    loginEndpoint.RequireRateLimiting("auth");
}

app.MapPost("/api/auth/logout", (AuthService authService, HttpContext context) =>
{
    var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
    if (string.IsNullOrEmpty(token))
    {
        return Results.BadRequest(new { error = "No token provided" });
    }
    authService.Logout(token);
    return Results.Ok(new { message = "Logged out" });
})
.WithName("Logout")
.WithTags("Auth");

app.MapGet("/api/auth/me", async (AuthService authService, HttpContext context) =>
{
    var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
    if (string.IsNullOrEmpty(token))
    {
        return Results.Unauthorized();
    }
    var player = await authService.GetPlayerFromToken(token);
    if (player == null)
    {
        return Results.Unauthorized();
    }
    return Results.Ok(new PlayerInfoResponse
    {
        PlayerId = player.Id,
        Username = player.Username,
        TotalWins = player.TotalWins,
        TotalLosses = player.TotalLosses,
        HighestMMR = player.HighestMMR,
        TotalBattles = player.TotalBattles
    });
})
.WithName("GetMe")
.WithTags("Auth");

// ========================================
// Health Check
// ========================================

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
.WithName("HealthCheck")
.WithTags("System");

app.MapGet("/api/info", (GameSettings settings) => Results.Ok(new
{
    version = "1.0.0",
    cardCount = cardService.GetAllCards().Count(),
    settings = new
    {
        baseHP = settings.Character.BaseHP,
        hpPerLevel = settings.Character.HPPerLevel,
        maxLevel = settings.Character.MaxLevel,
        baseQueueSlots = settings.Battle.BaseQueueSlots,
        statPointsPerLevel = settings.Character.StatPointsPerLevel,
        hpPerStatPoint = settings.Character.HPPerStatPoint,
        attackPercentPerPoint = settings.Character.AttackPercentPerPoint,
        defensePercentPerPoint = settings.Character.DefensePercentPerPoint,
        autoBattleWatchDelayMs = settings.AutoBattle.WatchModeDelayMs
    }
}))
.WithName("ServerInfo")
.WithTags("System");

Console.WriteLine("SuperDeck Server starting...");
Console.WriteLine($"Health check available at: /api/health");
Console.WriteLine("Now listening for requests...");
app.Run();

// ========================================
// Request/Response DTOs
// ========================================

public record CreateCharacterRequest(string Name, Suit SuitChoice, string? PlayerId = null);
public record UpdateStatsRequest(int Attack, int Defense, int Speed, int BonusHP = 0);
public record AddCardsRequest(List<string> CardIds);
public record RemoveCardsRequest(List<string> CardIds);
public record StartBattleRequest(
    string CharacterId,
    int? Seed = null,
    bool AutoBattle = false,
    string AutoBattleMode = "Watch",
    string? AIProfileId = null);

public record ToggleAutoBattleRequest(bool Enabled, string? AIProfileId = null);

public class InstantBattleResponse
{
    public string BattleId { get; set; } = string.Empty;
    public SuperDeck.Server.Services.BattleResult Result { get; set; } = new();
    public List<string> BattleLog { get; set; } = new();
    public int TotalRounds { get; set; }
}
public record GeneratePackRequest(string CharacterId);

// Auth DTOs
public record RegisterRequest(string Username, string Password);
public record LoginRequest(string Username, string Password);

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

public class PlayerInfoResponse
{
    public string PlayerId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int TotalWins { get; set; }
    public int TotalLosses { get; set; }
    public int HighestMMR { get; set; }
    public int TotalBattles { get; set; }
}

public class StartBattleResponse
{
    public string BattleId { get; set; } = string.Empty;
    public BattleState BattleState { get; set; } = new();
}

public class ActionResponse
{
    public bool Valid { get; set; }
    public string? Message { get; set; }
    public BattleState BattleState { get; set; } = new();
}
