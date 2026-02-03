using Dapper;
using MySqlConnector;
using SuperDeck.Core.Data.Repositories;
using SuperDeck.Core.Models;

namespace SuperDeck.Server.Data.Repositories.MariaDB;

public class MariaDBPlayerRepository : IPlayerRepository
{
    private readonly string _connectionString;

    public MariaDBPlayerRepository(IConfiguration config)
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
            CREATE TABLE IF NOT EXISTS Players (
                Id VARCHAR(36) PRIMARY KEY,
                Username VARCHAR(50) UNIQUE NOT NULL,
                PasswordHash VARCHAR(100) NOT NULL,
                Salt VARCHAR(100) NOT NULL,
                TotalWins INT DEFAULT 0,
                TotalLosses INT DEFAULT 0,
                HighestMMR INT DEFAULT 1000,
                TotalBattles INT DEFAULT 0,
                CreatedAt DATETIME NOT NULL,
                LastLoginAt DATETIME NOT NULL,
                INDEX idx_players_username (Username)
            )
        ");
    }

    public async Task<Player?> GetByIdAsync(string id)
    {
        using var connection = new MySqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<Player>(
            "SELECT * FROM Players WHERE Id = @Id", new { Id = id });
    }

    public async Task<Player?> GetByUsernameAsync(string username)
    {
        using var connection = new MySqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<Player>(
            "SELECT * FROM Players WHERE Username = @Username",
            new { Username = username });
    }

    public async Task<Player> CreateAsync(Player player)
    {
        using var connection = new MySqlConnection(_connectionString);
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
        using var connection = new MySqlConnection(_connectionString);
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
        using var connection = new MySqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Players WHERE Username = @Username",
            new { Username = username });
        return count > 0;
    }
}
