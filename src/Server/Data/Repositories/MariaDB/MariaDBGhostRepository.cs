using Dapper;
using MySqlConnector;
using SuperDeck.Core.Data.Repositories;
using SuperDeck.Core.Models;

namespace SuperDeck.Server.Data.Repositories.MariaDB;

public class MariaDBGhostRepository : IGhostRepository
{
    private readonly string _connectionString;

    public MariaDBGhostRepository(IConfiguration config)
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
            CREATE TABLE IF NOT EXISTS GhostSnapshots (
                Id VARCHAR(128) PRIMARY KEY,
                SourceCharacterId VARCHAR(128) NOT NULL,
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

        // Widen columns on existing tables that were created with VARCHAR(36)
        connection.Execute("ALTER TABLE GhostSnapshots MODIFY Id VARCHAR(128)");
        connection.Execute("ALTER TABLE GhostSnapshots MODIFY SourceCharacterId VARCHAR(128)");
    }

    public async Task<GhostSnapshot?> GetByIdAsync(string id)
    {
        using var connection = new MySqlConnection(_connectionString);
        var row = await connection.QueryFirstOrDefaultAsync<MariaDBGhostRow>(
            "SELECT * FROM GhostSnapshots WHERE Id = @Id", new { Id = id });

        return row?.ToGhostSnapshot();
    }

    public async Task<IEnumerable<GhostSnapshot>> GetByMMRRangeAsync(int minMMR, int maxMMR, int count)
    {
        using var connection = new MySqlConnection(_connectionString);
        var rows = await connection.QueryAsync<MariaDBGhostRow>(@"
            SELECT * FROM GhostSnapshots
            WHERE GhostMMR BETWEEN @MinMMR AND @MaxMMR
            ORDER BY RAND() LIMIT @Count",
            new { MinMMR = minMMR, MaxMMR = maxMMR, Count = count });

        return rows.Select(r => r.ToGhostSnapshot());
    }

    public async Task<GhostSnapshot> CreateAsync(GhostSnapshot ghost)
    {
        using var connection = new MySqlConnection(_connectionString);
        var row = new MariaDBGhostRow(ghost);

        await connection.ExecuteAsync(@"
            INSERT INTO GhostSnapshots (Id, SourceCharacterId, SerializedCharacterState, GhostMMR, Wins, Losses, TimesUsed, AIProfileId, DownloadedAt, CreatedAt)
            VALUES (@Id, @SourceCharacterId, @SerializedCharacterState, @GhostMMR, @Wins, @Losses, @TimesUsed, @AIProfileId, @DownloadedAt, @CreatedAt)",
            row);

        return ghost;
    }

    public async Task UpdateStatsAsync(string ghostId, bool won, int mmrChange)
    {
        using var connection = new MySqlConnection(_connectionString);
        var sql = won
            ? "UPDATE GhostSnapshots SET Wins = Wins + 1, TimesUsed = TimesUsed + 1, GhostMMR = GREATEST(100, GhostMMR + @MmrChange) WHERE Id = @Id"
            : "UPDATE GhostSnapshots SET Losses = Losses + 1, TimesUsed = TimesUsed + 1, GhostMMR = GREATEST(100, GhostMMR + @MmrChange) WHERE Id = @Id";

        await connection.ExecuteAsync(sql, new { Id = ghostId, MmrChange = mmrChange });
    }

    public async Task<IEnumerable<GhostSnapshot>> GetAllAsync()
    {
        using var connection = new MySqlConnection(_connectionString);
        var rows = await connection.QueryAsync<MariaDBGhostRow>("SELECT * FROM GhostSnapshots");
        return rows.Select(r => r.ToGhostSnapshot());
    }

    public async Task<int> GetCountAsync()
    {
        using var connection = new MySqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM GhostSnapshots");
    }
}

internal class MariaDBGhostRow
{
    public string Id { get; set; } = string.Empty;
    public string SourceCharacterId { get; set; } = string.Empty;
    public string SerializedCharacterState { get; set; } = string.Empty;
    public int GhostMMR { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int TimesUsed { get; set; }
    public string AIProfileId { get; set; } = string.Empty;
    public DateTime? DownloadedAt { get; set; }
    public DateTime? CreatedAt { get; set; }

    public MariaDBGhostRow() { }

    public MariaDBGhostRow(GhostSnapshot g)
    {
        Id = g.Id;
        SourceCharacterId = g.SourceCharacterId;
        SerializedCharacterState = g.SerializedCharacterState;
        GhostMMR = g.GhostMMR;
        Wins = g.Wins;
        Losses = g.Losses;
        TimesUsed = g.TimesUsed;
        AIProfileId = g.AIProfileId;
        DownloadedAt = g.DownloadedAt;
        CreatedAt = g.CreatedAt;
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
        DownloadedAt = DownloadedAt,
        CreatedAt = CreatedAt ?? DateTime.UtcNow
    };
}
