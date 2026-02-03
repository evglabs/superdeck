using FluentAssertions;
using Microsoft.Data.Sqlite;
using SuperDeck.Core.Models;
using SuperDeck.Tools.CharacterSeeder.Services;
using Dapper;

namespace SuperDeck.Tests.Tools.CharacterSeeder;

public class SqliteDatabaseSeederTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly SqliteDatabaseSeeder _seeder;

    public SqliteDatabaseSeederTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_seeder_{Guid.NewGuid()}.db");
        InitializeTestDatabase();
        _seeder = new SqliteDatabaseSeeder(_testDbPath);
    }

    private void InitializeTestDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={_testDbPath}");
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
            )");
    }

    [Fact]
    public async Task InsertCharacterAsync_ShouldInsertCharacter()
    {
        var character = CreateTestCharacter("test_1");

        await _seeder.InsertCharacterAsync(character);

        using var connection = new SqliteConnection($"Data Source={_testDbPath}");
        var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Characters WHERE Id = 'test_1'");
        count.Should().Be(1);
    }

    [Fact]
    public async Task CharacterExistsAsync_WhenCharacterExists_ShouldReturnTrue()
    {
        var character = CreateTestCharacter("exists_test");
        await _seeder.InsertCharacterAsync(character);

        var exists = await _seeder.CharacterExistsAsync("exists_test");

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task CharacterExistsAsync_WhenCharacterDoesNotExist_ShouldReturnFalse()
    {
        var exists = await _seeder.CharacterExistsAsync("nonexistent");

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCharacterAsync_ShouldUpdateExistingCharacter()
    {
        var character = CreateTestCharacter("update_test");
        await _seeder.InsertCharacterAsync(character);

        character.MMR = 1500;
        character.Attack = 20;
        await _seeder.UpdateCharacterAsync(character);

        using var connection = new SqliteConnection($"Data Source={_testDbPath}");
        var row = await connection.QueryFirstAsync<dynamic>("SELECT MMR, Attack FROM Characters WHERE Id = 'update_test'");
        ((int)row.MMR).Should().Be(1500);
        ((int)row.Attack).Should().Be(20);
    }

    [Fact]
    public async Task UpsertCharacterAsync_WhenNew_ShouldInsert()
    {
        var character = CreateTestCharacter("upsert_new");

        await _seeder.UpsertCharacterAsync(character);

        var exists = await _seeder.CharacterExistsAsync("upsert_new");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task UpsertCharacterAsync_WhenExists_ShouldUpdate()
    {
        var character = CreateTestCharacter("upsert_existing");
        await _seeder.InsertCharacterAsync(character);

        character.MMR = 2000;
        await _seeder.UpsertCharacterAsync(character);

        using var connection = new SqliteConnection($"Data Source={_testDbPath}");
        var mmr = await connection.ExecuteScalarAsync<int>("SELECT MMR FROM Characters WHERE Id = 'upsert_existing'");
        mmr.Should().Be(2000);
    }

    [Fact]
    public async Task InsertCharacterAsync_ShouldSerializeDeckCardIds()
    {
        var character = CreateTestCharacter("deck_test");
        character.DeckCardIds = new List<string> { "card1", "card2", "card3" };

        await _seeder.InsertCharacterAsync(character);

        using var connection = new SqliteConnection($"Data Source={_testDbPath}");
        var deckJson = await connection.ExecuteScalarAsync<string>("SELECT DeckCardIds FROM Characters WHERE Id = 'deck_test'");
        deckJson.Should().Contain("card1");
        deckJson.Should().Contain("card2");
        deckJson.Should().Contain("card3");
    }

    [Fact]
    public async Task InsertCharacterAsync_ShouldSetIsGhost()
    {
        var character = CreateTestCharacter("ghost_test");
        character.IsGhost = true;

        await _seeder.InsertCharacterAsync(character);

        using var connection = new SqliteConnection($"Data Source={_testDbPath}");
        var isGhost = await connection.ExecuteScalarAsync<int>("SELECT IsGhost FROM Characters WHERE Id = 'ghost_test'");
        isGhost.Should().Be(1);
    }

    private static Character CreateTestCharacter(string id)
    {
        return new Character
        {
            Id = id,
            Name = "Test Character",
            Level = 5,
            XP = 0,
            Attack = 10,
            Defense = 5,
            Speed = 10,
            DeckCardIds = new List<string> { "card1" },
            Wins = 10,
            Losses = 5,
            MMR = 1100,
            IsGhost = true,
            IsPublished = false,
            OwnerPlayerId = null,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }
    }
}
