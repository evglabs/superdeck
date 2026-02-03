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
            CREATE TABLE IF NOT EXISTS GhostSnapshots (
                Id VARCHAR(36) PRIMARY KEY,
                SourceCharacterId VARCHAR(36) NOT NULL,
                SerializedCharacterState TEXT NOT NULL,
                GhostMMR INT DEFAULT 1000,
                Wins INT DEFAULT 0,
                Losses INT DEFAULT 0,
                TimesUsed INT DEFAULT 0,
                AIProfileId VARCHAR(36) NOT NULL,
                DownloadedAt DATETIME,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                INDEX idx_ghosts_mmr (GhostMMR)
            )
        ");
    }

    private async Task<bool> GhostExistsAsync(string ghostId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM GhostSnapshots WHERE Id = @Id",
            new { Id = ghostId });
        return count > 0;
    }

    private async Task InsertGhostAsync(string ghostId, Character character)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var row = new GhostRow(ghostId, character);

        await connection.ExecuteAsync(@"
            INSERT INTO GhostSnapshots (Id, SourceCharacterId, SerializedCharacterState, GhostMMR, Wins, Losses, TimesUsed, AIProfileId, CreatedAt)
            VALUES (@Id, @SourceCharacterId, @SerializedCharacterState, @GhostMMR, @Wins, @Losses, @TimesUsed, @AIProfileId, @CreatedAt)",
            row);
    }

    private async Task UpdateGhostAsync(string ghostId, Character character)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var row = new GhostRow(ghostId, character);

        await connection.ExecuteAsync(@"
            UPDATE GhostSnapshots SET
                SerializedCharacterState = @SerializedCharacterState,
                GhostMMR = @GhostMMR,
                Wins = @Wins, Losses = @Losses
            WHERE Id = @Id",
            row);
    }

    public async Task UpsertGhostAsync(Character character)
    {
        var ghostId = character.Id;
        if (await GhostExistsAsync(ghostId))
        {
            await UpdateGhostAsync(ghostId, character);
        }
        else
        {
            await InsertGhostAsync(ghostId, character);
        }
    }

    private class GhostRow
    {
        public string Id { get; set; } = string.Empty;
        public string SourceCharacterId { get; set; } = string.Empty;
        public string SerializedCharacterState { get; set; } = string.Empty;
        public int GhostMMR { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int TimesUsed { get; set; }
        public string AIProfileId { get; set; } = "default";
        public DateTime? CreatedAt { get; set; }

        public GhostRow() { }

        public GhostRow(string ghostId, Character c)
        {
            Id = ghostId;
            SourceCharacterId = c.Id;
            SerializedCharacterState = JsonSerializer.Serialize(c);
            GhostMMR = c.MMR;
            Wins = c.Wins;
            Losses = c.Losses;
            TimesUsed = 0;
            AIProfileId = "default";
            CreatedAt = DateTime.UtcNow;
        }
    }
}
