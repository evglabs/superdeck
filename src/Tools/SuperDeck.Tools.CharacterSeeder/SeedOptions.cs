namespace SuperDeck.Tools.CharacterSeeder;

public class SeedOptions
{
    public required string Name { get; init; }
    public required string[] Suits { get; init; }
    public string Database { get; init; } = "superdeck.db";
    public int? Level { get; init; }
    public bool Verbose { get; init; }
    public string CardsPath { get; init; } = "Data/ServerCards";
    public string Provider { get; init; } = "sqlite";
    public string? ConnectionString { get; init; }
}
