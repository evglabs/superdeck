using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using SuperDeck.Core.Data.Repositories;
using SuperDeck.Core.Models;

namespace SuperDeck.Server.Data.Repositories;

public class SQLiteCharacterRepository : ICharacterRepository
{
    private readonly string _connectionString;

    public SQLiteCharacterRepository(IConfiguration config)
    {
        var dbPath = config["DatabasePath"] ?? "superdeck.db";
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Read and execute schema
        var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "schema.sql");
        if (!File.Exists(schemaPath))
        {
            schemaPath = "Data/schema.sql";
        }

        if (File.Exists(schemaPath))
        {
            var schema = File.ReadAllText(schemaPath);
            connection.Execute(schema);
        }
        else
        {
            // Create tables inline if schema file not found
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS Characters (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Level INTEGER DEFAULT 1,
                    XP INTEGER DEFAULT 0,
                    Attack INTEGER DEFAULT 0,
                    Defense INTEGER DEFAULT 0,
                    Speed INTEGER DEFAULT 5,
                    DeckCardIds TEXT,
                    Wins INTEGER DEFAULT 0,
                    Losses INTEGER DEFAULT 0,
                    MMR INTEGER DEFAULT 1000,
                    IsGhost INTEGER DEFAULT 0,
                    IsPublished INTEGER DEFAULT 0,
                    OwnerPlayerId TEXT,
                    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
                    LastModified TEXT DEFAULT CURRENT_TIMESTAMP
                );
                CREATE INDEX IF NOT EXISTS idx_characters_mmr ON Characters(MMR);
            ");
        }
    }

    public async Task<Character?> GetByIdAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        var row = await connection.QueryFirstOrDefaultAsync<CharacterRow>(
            "SELECT * FROM Characters WHERE Id = @Id", new { Id = id });

        return row?.ToCharacter();
    }

    public async Task<IEnumerable<Character>> GetByPlayerIdAsync(string? playerId)
    {
        using var connection = new SqliteConnection(_connectionString);

        IEnumerable<CharacterRow> rows;
        if (string.IsNullOrEmpty(playerId))
        {
            rows = await connection.QueryAsync<CharacterRow>(
                "SELECT * FROM Characters WHERE OwnerPlayerId IS NULL AND IsGhost = 0");
        }
        else
        {
            rows = await connection.QueryAsync<CharacterRow>(
                "SELECT * FROM Characters WHERE OwnerPlayerId = @PlayerId AND IsGhost = 0",
                new { PlayerId = playerId });
        }

        return rows.Select(r => r.ToCharacter());
    }

    public async Task<IEnumerable<Character>> GetAllAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        var rows = await connection.QueryAsync<CharacterRow>("SELECT * FROM Characters");
        return rows.Select(r => r.ToCharacter());
    }

    public async Task<Character> CreateAsync(Character character)
    {
        using var connection = new SqliteConnection(_connectionString);
        var row = new CharacterRow(character);

        await connection.ExecuteAsync(@"
            INSERT INTO Characters (Id, Name, Level, XP, Attack, Defense, Speed, DeckCardIds, Wins, Losses, MMR, IsGhost, IsPublished, OwnerPlayerId, CreatedAt, LastModified)
            VALUES (@Id, @Name, @Level, @XP, @Attack, @Defense, @Speed, @DeckCardIds, @Wins, @Losses, @MMR, @IsGhost, @IsPublished, @OwnerPlayerId, @CreatedAt, @LastModified)",
            row);

        return character;
    }

    public async Task<Character> UpdateAsync(Character character)
    {
        using var connection = new SqliteConnection(_connectionString);
        var row = new CharacterRow(character);

        await connection.ExecuteAsync(@"
            UPDATE Characters SET
                Name = @Name, Level = @Level, XP = @XP,
                Attack = @Attack, Defense = @Defense, Speed = @Speed,
                DeckCardIds = @DeckCardIds, Wins = @Wins, Losses = @Losses,
                MMR = @MMR, IsGhost = @IsGhost, IsPublished = @IsPublished,
                LastModified = @LastModified
            WHERE Id = @Id",
            row);

        return character;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        var affected = await connection.ExecuteAsync("DELETE FROM Characters WHERE Id = @Id", new { Id = id });
        return affected > 0;
    }

    public async Task<IEnumerable<Character>> GetGhostsByMMRRangeAsync(int minMMR, int maxMMR, int count)
    {
        using var connection = new SqliteConnection(_connectionString);
        var rows = await connection.QueryAsync<CharacterRow>(@"
            SELECT * FROM Characters
            WHERE IsGhost = 1 AND MMR BETWEEN @MinMMR AND @MaxMMR
            ORDER BY RANDOM() LIMIT @Count",
            new { MinMMR = minMMR, MaxMMR = maxMMR, Count = count });

        return rows.Select(r => r.ToCharacter());
    }

    public async Task UpdateStatsAsync(string characterId, bool won)
    {
        using var connection = new SqliteConnection(_connectionString);
        var sql = won
            ? "UPDATE Characters SET Wins = Wins + 1, MMR = MMR + 25, LastModified = @Now WHERE Id = @Id"
            : "UPDATE Characters SET Losses = Losses + 1, MMR = MAX(100, MMR - 25), LastModified = @Now WHERE Id = @Id";

        await connection.ExecuteAsync(sql, new { Id = characterId, Now = DateTime.UtcNow.ToString("o") });
    }
}

internal class CharacterRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int XP { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public string? DeckCardIds { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int MMR { get; set; }
    public int IsGhost { get; set; }
    public int IsPublished { get; set; }
    public string? OwnerPlayerId { get; set; }
    public string? CreatedAt { get; set; }
    public string? LastModified { get; set; }

    public CharacterRow() { }

    public CharacterRow(Character c)
    {
        Id = c.Id;
        Name = c.Name;
        Level = c.Level;
        XP = c.XP;
        Attack = c.Attack;
        Defense = c.Defense;
        Speed = c.Speed;
        DeckCardIds = JsonSerializer.Serialize(c.DeckCardIds);
        Wins = c.Wins;
        Losses = c.Losses;
        MMR = c.MMR;
        IsGhost = c.IsGhost ? 1 : 0;
        IsPublished = c.IsPublished ? 1 : 0;
        OwnerPlayerId = c.OwnerPlayerId;
        CreatedAt = c.CreatedAt.ToString("o");
        LastModified = c.LastModified.ToString("o");
    }

    public Character ToCharacter() => new()
    {
        Id = Id,
        Name = Name,
        Level = Level,
        XP = XP,
        Attack = Attack,
        Defense = Defense,
        Speed = Speed,
        DeckCardIds = string.IsNullOrEmpty(DeckCardIds)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(DeckCardIds) ?? new(),
        Wins = Wins,
        Losses = Losses,
        MMR = MMR,
        IsGhost = IsGhost == 1,
        IsPublished = IsPublished == 1,
        OwnerPlayerId = OwnerPlayerId,
        CreatedAt = DateTime.TryParse(CreatedAt, out var created) ? created : DateTime.UtcNow,
        LastModified = DateTime.TryParse(LastModified, out var modified) ? modified : DateTime.UtcNow
    };
}
