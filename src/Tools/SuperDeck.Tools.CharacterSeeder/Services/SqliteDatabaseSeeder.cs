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
            CREATE TABLE IF NOT EXISTS GhostSnapshots (
                Id TEXT PRIMARY KEY,
                SourceCharacterId TEXT NOT NULL,
                SerializedCharacterState TEXT NOT NULL,
                GhostMMR INTEGER DEFAULT 1000,
                Wins INTEGER DEFAULT 0,
                Losses INTEGER DEFAULT 0,
                TimesUsed INTEGER DEFAULT 0,
                AIProfileId TEXT NOT NULL,
                DownloadedAt TEXT,
                CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
            );
            CREATE INDEX IF NOT EXISTS idx_ghosts_mmr ON GhostSnapshots(GhostMMR);
        ");
    }

    public async Task<bool> GhostExistsAsync(string ghostId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM GhostSnapshots WHERE Id = @Id",
            new { Id = ghostId });
        return count > 0;
    }

    public async Task InsertGhostAsync(string ghostId, Character character)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var row = new GhostRow(ghostId, character);

        await connection.ExecuteAsync(@"
            INSERT INTO GhostSnapshots (Id, SourceCharacterId, SerializedCharacterState, GhostMMR, Wins, Losses, TimesUsed, AIProfileId, CreatedAt)
            VALUES (@Id, @SourceCharacterId, @SerializedCharacterState, @GhostMMR, @Wins, @Losses, @TimesUsed, @AIProfileId, @CreatedAt)",
            row);
    }

    public async Task UpdateGhostAsync(string ghostId, Character character)
    {
        await using var connection = new SqliteConnection(_connectionString);
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
        public string? CreatedAt { get; set; }

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
            CreatedAt = DateTime.UtcNow.ToString("o");
        }
    }
}
