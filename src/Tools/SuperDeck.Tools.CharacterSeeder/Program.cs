using System.CommandLine;
using SuperDeck.Core.Models.Enums;
using SuperDeck.Tools.CharacterSeeder;
using SuperDeck.Tools.CharacterSeeder.Services;

var nameOption = new Option<string>(
    name: "--name",
    description: "Base character name (e.g., 'Blaze')") { IsRequired = true };

var suitOption = new Option<string[]>(
    name: "--suit",
    description: "Character suit(s) — repeat for multiple (e.g., --suit Fire --suit Berserker)") { IsRequired = true, AllowMultipleArgumentsPerToken = true };

var databaseOption = new Option<string>(
    name: "--database",
    getDefaultValue: () => "superdeck.db",
    description: "SQLite database path (used when provider is sqlite)");

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

var providerOption = new Option<string>(
    name: "--provider",
    getDefaultValue: () => "sqlite",
    description: "Database provider: sqlite or mariadb");

var connectionStringOption = new Option<string?>(
    name: "--connection-string",
    description: "MariaDB connection string (required when provider is mariadb)");

var rootCommand = new RootCommand("Ghost Character Seeder - generates level-scaled ghost characters for offline mode")
{
    nameOption,
    suitOption,
    databaseOption,
    levelOption,
    verboseOption,
    cardsPathOption,
    providerOption,
    connectionStringOption
};

rootCommand.SetHandler(async (name, suitStrs, database, level, verbose, cardsPath, provider, connectionString) =>
{
    var suits = new List<Suit>();
    foreach (var suitStr in suitStrs)
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

        suits.Add(suit);
    }

    var normalizedProvider = provider.ToLowerInvariant();
    if (normalizedProvider != "sqlite" && normalizedProvider != "mariadb")
    {
        Console.Error.WriteLine($"Invalid provider: {provider}");
        Console.Error.WriteLine("Valid providers: sqlite, mariadb");
        Environment.Exit(1);
        return;
    }

    if (normalizedProvider == "mariadb" && string.IsNullOrEmpty(connectionString))
    {
        Console.Error.WriteLine("--connection-string is required when provider is mariadb");
        Environment.Exit(1);
        return;
    }

    var options = new SeedOptions
    {
        Name = name,
        Suits = suitStrs,
        Database = database,
        Level = level,
        Verbose = verbose,
        CardsPath = cardsPath,
        Provider = normalizedProvider,
        ConnectionString = connectionString
    };

    await RunSeeder(options, suits.ToArray());
},
nameOption, suitOption, databaseOption, levelOption, verboseOption, cardsPathOption, providerOption, connectionStringOption);

return await rootCommand.InvokeAsync(args);

async Task RunSeeder(SeedOptions options, Suit[] suits)
{
    var generator = new CharacterGenerator();
    var deckBuilder = new DeckBuilder();

    IDatabaseSeeder seeder = options.Provider switch
    {
        "mariadb" => new MariaDbDatabaseSeeder(options.ConnectionString!),
        _ => new SqliteDatabaseSeeder(options.Database)
    };

    if (options.Verbose)
    {
        Console.WriteLine($"Loading cards from: {options.CardsPath}");
        Console.WriteLine($"Database provider: {options.Provider}");
    }

    await deckBuilder.LoadCardsAsync(options.CardsPath);

    var levels = options.Level.HasValue
        ? new[] { options.Level.Value }
        : Enumerable.Range(1, 10).ToArray();

    var suitNames = string.Join(", ", suits.Select(s => s.ToString()));

    if (options.Verbose)
    {
        Console.WriteLine($"Generating ghost characters for {options.Name} ({suitNames})");
        Console.WriteLine($"Levels: {string.Join(", ", levels)}");
        if (options.Provider == "sqlite")
            Console.WriteLine($"Database: {options.Database}");
        else
            Console.WriteLine($"Database: MariaDB");
        Console.WriteLine();
    }

    foreach (var level in levels)
    {
        var deck = deckBuilder.BuildDeck(suits, level);
        var character = generator.Generate(options.Name, suits, level, deck);

        await seeder.UpsertGhostAsync(character);

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

    Console.WriteLine($"Successfully seeded {levels.Length} ghost(s) for {options.Name} ({suitNames})");
}
