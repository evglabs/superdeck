namespace SuperDeck.Core.Models;

public class Player
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;

    // Stats
    public int TotalWins { get; set; } = 0;
    public int TotalLosses { get; set; } = 0;
    public int HighestMMR { get; set; } = 1000;
    public int TotalBattles { get; set; } = 0;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
}
