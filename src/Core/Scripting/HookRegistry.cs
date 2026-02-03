using SuperDeck.Core.Models;
using SuperDeck.Core.Models.Enums;

namespace SuperDeck.Core.Scripting;

public class HookRegistry
{
    private readonly ScriptCompiler _compiler;

    public HookRegistry(ScriptCompiler compiler)
    {
        _compiler = compiler;
    }

    public async Task<StatusEffect> CompileStatusHooksAsync(StatusDefinition definition)
    {
        var effect = new StatusEffect
        {
            Name = definition.Name,
            Duration = definition.Duration,
            RemainingDuration = definition.Duration,
            HookScripts = new Dictionary<string, string>(definition.Hooks)
        };

        foreach (var (hookName, script) in definition.Hooks)
        {
            if (TryParseHookType(hookName, out var hookType))
            {
                try
                {
                    var compiledAction = await _compiler.CompileToActionAsync(script);
                    effect.CompiledHooks[hookType] = context =>
                    {
                        var globals = CreateGlobalsFromContext(context);
                        compiledAction(globals);
                        // Copy back Amount for stat calculation hooks
                        context.Amount = globals.Amount;
                    };
                }
                catch (ScriptCompilationException ex)
                {
                    // Log compilation error but don't fail completely
                    Console.Error.WriteLine($"Failed to compile hook {hookName}: {ex.Message}");
                }
            }
        }

        return effect;
    }

    public StatusEffect CompileStatusHooksSync(StatusDefinition definition)
    {
        return CompileStatusHooksAsync(definition).GetAwaiter().GetResult();
    }

    private static bool TryParseHookType(string hookName, out HookType hookType)
    {
        // Try direct parse first
        if (Enum.TryParse<HookType>(hookName, ignoreCase: true, out hookType))
        {
            return true;
        }

        // Handle common naming variations
        var normalizedName = hookName.Replace("_", "").Replace("-", "");
        return Enum.TryParse<HookType>(normalizedName, ignoreCase: true, out hookType);
    }

    private static ScriptGlobals CreateGlobalsFromContext(HookContext context)
    {
        return new ScriptGlobals
        {
            Player = context.Player,
            Opponent = context.Opponent,
            Caster = context.Caster,
            Target = context.Target,
            Battle = context.Battle,
            Status = context.Status,
            Amount = context.Amount,
            Rng = context.Rng,
            This = context.TriggeringCard ?? new Card()
        };
    }
}
