namespace SuperDeck.Core.Models.Enums;

public enum HookType
{
    // Basic Lifecycle (6)
    OnTurnStart,
    OnTurnEnd,
    OnQueue,
    OnPlay,
    OnDiscard,
    OnCardResolve,

    // Combat (4)
    OnTakeDamage,
    OnDealDamage,
    OnHeal,
    OnDeath,

    // Stat Calculation (3)
    OnCalculateAttack,
    OnCalculateDefense,
    OnCalculateSpeed,

    // Phase (3)
    OnDrawPhase,
    OnQueuePhaseStart,
    BeforeQueueResolve,

    // Interaction (3)
    OnOpponentPlay,
    OnBuffExpire,
    OnBattleEnd
}
