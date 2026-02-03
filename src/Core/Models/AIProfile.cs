namespace SuperDeck.Core.Models;

public class AIProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BehaviorRulesJson { get; set; } = "{}";  // JSON configuration
    public int Difficulty { get; set; } = 5;  // 1-10 scale
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
