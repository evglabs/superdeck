using SuperDeck.Core.Models;
using SuperDeck.Core.Models.Enums;

namespace SuperDeck.Core.Scripting;

public class ScriptGlobals
{
    // Character references
    public Character Player { get; set; } = null!;
    public Character Opponent { get; set; } = null!;
    public Character Caster { get; set; } = null!;
    public Character Target { get; set; } = null!;

    // Battle state
    public BattleState Battle { get; set; } = null!;

    // Display name helpers - return "You" for human player
    public string PlayerDisplayName => IsHumanPlayer(Player) ? "You" : Player.Name;
    public string OpponentDisplayName => IsHumanPlayer(Opponent) ? "You" : Opponent.Name;
    public string CasterDisplayName => IsHumanPlayer(Caster) ? "You" : Caster.Name;
    public string TargetDisplayName => IsHumanPlayer(Target) ? "You" : Target.Name;

    private bool IsHumanPlayer(Character c) => ReferenceEquals(c, Battle.Player);

    public string GetDisplayName(Character c) => IsHumanPlayer(c) ? "You" : c.Name;

    // Card being executed
    public Card This { get; set; } = null!;

    // Status effect (for hooks)
    public StatusEffect? Status { get; set; }

    // Modifiable Amount (for stat calculation hooks)
    public int Amount { get; set; }

    // Direct collection access
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

    // RNG
    public Random Rng { get; set; } = null!;

    // Custom state access for status effects
    public Dictionary<string, object> State => Status?.CustomState ?? new();

    // CardType enum access for scripts
    public CardType CardType_Attack => CardType.Attack;
    public CardType CardType_Defense => CardType.Defense;
    public CardType CardType_Buff => CardType.Buff;
    public CardType CardType_Debuff => CardType.Debuff;
    public CardType CardType_Utility => CardType.Utility;

    // Stat scaling settings
    public double AttackPercentPerPoint { get; set; } = 2.0;
    public double DefensePercentPerPoint { get; set; } = 2.0;

    // Logging
    public void Log(string message)
    {
        Battle.BattleLog.Add(message);
    }

    // Helper methods
    public void DealDamage(Character target, int amount)
    {
        // Apply attacker's attack percentage bonus
        var attacker = target == Battle.Player ? Battle.Opponent : Battle.Player;
        int attackStat = attacker.BattleStats.Attack;
        double attackMultiplier = 1.0 + (attackStat * AttackPercentPerPoint / 100.0);
        int boostedAmount = (int)(amount * attackMultiplier);

        // Apply target's defense percentage reduction
        int defense = target.BattleStats.Defense;
        double defenseMultiplier = Math.Max(0, 1.0 - (defense * DefensePercentPerPoint / 100.0));
        int damageAfterDefense = Math.Max(1, (int)(boostedAmount * defenseMultiplier));

        // Execute OnTakeDamage hooks for the target
        var targetStatuses = target == Battle.Player ? Battle.PlayerStatuses : Battle.OpponentStatuses;
        var hookStatuses = targetStatuses
            .Where(s => s.CompiledHooks.ContainsKey(HookType.OnTakeDamage))
            .ToList();

        int finalDamage = damageAfterDefense;
        foreach (var status in hookStatuses)
        {
            try
            {
                // Create a context for the hook
                var hookContext = new HookContext
                {
                    Player = target,
                    Opponent = target == Battle.Player ? Battle.Opponent : Battle.Player,
                    Caster = target,
                    Target = target,
                    Battle = Battle,
                    Rng = Rng,
                    Status = status,
                    Amount = finalDamage,
                    IncomingDamage = finalDamage,
                    Log = Log
                };
                status.CompiledHooks[HookType.OnTakeDamage](hookContext);
                finalDamage = hookContext.Amount; // Hooks can modify the amount
            }
            catch (Exception ex)
            {
                Log($"[Error] OnTakeDamage hook '{status.Name}' failed: {ex.Message}");
            }
        }

        finalDamage = Math.Max(0, finalDamage);
        target.CurrentHP -= finalDamage;

        var displayName = GetDisplayName(target);
        if (finalDamage != damageAfterDefense)
        {
            Log($"{displayName} takes {finalDamage} damage (modified from {damageAfterDefense} by effects)!");
        }
        else if (boostedAmount != amount || defense > 0)
        {
            Log($"{displayName} takes {finalDamage} damage (base {amount}, +{attackStat} atk, -{defense} def)!");
        }
        else
        {
            Log($"{displayName} takes {finalDamage} damage!");
        }
    }

