using SuperDeck.Core.Models.Enums;

namespace SuperDeck.Core.Models;

public class ImmediateEffect
{
    public TargetType Target { get; set; }
    public string Script { get; set; } = string.Empty;
}
