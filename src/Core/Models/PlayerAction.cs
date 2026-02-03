namespace SuperDeck.Core.Models;

public class PlayerAction
{
    public string Action { get; set; } = string.Empty;  // "queue_card", "confirm_queue", "forfeit"
    public int? HandIndex { get; set; }
    public int? QueueSlot { get; set; }
}
