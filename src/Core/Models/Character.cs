namespace SuperDeck.Core.Models;

public class Character
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public int XP { get; set; } = 0;

    // Base stats (from leveling)
    public int Attack { get; set; } = 0;
    public int Defense { get; set; } = 0;
    public int Speed { get; set; } = 5;  // Default starting speed

    // Battle-modified stats (reset each battle)
    public BattleStats BattleStats { get; set; } = new();

    // Combat state
    public int CurrentHP { get; set; }
    public int MaxHP => 100 + (Level * 10);

    // Deck and cards
    public List<string> DeckCardIds { get; set; } = new();

    // Record
    public int Wins { get; set; } = 0;
    public int Losses { get; set; } = 0;
    public int MMR { get; set; } = 1000;

    // Ghost/Publication status
    public bool IsGhost { get; set; } = false;
    public bool IsPublished { get; set; } = false;
    public string? OwnerPlayerId { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    // Status tracking (populated during battle)
    public List<StatusEffect> ActiveStatuses { get; set; } = new();

    // Economy (for Money suit cards)
    public long Money { get; set; } = 0;

    // Battle tracking
    public int TurnsWithoutDamage { get; set; } = 0;
    public bool HasPriority { get; set; } = false;
    public List<Card> LastTurnPlayedCards { get; set; } = new();
    public List<string> PendingRewards { get; set; } = new();

    public void InitializeForBattle()
    {
        CurrentHP = MaxHP;
        BattleStats.ResetFrom(this);
        ActiveStatuses.Clear();
    }
}
