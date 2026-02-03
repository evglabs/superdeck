using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using SuperDeck.Core.Models;

namespace SuperDeck.Tools.CharacterSeeder.Services;

public class SqliteDatabaseSeeder : IDatabaseSeeder
{
    private readonly string _connectionString;

    public SqliteDatabaseSeeder(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
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
            CREATE INDEX IF NOT EXISTS idx_characters_isghost ON Characters(IsGhost);
        ");
    }

    public async Task<bool> CharacterExistsAsync(string characterId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Characters WHERE Id = @Id",
            new { Id = characterId });
        return count > 0;
    }

    public async Task InsertCharacterAsync(Character character)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var row = new CharacterRow(character);

        await connection.ExecuteAsync(@"
            INSERT INTO Characters (Id, Name, Level, XP, Attack, Defense, Speed, DeckCardIds, Wins, Losses, MMR, IsGhost, IsPublished, OwnerPlayerId, CreatedAt, LastModified)
            VALUES (@Id, @Name, @Level, @XP, @Attack, @Defense, @Speed, @DeckCardIds, @Wins, @Losses, @MMR, @IsGhost, @IsPublished, @OwnerPlayerId, @CreatedAt, @LastModified)",
            row);
    }

    public async Task UpdateCharacterAsync(Character character)
    {
        await using var connection = new SqliteConnection(_connectionString);
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
    }

    public async Task UpsertCharacterAsync(Character character)
    {
        if (await CharacterExistsAsync(character.Id))
        {
            await UpdateCharacterAsync(character);
        }
        else
        {
            await InsertCharacterAsync(character);
        }
    }

    private class CharacterRow
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
    }
}
