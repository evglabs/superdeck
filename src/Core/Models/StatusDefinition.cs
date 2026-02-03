namespace SuperDeck.Core.Models;

public class StatusDefinition
{
    public string Name { get; set; } = string.Empty;
    public int Duration { get; set; }
    public Dictionary<string, string> Hooks { get; set; } = new();
}
