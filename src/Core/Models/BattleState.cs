using SuperDeck.Core.Models.Enums;
using SuperDeck.Core.Models.Events;

namespace SuperDeck.Core.Models;

public class BattleState
{
    public string BattleId { get; set; } = Guid.NewGuid().ToString();
    public int Round { get; set; } = 0;
    public BattlePhase Phase { get; set; } = BattlePhase.NotStarted;

    // Participants
    public Character Player { get; set; } = new();
    public Character Opponent { get; set; } = new();

    // Turn order (set during resolution phase)
    public bool PlayerGoesFirst { get; set; }
    public int CurrentQueueIndex { get; set; } = 0;

    // Card piles - Player
    public List<Card> PlayerDeck { get; set; } = new();      // Draw pile
    public List<Card> PlayerHand { get; set; } = new();      // Current hand
    public List<Card> PlayerQueue { get; set; } = new();     // Queued for this round
    public List<Card> PlayerDiscard { get; set; } = new();   // Used cards

    // Card piles - Opponent
    public List<Card> OpponentDeck { get; set; } = new();
    public List<Card> OpponentHand { get; set; } = new();
    public List<Card> OpponentQueue { get; set; } = new();
    public List<Card> OpponentDiscard { get; set; } = new();

    // Status effects
    public List<StatusEffect> PlayerStatuses { get; set; } = new();
    public List<StatusEffect> OpponentStatuses { get; set; } = new();

    // Effective stats (base + status effects)
    public EffectiveStats PlayerEffectiveStats { get; set; } = new();
    public EffectiveStats OpponentEffectiveStats { get; set; } = new();

    // Configuration (from server settings)
    public int BaseQueueSlots { get; set; } = 3;
    public int MaxQueueSlots { get; set; } = 5;
    public int CurrentPlayerQueueSlots { get; set; } = 3;
    public int CurrentOpponentQueueSlots { get; set; } = 3;
    public int StartingHandSize { get; set; } = 5;
    public int OpponentStartingHandSize { get; set; } = 5;  // Can differ for UBER boss
    public int CardsDrawnPerTurn { get; set; } = 3;

    // Battle log (string-based, kept for backwards compatibility)
    public List<string> BattleLog { get; set; } = new();

    // Structured events for animated playback
    public List<BattleEvent> Events { get; set; } = new();
    private int _eventSequence = 0;

    /// <summary>
    /// Emits a structured event for client playback.
    /// Automatically assigns sequence number and captures current log length.
    /// </summary>
    public void EmitEvent(BattleEvent evt)
    {
        evt.SequenceNumber = _eventSequence++;
        evt.BattleLogLength = BattleLog.Count;
        Events.Add(evt);
    }

    // Result
    public string? WinnerId { get; set; }
    public bool IsComplete => WinnerId != null;
    public bool BothWin { get; set; } = false;  // For Broligarchy card
    public Character? Winner { get; set; }
    public int OverkillDamage { get; set; } = 0;

    // Turn tracking
    public int CardsPlayedThisTurn { get; set; } = 0;

    // Special mechanics
    public bool AllowDeckQueue { get; set; } = false;  // Super Intelligence
    public bool OpponentQueueRevealed { get; set; } = false;  // Snoop
    public bool OpponentHandRevealed { get; set; } = false;  // Mind Read
    public Card? EchoCard { get; set; }  // Card to echo
    public double EchoEffectiveness { get; set; } = 1.0;
    public bool MirrorNextResolve { get; set; } = false;  // Mirror Image
    public double MirrorEffectiveness { get; set; } = 1.0;
    public List<Card> SuspendedCards { get; set; } = new();  // Teleport

    // Utility method to add log entry
    public void Log(string message)
    {
        BattleLog.Add(message);
    }
}
