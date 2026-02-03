using SuperDeck.Core.Models;
using SuperDeck.Core.Models.Enums;

namespace SuperDeck.Core.Scripting;

public class HookExecutor
{
    public void ExecuteHooks(
        HookType hookType,
        BattleState battle,
        Character player,
        Character opponent,
        Random rng,
        Card? triggeringCard = null,
        int initialAmount = 0)
    {
        var context = CreateContext(battle, player, opponent, rng, triggeringCard, initialAmount);

        // Get statuses for the player (FIFO order - first applied executes first)
        var playerStatuses = (player == battle.Player ? battle.PlayerStatuses : battle.OpponentStatuses)
            .Where(s => s.CompiledHooks.ContainsKey(hookType))
            .ToList();

        foreach (var status in playerStatuses)
        {
            context.Status = status;
            try
            {
                status.CompiledHooks[hookType](context);
            }
            catch (Exception ex)
            {
                battle.BattleLog.Add($"[Error] Status '{status.Name}' hook failed: {ex.Message}");
            }
        }
    }

    public void ExecuteHooksForBoth(
        HookType hookType,
        BattleState battle,
        Random rng,
        Card? triggeringCard = null,
        int initialAmount = 0)
    {
        // Execute for player first
        ExecuteHooks(hookType, battle, battle.Player, battle.Opponent, rng, triggeringCard, initialAmount);
        // Then for opponent
        ExecuteHooks(hookType, battle, battle.Opponent, battle.Player, rng, triggeringCard, initialAmount);
    }

    public int ExecuteStatCalculationHooks(
        HookType hookType,
        BattleState battle,
        Character player,
        Character opponent,
        Random rng,
        int baseValue)
    {
        var context = CreateContext(battle, player, opponent, rng, null, baseValue);

        var statuses = (player == battle.Player ? battle.PlayerStatuses : battle.OpponentStatuses)
            .Where(s => s.CompiledHooks.ContainsKey(hookType))
            .ToList();

        foreach (var status in statuses)
        {
            context.Status = status;
            try
            {
                status.CompiledHooks[hookType](context);
            }
            catch (Exception ex)
            {
                battle.BattleLog.Add($"[Error] Stat calculation failed: {ex.Message}");
            }
        }

        return context.Amount;
    }

    public int GetCalculatedStat(
        HookType hookType,
        BattleState battle,
        Character character,
        Random rng,
        int baseValue)
    {
        var opponent = character == battle.Player ? battle.Opponent : battle.Player;
        return ExecuteStatCalculationHooks(hookType, battle, character, opponent, rng, baseValue);
    }

    private static HookContext CreateContext(
        BattleState battle,
        Character player,
        Character opponent,
        Random rng,
        Card? triggeringCard,
        int amount)
    {
        return new HookContext
        {
            Player = player,
            Opponent = opponent,
            Caster = player,
            Target = opponent,
            Battle = battle,
            Rng = rng,
            TriggeringCard = triggeringCard,
            Amount = amount,
            Log = msg => battle.BattleLog.Add(msg)
        };
    }
}
