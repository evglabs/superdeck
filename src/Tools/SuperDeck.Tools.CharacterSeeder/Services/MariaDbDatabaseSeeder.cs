using System.Text.Json;
using Dapper;
using MySqlConnector;
using SuperDeck.Core.Models;

namespace SuperDeck.Tools.CharacterSeeder.Services;

public class MariaDbDatabaseSeeder : IDatabaseSeeder
{
    private readonly string _connectionString;

    public MariaDbDatabaseSeeder(string connectionString)
    {
        _connectionString = connectionString;
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

    private async Task<bool> CharacterExistsAsync(string characterId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Characters WHERE Id = @Id",
            new { Id = characterId });
        return count > 0;
    }

    private async Task InsertCharacterAsync(Character character)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var row = new CharacterRow(character);

        await connection.ExecuteAsync(@"
            INSERT INTO Characters (Id, Name, Level, XP, Attack, Defense, Speed, DeckCardIds, Wins, Losses, MMR, IsGhost, IsPublished, OwnerPlayerId, CreatedAt, LastModified)
            VALUES (@Id, @Name, @Level, @XP, @Attack, @Defense, @Speed, @DeckCardIds, @Wins, @Losses, @MMR, @IsGhost, @IsPublished, @OwnerPlayerId, @CreatedAt, @LastModified)",
            row);
    }

    private async Task UpdateCharacterAsync(Character character)
    {
        await using var connection = new MySqlConnection(_connectionString);
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
        public bool IsGhost { get; set; }
        public bool IsPublished { get; set; }
        public string? OwnerPlayerId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastModified { get; set; }

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
            IsGhost = c.IsGhost;
            IsPublished = c.IsPublished;
            OwnerPlayerId = c.OwnerPlayerId;
            CreatedAt = c.CreatedAt;
            LastModified = c.LastModified;
        }
    }
}
