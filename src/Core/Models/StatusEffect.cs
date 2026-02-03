using System.Text.Json.Serialization;
using SuperDeck.Core.Models.Enums;

namespace SuperDeck.Core.Models;

public class StatusEffect
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public int Duration { get; set; }
    public int RemainingDuration { get; set; }
    public string SourceCardId { get; set; } = string.Empty;
    public bool IsBuff { get; set; } = true;  // True for buffs, false for debuffs

    // Custom state storage for complex effects (e.g., Battery charge)
    [JsonIgnore]
    public Dictionary<string, object> CustomState { get; set; } = new();

    // Compiled hooks (set by ScriptCompiler at runtime) - not serializable
    [JsonIgnore]
    public Dictionary<HookType, Action<HookContext>> CompiledHooks { get; set; } = new();

    // Original hook scripts (for serialization)
    public Dictionary<string, string> HookScripts { get; set; } = new();

    // Timestamp
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    public void Remove(List<StatusEffect> statusList)
    {
        statusList.Remove(this);
    }

    public StatusEffect Clone()
    {
        return new StatusEffect
        {
            Name = Name,
            Duration = Duration,
            RemainingDuration = RemainingDuration,
            SourceCardId = SourceCardId,
            IsBuff = IsBuff,
            CustomState = new Dictionary<string, object>(CustomState),
            HookScripts = new Dictionary<string, string>(HookScripts),
            CompiledHooks = new Dictionary<HookType, Action<HookContext>>(CompiledHooks)
        };
    }
}
