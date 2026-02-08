using Dapper;
using Microsoft.Data.Sqlite;
using SuperDeck.Core.Data.Repositories;
using SuperDeck.Core.Models;

namespace SuperDeck.Server.Data.Repositories;

public class SQLiteGhostRepository : IGhostRepository
{
    private readonly string _connectionString;

    public SQLiteGhostRepository(IConfiguration config)
    {
        var dbPath = config["DatabasePath"] ?? "superdeck.db";
        _connectionString = $"Data Source={dbPath}";
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

        // Migration: add IsRetirementGhost column
        try { connection.Execute("ALTER TABLE GhostSnapshots ADD COLUMN IsRetirementGhost INTEGER DEFAULT 0"); }
        catch { /* column already exists */ }
    }

    public async Task<GhostSnapshot?> GetByIdAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        var row = await connection.QueryFirstOrDefaultAsync<GhostRow>(
            "SELECT * FROM GhostSnapshots WHERE Id = @Id", new { Id = id });

        return row?.ToGhostSnapshot();
    }

    public async Task<IEnumerable<GhostSnapshot>> GetByMMRRangeAsync(int minMMR, int maxMMR, int count)
    {
        using var connection = new SqliteConnection(_connectionString);
        var rows = await connection.QueryAsync<GhostRow>(@"
            SELECT * FROM GhostSnapshots
            WHERE GhostMMR BETWEEN @MinMMR AND @MaxMMR
            ORDER BY RANDOM() LIMIT @Count",
            new { MinMMR = minMMR, MaxMMR = maxMMR, Count = count });

        return rows.Select(r => r.ToGhostSnapshot());
    }

    public async Task<GhostSnapshot> CreateAsync(GhostSnapshot ghost)
    {
        using var connection = new SqliteConnection(_connectionString);
        var row = new GhostRow(ghost);

        await connection.ExecuteAsync(@"
            INSERT INTO GhostSnapshots (Id, SourceCharacterId, SerializedCharacterState, GhostMMR, Wins, Losses, TimesUsed, AIProfileId, DownloadedAt, CreatedAt, IsRetirementGhost)
            VALUES (@Id, @SourceCharacterId, @SerializedCharacterState, @GhostMMR, @Wins, @Losses, @TimesUsed, @AIProfileId, @DownloadedAt, @CreatedAt, @IsRetirementGhost)",
            row);

        return ghost;
    }

    public async Task UpdateStatsAsync(string ghostId, bool won, int mmrChange)
    {
        using var connection = new SqliteConnection(_connectionString);
        var sql = won
            ? "UPDATE GhostSnapshots SET Wins = Wins + 1, TimesUsed = TimesUsed + 1, GhostMMR = MAX(100, GhostMMR + @MmrChange) WHERE Id = @Id"
            : "UPDATE GhostSnapshots SET Losses = Losses + 1, TimesUsed = TimesUsed + 1, GhostMMR = MAX(100, GhostMMR + @MmrChange) WHERE Id = @Id";

        await connection.ExecuteAsync(sql, new { Id = ghostId, MmrChange = mmrChange });
    }

    public async Task<IEnumerable<GhostSnapshot>> GetAllAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        var rows = await connection.QueryAsync<GhostRow>("SELECT * FROM GhostSnapshots");
        return rows.Select(r => r.ToGhostSnapshot());
    }

    public async Task<int> GetCountAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM GhostSnapshots");
    }
}

internal class GhostRow
{
    public string Id { get; set; } = string.Empty;
    public string SourceCharacterId { get; set; } = string.Empty;
    public string SerializedCharacterState { get; set; } = string.Empty;
    public int GhostMMR { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int TimesUsed { get; set; }
    public string AIProfileId { get; set; } = string.Empty;
    public string? DownloadedAt { get; set; }
    public string? CreatedAt { get; set; }
    public int IsRetirementGhost { get; set; }

    public GhostRow() { }

    public GhostRow(GhostSnapshot g)
    {
        Id = g.Id;
        SourceCharacterId = g.SourceCharacterId;
        SerializedCharacterState = g.SerializedCharacterState;
        GhostMMR = g.GhostMMR;
        Wins = g.Wins;
        Losses = g.Losses;
        TimesUsed = g.TimesUsed;
        AIProfileId = g.AIProfileId;
        DownloadedAt = g.DownloadedAt?.ToString("o");
        CreatedAt = g.CreatedAt.ToString("o");
        IsRetirementGhost = g.IsRetirementGhost ? 1 : 0;
    }

    public GhostSnapshot ToGhostSnapshot() => new()
    {
        Id = Id,
        SourceCharacterId = SourceCharacterId,
        SerializedCharacterState = SerializedCharacterState,
        GhostMMR = GhostMMR,
        Wins = Wins,
        Losses = Losses,
        TimesUsed = TimesUsed,
        AIProfileId = AIProfileId,
        DownloadedAt = string.IsNullOrEmpty(DownloadedAt) ? null : DateTime.Parse(DownloadedAt),
        CreatedAt = DateTime.TryParse(CreatedAt, out var created) ? created : DateTime.UtcNow,
        IsRetirementGhost = IsRetirementGhost == 1
    };
}
