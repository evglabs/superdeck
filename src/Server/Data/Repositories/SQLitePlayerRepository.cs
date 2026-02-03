using Dapper;
using Microsoft.Data.Sqlite;
using SuperDeck.Core.Data.Repositories;
using SuperDeck.Core.Models;

namespace SuperDeck.Server.Data.Repositories;

public class SQLitePlayerRepository : IPlayerRepository
{
    private readonly string _connectionString;

    public SQLitePlayerRepository(IConfiguration config)
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
            CREATE TABLE IF NOT EXISTS Players (
                Id TEXT PRIMARY KEY,
                Username TEXT UNIQUE NOT NULL,
                PasswordHash TEXT NOT NULL,
                Salt TEXT NOT NULL,
                TotalWins INTEGER DEFAULT 0,
                TotalLosses INTEGER DEFAULT 0,
                HighestMMR INTEGER DEFAULT 1000,
                TotalBattles INTEGER DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                LastLoginAt TEXT NOT NULL
            )
        ");
    }

    public async Task<Player?> GetByIdAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<Player>(
            "SELECT * FROM Players WHERE Id = @Id", new { Id = id });
    }

    public async Task<Player?> GetByUsernameAsync(string username)
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<Player>(
            "SELECT * FROM Players WHERE Username = @Username COLLATE NOCASE",
            new { Username = username });
    }

    public async Task<Player> CreateAsync(Player player)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(@"
            INSERT INTO Players (Id, Username, PasswordHash, Salt, TotalWins, TotalLosses,
                                 HighestMMR, TotalBattles, CreatedAt, LastLoginAt)
            VALUES (@Id, @Username, @PasswordHash, @Salt, @TotalWins, @TotalLosses,
                    @HighestMMR, @TotalBattles, @CreatedAt, @LastLoginAt)",
            player);
        return player;
    }

    public async Task<Player?> UpdateAsync(Player player)
    {
        using var connection = new SqliteConnection(_connectionString);
        var updated = await connection.ExecuteAsync(@"
            UPDATE Players SET
                TotalWins = @TotalWins,
                TotalLosses = @TotalLosses,
                HighestMMR = @HighestMMR,
                TotalBattles = @TotalBattles,
                LastLoginAt = @LastLoginAt
            WHERE Id = @Id",
            player);
        return updated > 0 ? player : null;
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        using var connection = new SqliteConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Players WHERE Username = @Username COLLATE NOCASE",
            new { Username = username });
        return count > 0;
    }
}
