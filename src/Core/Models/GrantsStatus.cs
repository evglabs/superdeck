using SuperDeck.Core.Models.Enums;

namespace SuperDeck.Core.Models;

public class GrantsStatus
{
    public TargetType Target { get; set; }
    public StatusDefinition Status { get; set; } = new();
}