    public void DealRawDamage(Character target, int amount)
    {
        // Raw damage bypasses defense but still triggers OnTakeDamage hooks
        var targetStatuses = target == Battle.Player ? Battle.PlayerStatuses : Battle.OpponentStatuses;
        var hookStatuses = targetStatuses
            .Where(s => s.CompiledHooks.ContainsKey(HookType.OnTakeDamage))
            .ToList();

        int finalDamage = amount;
        foreach (var status in hookStatuses)
        {
            try
            {
                var hookContext = new HookContext
                {
                    Player = target,
                    Opponent = target == Battle.Player ? Battle.Opponent : Battle.Player,
                    Caster = target,
                    Target = target,
                    Battle = Battle,
                    Rng = Rng,
                    Status = status,
                    Amount = finalDamage,
                    IncomingDamage = finalDamage,
                    Log = Log
                };
                status.CompiledHooks[HookType.OnTakeDamage](hookContext);
                finalDamage = hookContext.Amount;
            }
            catch (Exception ex)
            {
                Log($"[Error] OnTakeDamage hook '{status.Name}' failed: {ex.Message}");
            }
        }

        finalDamage = Math.Max(0, finalDamage);
        target.CurrentHP -= finalDamage;
        Log($"{GetDisplayName(target)} takes {finalDamage} raw damage!");
    }

    public void Heal(Character target, int amount)
    {
        int oldHP = target.CurrentHP;
        target.CurrentHP = Math.Min(target.MaxHP, target.CurrentHP + amount);
        int actualHeal = target.CurrentHP - oldHP;
        if (actualHeal > 0)
        {
            Log($"{GetDisplayName(target)} heals for {actualHeal} HP!");
        }
    }

    public void ApplyStatus(Character target, StatusEffect status)
    {
        var targetStatuses = target == Player ? PlayerStatuses : OpponentStatuses;
        targetStatuses.Add(status);
        Log($"{GetDisplayName(target)} gains {status.Name}!");
    }

    public void RemoveStatus(Character target, string statusName)
    {
        var targetStatuses = target == Player ? PlayerStatuses : OpponentStatuses;
        var toRemove = targetStatuses.FirstOrDefault(s => s.Name == statusName);
        if (toRemove != null)
        {
            targetStatuses.Remove(toRemove);
            Log($"{GetDisplayName(target)} loses {statusName}!");
        }
    }

    public void DrawCards(Character character, int count)
    {
        var (deck, hand, discard) = character == Player
            ? (PlayerDeck, PlayerHand, PlayerDiscard)
            : (OpponentDeck, OpponentHand, OpponentDiscard);

        for (int i = 0; i < count; i++)
        {
            if (deck.Count == 0 && discard.Count > 0)
            {
                // Recycle discard pile
                Shuffle(discard);
                deck.AddRange(discard);
                discard.Clear();
                Log($"{GetDisplayName(character)}'s discard pile shuffled into deck!");
            }

            if (deck.Count > 0)
            {
                var card = deck[0];
                deck.RemoveAt(0);
                hand.Add(card);
            }
        }
    }

    public void DiscardCards(Character character, int count)
    {
        var (hand, discard) = character == Player
            ? (PlayerHand, PlayerDiscard)
            : (OpponentHand, OpponentDiscard);

        for (int i = 0; i < count && hand.Count > 0; i++)
        {
            // Discard from the end of hand (most recently drawn)
            var card = hand[^1];
            hand.RemoveAt(hand.Count - 1);
            discard.Add(card);
            Log($"{GetDisplayName(character)} discards {card.Name}!");
        }
    }

