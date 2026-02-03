namespace SuperDeck.Core.Models;

public class BehaviorRules
{
    public double DefensiveThreshold { get; set; } = 0.3;
    public double RandomnessFactor { get; set; } = 0.1;
    public bool PriorityAttackWhenHPAdvantage { get; set; } = true;
    public bool AlwaysQueueMaxCards { get; set; } = true;
    public Dictionary<string, double> CardTypePreferences { get; set; } = new()
    {
        ["Attack"] = 1.0,
        ["Defense"] = 0.8,
        ["Utility"] = 0.6,
        ["Status"] = 0.7
    };
    public Dictionary<string, double> SuitPreferences { get; set; } = new();
}
