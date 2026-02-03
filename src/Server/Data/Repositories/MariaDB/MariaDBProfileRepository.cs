using System.Text.Json;
using Dapper;
using MySqlConnector;
using SuperDeck.Core.Data.Repositories;
using SuperDeck.Core.Models;

namespace SuperDeck.Server.Data.Repositories.MariaDB;

public class MariaDBProfileRepository : IAIProfileRepository
{
    private readonly string _connectionString;
    private const string DefaultProfileId = "default";

    public MariaDBProfileRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("MariaDB")
            ?? throw new InvalidOperationException("MariaDB connection string not configured");
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS AIProfiles (
                Id VARCHAR(36) PRIMARY KEY,
                Name VARCHAR(100) NOT NULL,
                Description TEXT,
                BehaviorRulesJson TEXT NOT NULL,
                Difficulty INT DEFAULT 5,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            )
        ");

        // Seed default profile if not exists
        var exists = connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM AIProfiles WHERE Id = @Id",
            new { Id = DefaultProfileId });

        if (exists == 0)
        {
            var defaultRules = new BehaviorRules();
            var defaultProfile = new AIProfile
            {
                Id = DefaultProfileId,
                Name = "Default AI",
                Description = "Standard balanced AI behavior",
                BehaviorRulesJson = JsonSerializer.Serialize(defaultRules),
                Difficulty = 5
            };

            connection.Execute(@"
                INSERT INTO AIProfiles (Id, Name, Description, BehaviorRulesJson, Difficulty, CreatedAt)
                VALUES (@Id, @Name, @Description, @BehaviorRulesJson, @Difficulty, @CreatedAt)",
                new
                {
                    defaultProfile.Id,
                    defaultProfile.Name,
                    defaultProfile.Description,
                    defaultProfile.BehaviorRulesJson,
                    defaultProfile.Difficulty,
                    defaultProfile.CreatedAt
                });
        }
    }

    public async Task<AIProfile?> GetByIdAsync(string id)
    {
        using var connection = new MySqlConnection(_connectionString);
        var row = await connection.QueryFirstOrDefaultAsync<MariaDBProfileRow>(
            "SELECT * FROM AIProfiles WHERE Id = @Id", new { Id = id });

        return row?.ToAIProfile();
    }

    public async Task<IEnumerable<AIProfile>> GetAllAsync()
    {
        using var connection = new MySqlConnection(_connectionString);
        var rows = await connection.QueryAsync<MariaDBProfileRow>("SELECT * FROM AIProfiles");
        return rows.Select(r => r.ToAIProfile());
    }

    public async Task<AIProfile> CreateAsync(AIProfile profile)
    {
        using var connection = new MySqlConnection(_connectionString);

        await connection.ExecuteAsync(@"
            INSERT INTO AIProfiles (Id, Name, Description, BehaviorRulesJson, Difficulty, CreatedAt)
            VALUES (@Id, @Name, @Description, @BehaviorRulesJson, @Difficulty, @CreatedAt)",
            new
            {
                profile.Id,
                profile.Name,
                profile.Description,
                profile.BehaviorRulesJson,
                profile.Difficulty,
                profile.CreatedAt
            });

        return profile;
    }

    public async Task<AIProfile?> GetDefaultAsync()
    {
        return await GetByIdAsync(DefaultProfileId);
    }
}

internal class MariaDBProfileRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string BehaviorRulesJson { get; set; } = "{}";
    public int Difficulty { get; set; }
    public DateTime? CreatedAt { get; set; }

    public AIProfile ToAIProfile() => new()
    {
        Id = Id,
        Name = Name,
        Description = Description ?? string.Empty,
        BehaviorRulesJson = BehaviorRulesJson,
        Difficulty = Difficulty,
        CreatedAt = CreatedAt ?? DateTime.UtcNow
    };
}
