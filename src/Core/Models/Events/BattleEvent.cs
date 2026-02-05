using System.Text.Json.Serialization;

namespace SuperDeck.Core.Models.Events;

/// <summary>
/// Base class for all battle events. Events are emitted during battle resolution
/// and can be played back by the client for animated visualization.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "eventType")]
[JsonDerivedType(typeof(RoundStartEvent), "round_start")]
[JsonDerivedType(typeof(SpeedRollEvent), "speed_roll")]
[JsonDerivedType(typeof(CardPlayedEvent), "card_played")]
[JsonDerivedType(typeof(DamageDealtEvent), "damage_dealt")]
[JsonDerivedType(typeof(HealingEvent), "healing")]
[JsonDerivedType(typeof(StatusGainedEvent), "status_gained")]
[JsonDerivedType(typeof(StatusExpiredEvent), "status_expired")]
[JsonDerivedType(typeof(StatusTriggeredEvent), "status_triggered")]
[JsonDerivedType(typeof(BattleEndEvent), "battle_end")]
[JsonDerivedType(typeof(MessageEvent), "message")]
public abstract class BattleEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int SequenceNumber { get; set; }
    public int SuggestedDelayMs { get; set; } = 400;
}

/// <summary>
/// Emitted at the start of each round.
/// </summary>
public class RoundStartEvent : BattleEvent
{
    public int RoundNumber { get; set; }
    public int PlayerHP { get; set; }
    public int OpponentHP { get; set; }

    public RoundStartEvent()
    {
        SuggestedDelayMs = 500;
    }
}

/// <summary>
/// Emitted when turn order is determined via speed roll.
/// </summary>
public class SpeedRollEvent : BattleEvent
{
    public int PlayerSpeed { get; set; }
    public int OpponentSpeed { get; set; }
    public bool PlayerGoesFirst { get; set; }
    public string PlayerName { get; set; } = "You";
    public string OpponentName { get; set; } = string.Empty;

    public SpeedRollEvent()
    {
        SuggestedDelayMs = 600;
    }
}

/// <summary>
/// Emitted when a card is played by either side.
/// </summary>
public class CardPlayedEvent : BattleEvent
{
    public string CasterName { get; set; } = string.Empty;
    public bool CasterIsPlayer { get; set; }
    public Card Card { get; set; } = new();
    public string TargetName { get; set; } = string.Empty;
    public bool TargetIsPlayer { get; set; }

    public CardPlayedEvent()
    {
        SuggestedDelayMs = 500;
    }
}

/// <summary>
/// Emitted when damage is dealt to a target.
/// </summary>
public class DamageDealtEvent : BattleEvent
{
    public int Amount { get; set; }
    public int BaseDamage { get; set; }
    public int FinalDamage { get; set; }
    public bool TargetIsPlayer { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public int TargetHPBefore { get; set; }
    public int TargetHPAfter { get; set; }
    public bool IsDOT { get; set; } = false;
    public string? DOTSourceName { get; set; }

    public DamageDealtEvent()
    {
        SuggestedDelayMs = 400;
    }
}

/// <summary>
/// Emitted when HP is restored.
/// </summary>
public class HealingEvent : BattleEvent
{
    public int Amount { get; set; }
    public bool TargetIsPlayer { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public int TargetHPBefore { get; set; }
    public int TargetHPAfter { get; set; }

    public HealingEvent()
    {
        SuggestedDelayMs = 300;
    }
}

/// <summary>
/// Emitted when a status effect (buff/debuff) is applied.
/// </summary>
public class StatusGainedEvent : BattleEvent
{
    public string StatusName { get; set; } = string.Empty;
    public int Duration { get; set; }
    public bool IsBuff { get; set; }
    public bool TargetIsPlayer { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public string SourceCardName { get; set; } = string.Empty;

    public StatusGainedEvent()
    {
        SuggestedDelayMs = 300;
    }
}

/// <summary>
/// Emitted when a status effect expires (duration reaches 0).
/// </summary>
public class StatusExpiredEvent : BattleEvent
{
    public string StatusName { get; set; } = string.Empty;
    public bool WasOnPlayer { get; set; }
    public string TargetName { get; set; } = string.Empty;

    public StatusExpiredEvent()
    {
        SuggestedDelayMs = 200;
    }
}

/// <summary>
/// Emitted when a status effect hook triggers (e.g., DOT damage, turn start effects).
/// </summary>
public class StatusTriggeredEvent : BattleEvent
{
    public string StatusName { get; set; } = string.Empty;
    public string HookType { get; set; } = string.Empty;
    public bool OwnerIsPlayer { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public StatusTriggeredEvent()
    {
        SuggestedDelayMs = 300;
    }
}

/// <summary>
/// Emitted when the battle concludes.
/// </summary>
public class BattleEndEvent : BattleEvent
{
    public string WinnerId { get; set; } = string.Empty;
    public string WinnerName { get; set; } = string.Empty;
    public bool PlayerWon { get; set; }
    public int PlayerFinalHP { get; set; }
    public int OpponentFinalHP { get; set; }
    public int OverkillDamage { get; set; }
    public string Reason { get; set; } = string.Empty;

    public BattleEndEvent()
    {
        SuggestedDelayMs = 800;
    }
}

/// <summary>
/// Emitted for generic script output or battle messages.
/// </summary>
public class MessageEvent : BattleEvent
{
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = "info";

    public MessageEvent()
    {
        SuggestedDelayMs = 200;
    }
}
