using System.CommandLine;
using SuperDeck.Core.Models.Enums;
using SuperDeck.Tools.CharacterSeeder;
using SuperDeck.Tools.CharacterSeeder.Services;

var nameOption = new Option<string>(
    name: "--name",
    description: "Base character name (e.g., 'Blaze')") { IsRequired = true };

var suitOption = new Option<string>(
    name: "--suit",
    description: "Character suit (e.g., 'Fire', 'Berserker')") { IsRequired = true };

var databaseOption = new Option<string>(
    name: "--database",
    getDefaultValue: () => "superdeck.db",
    description: "SQLite database path");

var levelOption = new Option<int?>(
    name: "--level",
    description: "Specific level (1-10) or omit for all levels");

var verboseOption = new Option<bool>(
    name: "--verbose",
    getDefaultValue: () => false,
    description: "Show detailed output");

var cardsPathOption = new Option<string>(
    name: "--cards-path",
    getDefaultValue: () => "Data/ServerCards",
    description: "Path to ServerCards directory");

var rootCommand = new RootCommand("Ghost Character Seeder - generates level-scaled ghost characters for offline mode")
{
    nameOption,
    suitOption,
    databaseOption,
    levelOption,
    verboseOption,
    cardsPathOption
};

rootCommand.SetHandler(async (name, suitStr, database, level, verbose, cardsPath) =>
{
    if (!Enum.TryParse<Suit>(suitStr, ignoreCase: true, out var suit))
    {
        Console.Error.WriteLine($"Invalid suit: {suitStr}");
        Console.Error.WriteLine($"Valid suits: {string.Join(", ", Enum.GetNames<Suit>())}");
        Environment.Exit(1);
        return;
    }

    if (suit == Suit.Basic)
    {
        Console.Error.WriteLine("Cannot create ghost characters for Basic suit");
        Environment.Exit(1);
        return;
    }

    var options = new SeedOptions
    {
        Name = name,
        Suit = suitStr,
        Database = database,
        Level = level,
        Verbose = verbose,
        CardsPath = cardsPath
    };

    await RunSeeder(options, suit);
},
nameOption, suitOption, databaseOption, levelOption, verboseOption, cardsPathOption);

return await rootCommand.InvokeAsync(args);

async Task RunSeeder(SeedOptions options, Suit suit)
{
    var generator = new CharacterGenerator();
    var deckBuilder = new DeckBuilder();
    var seeder = new DatabaseSeeder(options.Database);

    if (options.Verbose)
    {
        Console.WriteLine($"Loading cards from: {options.CardsPath}");
    }

    await deckBuilder.LoadCardsAsync(options.CardsPath);

    var levels = options.Level.HasValue
        ? new[] { options.Level.Value }
        : Enumerable.Range(1, 10).ToArray();

    if (options.Verbose)
    {
        Console.WriteLine($"Generating ghost characters for {options.Name} ({suit})");
        Console.WriteLine($"Levels: {string.Join(", ", levels)}");
        Console.WriteLine($"Database: {options.Database}");
        Console.WriteLine();
    }

    foreach (var level in levels)
    {
        var deck = deckBuilder.BuildDeck(suit, level);
        var character = generator.Generate(options.Name, suit, level, deck);

        await seeder.UpsertCharacterAsync(character);

        if (options.Verbose)
        {
            Console.WriteLine($"  [{character.Id}]");
            Console.WriteLine($"    Name: {character.Name}");
            Console.WriteLine($"    Level: {character.Level}");
            Console.WriteLine($"    Stats: ATK={character.Attack}, DEF={character.Defense}, SPD={character.Speed}");
            Console.WriteLine($"    MMR: {character.MMR}");
            Console.WriteLine($"    Record: {character.Wins}W / {character.Losses}L");
            Console.WriteLine($"    Deck Size: {character.DeckCardIds.Count} cards");
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine($"Created: {character.Id} (MMR: {character.MMR})");
        }
    }

    Console.WriteLine($"Successfully seeded {levels.Length} ghost character(s) for {options.Name} ({suit})");
}
