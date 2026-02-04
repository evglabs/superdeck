namespace SuperDeck.Core.Models;

public class HookContext
{
    // Character references
    public Character Player { get; set; } = null!;      // Status owner
    public Character Opponent { get; set; } = null!;    // Enemy
    public Character Caster { get; set; } = null!;      // Who triggered
    public Character Target { get; set; } = null!;      // Based on target field

    // Battle state
    public BattleState Battle { get; set; } = null!;

    // Status reference
    public StatusEffect Status { get; set; } = null!;

    // Triggering card (if applicable)
    public Card? TriggeringCard { get; set; }

    // Modifiable values (for stat calculation hooks)
    public int Amount { get; set; }
    public int IncomingDamage { get; set; }  // Original damage before hooks
    public int OutgoingDamage { get; set; }  // Damage being dealt (for OnDealDamage)
    public bool PreventExpire { get; set; }  // Prevent status from expiring
    public bool PreventQueue { get; set; }   // Prevent card from being queued
    public StatusEffect? ExpiringStatus { get; set; }  // Status about to expire (for OnBuffExpire)

    // Utilities
    public Random Rng { get; set; } = null!;
    public Action<string> Log { get; set; } = null!;

    // Card references for direct manipulation
    public List<Card> PlayerHand => Battle.PlayerHand;
    public List<Card> OpponentHand => Battle.OpponentHand;
    public List<Card> PlayerQueue => Battle.PlayerQueue;
    public List<Card> OpponentQueue => Battle.OpponentQueue;
    public List<Card> PlayerDiscard => Battle.PlayerDiscard;
    public List<Card> OpponentDiscard => Battle.OpponentDiscard;
    public List<Card> PlayerDeck => Battle.PlayerDeck;
    public List<Card> OpponentDeck => Battle.OpponentDeck;
    public List<StatusEffect> PlayerStatuses => Battle.PlayerStatuses;
    public List<StatusEffect> OpponentStatuses => Battle.OpponentStatuses;
}
