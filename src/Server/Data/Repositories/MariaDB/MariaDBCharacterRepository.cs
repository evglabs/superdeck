using System.Text.Json;
using Dapper;
using MySqlConnector;
using SuperDeck.Core.Data.Repositories;
using SuperDeck.Core.Models;

namespace SuperDeck.Server.Data.Repositories.MariaDB;

public class MariaDBCharacterRepository : ICharacterRepository
{
    private readonly string _connectionString;

    public MariaDBCharacterRepository(IConfiguration config)
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
            CREATE TABLE IF NOT EXISTS Characters (
                Id VARCHAR(36) PRIMARY KEY,
                Name VARCHAR(100) NOT NULL,
                Level INT DEFAULT 1,
                XP INT DEFAULT 0,
                Attack INT DEFAULT 0,
                Defense INT DEFAULT 0,
                Speed INT DEFAULT 0,
                DeckCardIds TEXT,
                Wins INT DEFAULT 0,
                Losses INT DEFAULT 0,
                MMR INT DEFAULT 1000,
                IsGhost TINYINT(1) DEFAULT 0,
                IsPublished TINYINT(1) DEFAULT 0,
                OwnerPlayerId VARCHAR(36),
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                LastModified DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                INDEX idx_characters_mmr (MMR),
                INDEX idx_characters_player (OwnerPlayerId)
            )
        ");
    }

    public async Task<Character?> GetByIdAsync(string id)
    {
        using var connection = new MySqlConnection(_connectionString);
        var row = await connection.QueryFirstOrDefaultAsync<MariaDBCharacterRow>(
            "SELECT * FROM Characters WHERE Id = @Id", new { Id = id });

        return row?.ToCharacter();
    }

    public async Task<IEnumerable<Character>> GetByPlayerIdAsync(string? playerId)
    {
        using var connection = new MySqlConnection(_connectionString);

        IEnumerable<MariaDBCharacterRow> rows;
        if (string.IsNullOrEmpty(playerId))
        {
            rows = await connection.QueryAsync<MariaDBCharacterRow>(
                "SELECT * FROM Characters WHERE OwnerPlayerId IS NULL AND IsGhost = 0");
        }
        else
        {
            rows = await connection.QueryAsync<MariaDBCharacterRow>(
                "SELECT * FROM Characters WHERE OwnerPlayerId = @PlayerId AND IsGhost = 0",
                new { PlayerId = playerId });
        }

        return rows.Select(r => r.ToCharacter());
    }

    public async Task<IEnumerable<Character>> GetAllAsync()
    {
        using var connection = new MySqlConnection(_connectionString);
        var rows = await connection.QueryAsync<MariaDBCharacterRow>("SELECT * FROM Characters");
        return rows.Select(r => r.ToCharacter());
    }

    public async Task<Character> CreateAsync(Character character)
    {
        using var connection = new MySqlConnection(_connectionString);
        var row = new MariaDBCharacterRow(character);

        await connection.ExecuteAsync(@"
            INSERT INTO Characters (Id, Name, Level, XP, Attack, Defense, Speed, DeckCardIds, Wins, Losses, MMR, IsGhost, IsPublished, OwnerPlayerId, CreatedAt, LastModified)
            VALUES (@Id, @Name, @Level, @XP, @Attack, @Defense, @Speed, @DeckCardIds, @Wins, @Losses, @MMR, @IsGhost, @IsPublished, @OwnerPlayerId, @CreatedAt, @LastModified)",
            row);

        return character;
    }

    public async Task<Character> UpdateAsync(Character character)
    {
        using var connection = new MySqlConnection(_connectionString);
        var row = new MariaDBCharacterRow(character);

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
        using var connection = new MySqlConnection(_connectionString);
        var affected = await connection.ExecuteAsync("DELETE FROM Characters WHERE Id = @Id", new { Id = id });
        return affected > 0;
    }

    public async Task<IEnumerable<Character>> GetGhostsByMMRRangeAsync(int minMMR, int maxMMR, int count)
    {
        using var connection = new MySqlConnection(_connectionString);
        var rows = await connection.QueryAsync<MariaDBCharacterRow>(@"
            SELECT * FROM Characters
            WHERE IsGhost = 1 AND MMR BETWEEN @MinMMR AND @MaxMMR
            ORDER BY RAND() LIMIT @Count",
            new { MinMMR = minMMR, MaxMMR = maxMMR, Count = count });

        return rows.Select(r => r.ToCharacter());
    }

    public async Task UpdateStatsAsync(string characterId, bool won)
    {
        using var connection = new MySqlConnection(_connectionString);
        var sql = won
            ? "UPDATE Characters SET Wins = Wins + 1, MMR = MMR + 25, LastModified = @Now WHERE Id = @Id"
            : "UPDATE Characters SET Losses = Losses + 1, MMR = GREATEST(100, MMR - 25), LastModified = @Now WHERE Id = @Id";

        await connection.ExecuteAsync(sql, new { Id = characterId, Now = DateTime.UtcNow });
    }
}

internal class MariaDBCharacterRow
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
    public bool IsGhost { get; set; }
    public bool IsPublished { get; set; }
    public string? OwnerPlayerId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? LastModified { get; set; }

    public MariaDBCharacterRow() { }

    public MariaDBCharacterRow(Character c)
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
        IsGhost = c.IsGhost;
        IsPublished = c.IsPublished;
        OwnerPlayerId = c.OwnerPlayerId;
        CreatedAt = c.CreatedAt;
        LastModified = c.LastModified;
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
        IsGhost = IsGhost,
        IsPublished = IsPublished,
        OwnerPlayerId = OwnerPlayerId,
        CreatedAt = CreatedAt ?? DateTime.UtcNow,
        LastModified = LastModified ?? DateTime.UtcNow
    };
}
