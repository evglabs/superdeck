using FluentAssertions;
using Microsoft.Data.Sqlite;
using SuperDeck.Core.Models;
using SuperDeck.Tools.CharacterSeeder.Services;
using Dapper;
using System.Text.Json;

namespace SuperDeck.Tests.Tools.CharacterSeeder;

public class DatabaseSeederTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly SqliteDatabaseSeeder _seeder;

    public DatabaseSeederTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_seeder_{Guid.NewGuid()}.db");
        _seeder = new SqliteDatabaseSeeder(_testDbPath);
    }

    [Fact]
    public async Task UpsertGhostAsync_WhenNew_ShouldInsertGhostSnapshot()
    {
        var character = CreateTestCharacter("ghost_test_fire_lv5");

        await _seeder.UpsertGhostAsync(character);

        using var connection = new SqliteConnection($"Data Source={_testDbPath}");
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM GhostSnapshots WHERE Id = 'ghost_test_fire_lv5'");
        count.Should().Be(1);
    }

    [Fact]
    public async Task UpsertGhostAsync_ShouldSerializeCharacterState()
    {
        var character = CreateTestCharacter("ghost_serialize_test");

        await _seeder.UpsertGhostAsync(character);

        using var connection = new SqliteConnection($"Data Source={_testDbPath}");
        var json = await connection.ExecuteScalarAsync<string>(
            "SELECT SerializedCharacterState FROM GhostSnapshots WHERE Id = 'ghost_serialize_test'");
        json.Should().NotBeNullOrEmpty();

        var deserialized = JsonSerializer.Deserialize<Character>(json!);
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("Test Character");
        deserialized.Attack.Should().Be(10);
        deserialized.DeckCardIds.Should().Contain("card1");
    }

    [Fact]
    public async Task UpsertGhostAsync_ShouldSetGhostMMR()
    {
        var character = CreateTestCharacter("ghost_mmr_test");
        character.MMR = 1234;

        await _seeder.UpsertGhostAsync(character);

        using var connection = new SqliteConnection($"Data Source={_testDbPath}");
        var mmr = await connection.ExecuteScalarAsync<int>(
            "SELECT GhostMMR FROM GhostSnapshots WHERE Id = 'ghost_mmr_test'");
        mmr.Should().Be(1234);
    }

    [Fact]
    public async Task UpsertGhostAsync_ShouldSetAIProfileToDefault()
    {
        var character = CreateTestCharacter("ghost_profile_test");

        await _seeder.UpsertGhostAsync(character);

        using var connection = new SqliteConnection($"Data Source={_testDbPath}");
        var profileId = await connection.ExecuteScalarAsync<string>(
            "SELECT AIProfileId FROM GhostSnapshots WHERE Id = 'ghost_profile_test'");
        profileId.Should().Be("default");
    }

    [Fact]
    public async Task UpsertGhostAsync_WhenExists_ShouldUpdateExisting()
    {
        var character = CreateTestCharacter("ghost_upsert_test");
        character.MMR = 1000;
        await _seeder.UpsertGhostAsync(character);

        character.MMR = 1500;
        character.Attack = 20;
        await _seeder.UpsertGhostAsync(character);

        using var connection = new SqliteConnection($"Data Source={_testDbPath}");
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM GhostSnapshots WHERE Id = 'ghost_upsert_test'");
        count.Should().Be(1);

        var mmr = await connection.ExecuteScalarAsync<int>(
            "SELECT GhostMMR FROM GhostSnapshots WHERE Id = 'ghost_upsert_test'");
        mmr.Should().Be(1500);

        var json = await connection.ExecuteScalarAsync<string>(
            "SELECT SerializedCharacterState FROM GhostSnapshots WHERE Id = 'ghost_upsert_test'");
        var deserialized = JsonSerializer.Deserialize<Character>(json!);
        deserialized!.Attack.Should().Be(20);
    }

    [Fact]
    public async Task UpsertGhostAsync_ShouldSetSourceCharacterId()
    {
        var character = CreateTestCharacter("ghost_source_test");

        await _seeder.UpsertGhostAsync(character);

        using var connection = new SqliteConnection($"Data Source={_testDbPath}");
        var sourceId = await connection.ExecuteScalarAsync<string>(
            "SELECT SourceCharacterId FROM GhostSnapshots WHERE Id = 'ghost_source_test'");
        sourceId.Should().Be("ghost_source_test");
    }

    [Fact]
    public async Task GhostExistsAsync_WhenGhostExists_ShouldReturnTrue()
    {
        var character = CreateTestCharacter("ghost_exists_test");
        await _seeder.UpsertGhostAsync(character);

        var exists = await _seeder.GhostExistsAsync("ghost_exists_test");

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task GhostExistsAsync_WhenGhostDoesNotExist_ShouldReturnFalse()
    {
        var exists = await _seeder.GhostExistsAsync("nonexistent");

        exists.Should().BeFalse();
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
