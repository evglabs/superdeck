namespace SuperDeck.Core.Models;

public enum AutoBattleMode
{
    Watch,   // Show battle progress, AI selects cards
    Instant  // Run entire battle server-side, return result
}

public class BattleSession
{
    public string BattleId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string CharacterId { get; set; } = string.Empty;
    public string GhostId { get; set; } = string.Empty;
    public string AIProfileId { get; set; } = "default";
    public int PlayerMMRAtStart { get; set; }
    public int OpponentMMRAtStart { get; set; }
    public BattleState State { get; set; } = new();
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public Random Rng { get; set; } = new();  // Seeded RNG for determinism

    // Auto-battle state
    public bool AutoBattleEnabled { get; set; } = false;
    public AutoBattleMode AutoBattleMode { get; set; } = AutoBattleMode.Watch;
    public string? PlayerAIProfileId { get; set; }
    public BehaviorRules? PlayerBehaviorRules { get; set; }

    public void UpdateActivity()
    {
        LastActivity = DateTime.UtcNow;
    }
}