    public void Shuffle<T>(List<T> list)
    {
        // Fisher-Yates shuffle
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // Card property accessors for use in scripts
    public Card? Card => This;

    // Hook-specific variables
    public int IncomingDamage { get; set; }
    public int OutgoingDamage { get; set; }
    public double AttackMultiplier { get; set; } = 1.0;
    public double SpeedBonus { get; set; } = 0;
    public bool PreventExpire { get; set; } = false;
    public bool PreventQueue { get; set; } = false;
    public StatusEffect? ExpiringStatus { get; set; }

    // Alias for ApplyStatus (used in card scripts)
    public void AddStatus(Character target, StatusEffect status)
    {
        ApplyStatus(target, status);
    }

    // Get all statuses for a character
    public List<StatusEffect> GetStatuses(Character target)
    {
        return target == Player ? PlayerStatuses : OpponentStatuses;
    }

    // Get statuses by name
    public List<StatusEffect> GetStatuses(Character target, string statusName)
    {
        var statuses = target == Player ? PlayerStatuses : OpponentStatuses;
        return statuses.Where(s => s.Name == statusName).ToList();
    }

    // Get only buffs
    public List<StatusEffect> GetBuffs(Character target)
    {
        var statuses = target == Player ? PlayerStatuses : OpponentStatuses;
        return statuses.Where(s => s.IsBuff).ToList();
    }

    // Remove current status (for use in status hooks)
    public void RemoveStatus()
    {
        if (Status != null)
        {
            var targetStatuses = Player.ActiveStatuses.Contains(Status) ? PlayerStatuses : OpponentStatuses;
            targetStatuses.Remove(Status);
        }
    }

    // Remove a specific status effect
    public void RemoveStatus(StatusEffect status)
    {
        PlayerStatuses.Remove(status);
        OpponentStatuses.Remove(status);
    }

    // Play a card immediately
    public void PlayCard(Card card)
    {
        // Queue the card for immediate resolution
        Battle.Log($"Playing {card.Name}!");
        // The actual execution would be handled by the BattleService
        // For now, just add to a pending list
        PlayerQueue.Add(card);
    }

    // Self-destruct the current card (remove from deck permanently)
    public void SelfDestruct()
    {
        if (This != null)
        {
            PlayerDeck.Remove(This);
            OpponentDeck.Remove(This);
            PlayerHand.Remove(This);
            OpponentHand.Remove(This);
            Battle.Log($"{This.Name} self-destructs!");
        }
    }

    // Get a random card from any suit (for Bootstraps)
    public Card? GetRandomCardFromAllSuits()
    {
        // This would need access to the card library
        // For now, return null and let the caller handle it
        return null;
    }

    // Helper to create and apply a simple damage-over-time status
    public void ApplyDOT(Character target, string name, int damagePerTurn, int duration)
    {
        var status = new StatusEffect
        {
            Name = name,
            Duration = duration,
            RemainingDuration = duration,
            IsBuff = false,
            HookScripts = new Dictionary<string, string>
            {
                { "OnTurnStart", $"Player.CurrentHP -= {damagePerTurn}; Log(PlayerDisplayName + \" suffers {damagePerTurn} {name} damage!\");" }
            }
        };
        ApplyStatus(target, status);
    }

    // Helper to apply irradiation (used by multiple radiation cards)
    public void ApplyIrradiate(Character target, int duration = 3)
    {
        ApplyDOT(target, "Irradiated", 3, duration);
    }

    // Helper to apply Hulkerize buff (doubles attack)
    public void ApplyHulkerize(Character target, int duration = 3)
    {
        var status = new StatusEffect
        {
            Name = "Hulkerized",
            Duration = duration,
            RemainingDuration = duration,
            IsBuff = true,
            HookScripts = new Dictionary<string, string>
            {
                { "OnCalculateAttack", "AttackMultiplier *= 2.0; Log(PlayerDisplayName + \" SMASH! Attack doubled!\");" }
            }
        };
        ApplyStatus(target, status);
    }

    // Helper to apply a stat buff
    public void ApplyStatBuff(Character target, string name, string stat, int amount, int duration)
    {
        var hookScript = stat switch
        {
            "Attack" => $"Amount += {amount};",
            "Defense" => $"Amount += {amount};",
            "Speed" => $"SpeedBonus += {amount};",
            _ => ""
        };
        var hookType = stat switch
        {
            "Attack" => "OnCalculateAttack",
            "Defense" => "OnCalculateDefense",
            "Speed" => "OnCalculateSpeed",
            _ => "OnTurnStart"
        };
        var status = new StatusEffect
        {
            Name = name,
            Duration = duration,
            RemainingDuration = duration,
            IsBuff = true,
            HookScripts = new Dictionary<string, string> { { hookType, hookScript } }
        };
        ApplyStatus(target, status);
    }
}
