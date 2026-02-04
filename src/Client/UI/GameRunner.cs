using Spectre.Console;
using SuperDeck.Client.Networking;
using SuperDeck.Core.Models;
using SuperDeck.Core.Models.Enums;

namespace SuperDeck.Client.UI;

public class GameRunner
{
    private readonly ApiClient _apiClient;
    private Character? _currentCharacter;
    private ServerSettings _serverSettings = new();

    public GameRunner(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task RunAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("SuperDeck").Color(Color.Gold1).Centered());
        AnsiConsole.MarkupLine("[grey]A Deck-Building Card Game[/]");
        AnsiConsole.WriteLine();

        // Fetch server settings
        var serverInfo = await _apiClient.GetServerInfoAsync();
        if (serverInfo != null)
        {
            _serverSettings = serverInfo.Settings;
        }

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Main Menu[/]")
                    .HighlightStyle(new Style(Color.Gold1))
                    .AddChoices(new[]
                    {
                        "New Character",
                        "Load Character",
                        "Exit"
                    }));

            switch (choice)
            {
                case "New Character":
                    await CreateCharacterAsync();
                    break;
                case "Load Character":
                    await LoadCharacterAsync();
                    break;
                case "Exit":
                    return;
            }

            if (_currentCharacter != null)
            {
                await CharacterMenuAsync();
            }
        }
    }

    private async Task CreateCharacterAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[yellow]Create New Character[/]").RuleStyle("grey"));

        var name = AnsiConsole.Ask<string>("[green]Enter character name:[/]");

        // Filter out rare suits for starter selection
        var availableSuits = Enum.GetValues<Suit>()
            .Where(s => s != Suit.Basic && s != Suit.Money)
            .ToArray();

        var suitChoice = AnsiConsole.Prompt(
            new SelectionPrompt<Suit>()
                .Title("[yellow]Choose your starting suit:[/]")
                .HighlightStyle(new Style(Color.Cyan1))
                .AddChoices(availableSuits));

        await AnsiConsole.Status()
            .StartAsync("Creating character...", async ctx =>
            {
                _currentCharacter = await _apiClient.CreateCharacterAsync(name, suitChoice, _apiClient.CurrentPlayerId);
            });

        AnsiConsole.MarkupLine($"[green]Created character:[/] [bold]{Markup.Escape(_currentCharacter!.Name)}[/]");
        AnsiConsole.MarkupLine($"[grey]Starting deck: 3 Punch + 3 Block[/]");
        AnsiConsole.WriteLine();

        // Fetch starter pack cards for the chosen suit
        var starterPackCards = (await _apiClient.GetStarterPackCardsAsync(suitChoice)).ToList();

        if (starterPackCards.Any())
        {
            AnsiConsole.Write(new Rule($"[yellow]Select 3 cards from {suitChoice} Starter Pack[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();

            // Display available cards
            var cardTable = new Table();
            cardTable.AddColumn("Card");
            cardTable.AddColumn("Type");
            cardTable.AddColumn("Rarity");
            cardTable.AddColumn("Description");

            foreach (var card in starterPackCards)
            {
                var typeColor = card.Type switch
                {
                    CardType.Attack => "red",
                    CardType.Defense => "blue",
                    CardType.Buff => "green",
                    CardType.Debuff => "purple",
                    _ => "grey"
                };

                var rarityColor = card.Rarity switch
                {
                    Rarity.Common => "white",
                    Rarity.Uncommon => "green",
                    Rarity.Rare => "blue",
                    _ => "grey"
                };

                cardTable.AddRow(
                    $"[bold]{Markup.Escape(card.Name)}[/]",
                    $"[{typeColor}]{card.Type}[/]",
                    $"[{rarityColor}]{card.Rarity}[/]",
                    Markup.Escape(card.Description ?? "")
                );
            }

            AnsiConsole.Write(cardTable);
            AnsiConsole.WriteLine();

            // Multi-select prompt for card selection (up to 3)
            var selectedCards = AnsiConsole.Prompt(
                new MultiSelectionPrompt<Card>()
                    .Title("[yellow]Select up to 3 cards to add to your deck:[/]")
                    .NotRequired()
                    .HighlightStyle(new Style(Color.Cyan1))
                    .InstructionsText("[grey](Press [blue]<space>[/] to select, [green]<enter>[/] to confirm)[/]")
                    .AddChoices(starterPackCards)
                    .UseConverter(c => $"{Markup.Escape(c.Name)} ({c.Type}, {c.Rarity})"));

            // Limit to 3 if user somehow selected more
            if (selectedCards.Count > 3)
            {
                selectedCards = selectedCards.Take(3).ToList();
                AnsiConsole.MarkupLine("[yellow]Limited selection to 3 cards.[/]");
            }

            if (selectedCards.Any())
            {
                // Add selected cards to deck
                var selectedCardIds = selectedCards.Select(c => c.Id).ToList();
                await AnsiConsole.Status()
                    .StartAsync("Adding cards to deck...", async ctx =>
                    {
                        _currentCharacter = await _apiClient.AddCardsToDeckAsync(_currentCharacter!.Id, selectedCardIds);
                    });

                AnsiConsole.MarkupLine("[green]Cards added to deck:[/]");
                foreach (var card in selectedCards)
                {
                    AnsiConsole.MarkupLine($"  [cyan]+ {Markup.Escape(card.Name)}[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[grey]No additional cards selected.[/]");
            }
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]No starter pack cards available for {suitChoice}.[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    private async Task LoadCharacterAsync()
    {
        // Filter by player ID if authenticated
        var characters = (await _apiClient.GetCharactersAsync(_apiClient.CurrentPlayerId)).ToList();

        if (!characters.Any())
        {
            AnsiConsole.MarkupLine("[red]No characters found. Create one first![/]");
            AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
            Console.ReadKey(true);
            return;
        }

        _currentCharacter = AnsiConsole.Prompt(
            new SelectionPrompt<Character>()
                .Title("[yellow]Select character:[/]")
                .HighlightStyle(new Style(Color.Cyan1))
                .AddChoices(characters)
                .UseConverter(c => $"{Markup.Escape(c.Name)} (Level {c.Level}, MMR {c.MMR}, W:{c.Wins}/L:{c.Losses})"));
    }

    private async Task CharacterMenuAsync()
    {
        while (_currentCharacter != null)
        {
            AnsiConsole.Clear();
            DisplayCharacterStats();

            var availablePoints = GetAvailableStatPoints(_currentCharacter);
            var menuTitle = availablePoints > 0
                ? $"[yellow]{_currentCharacter.Name}'s Menu[/] [green](+{availablePoints} stat points available)[/]"
                : $"[yellow]{_currentCharacter.Name}'s Menu[/]";

            var choices = new List<string>
            {
                "Start Battle",
                "View Deck"
            };

            if (availablePoints > 0)
            {
                choices.Add("Allocate Stats");
            }

            choices.Add("Delete Character");
            choices.Add("Back to Main Menu");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(menuTitle)
                    .HighlightStyle(new Style(Color.Gold1))
                    .AddChoices(choices));

            switch (choice)
            {
                case "Start Battle":
                    await StartBattleAsync();
                    break;
                case "View Deck":
                    await ViewDeckAsync();
                    break;
                case "Allocate Stats":
                    await AllocateStatsAsync();
                    break;
                case "Delete Character":
                    await DeleteCharacterAsync();
                    break;
                case "Back to Main Menu":
                    _currentCharacter = null;
                    break;
            }
        }
    }

    private void DisplayCharacterStats()
    {
        var table = new Table().Centered();
        table.Title($"[bold yellow]{Markup.Escape(_currentCharacter!.Name)}[/]");
        table.Border(TableBorder.Rounded);
        table.AddColumn(new TableColumn("[cyan]Stat[/]").Centered());
        table.AddColumn(new TableColumn("[cyan]Value[/]").Centered());

        table.AddRow("Level", $"[bold]{_currentCharacter.Level}[/] / 10");
        var hpDisplay = _currentCharacter.BonusHP > 0
            ? $"[green]{_currentCharacter.MaxHP}[/] [grey](base {100 + _currentCharacter.Level * _serverSettings.HpPerLevel} + {_currentCharacter.BonusHP} bonus)[/]"
            : $"[green]{_currentCharacter.MaxHP}[/]";
        table.AddRow("HP", hpDisplay);
        table.AddRow("Attack", $"[red]{_currentCharacter.Attack}[/]");
        table.AddRow("Defense", $"[blue]{_currentCharacter.Defense}[/]");
        table.AddRow("Speed", $"[yellow]{_currentCharacter.Speed}[/]");
        table.AddRow("───", "───");
        table.AddRow("Wins", $"[green]{_currentCharacter.Wins}[/]");
        table.AddRow("Losses", $"[red]{_currentCharacter.Losses}[/]");
        table.AddRow("MMR", $"[gold1]{_currentCharacter.MMR}[/]");
        table.AddRow("XP", $"{_currentCharacter.XP}");
        table.AddRow("Deck Size", $"{_currentCharacter.DeckCardIds.Count} cards");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private int GetAvailableStatPoints(Character character)
    {
        int totalAllowed = character.Level * _serverSettings.StatPointsPerLevel;
        int hpPoints = character.BonusHP / _serverSettings.HpPerStatPoint;
        int totalUsed = character.Attack + character.Defense + character.Speed + hpPoints;
        return Math.Max(0, totalAllowed - totalUsed);
    }

    private async Task ViewDeckAsync()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[yellow]Deck ({_currentCharacter!.DeckCardIds.Count} cards)[/]").RuleStyle("grey"));

            // Load all cards
            var deckCards = new List<(string id, Card card)>();
            foreach (var cardId in _currentCharacter.DeckCardIds)
            {
                var card = await _apiClient.GetCardAsync(cardId);
                if (card != null)
                {
                    deckCards.Add((cardId, card));
                }
            }

            // Display deck table
            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumn("#");
            table.AddColumn("Card");
            table.AddColumn("Type");
            table.AddColumn("Suit");
            table.AddColumn("Rarity");
            table.AddColumn("Description");

            int index = 1;
            foreach (var (id, card) in deckCards)
            {
                var typeColor = card.Type switch
                {
                    CardType.Attack => "red",
                    CardType.Defense => "blue",
                    CardType.Buff => "green",
                    CardType.Debuff => "purple",
                    _ => "grey"
                };

                var rarityColor = card.Rarity switch
                {
                    Rarity.Common => "white",
                    Rarity.Uncommon => "green",
                    Rarity.Rare => "blue",
                    Rarity.Epic => "purple",
                    Rarity.Legendary => "gold1",
                    _ => "grey"
                };

                var desc = card.Description ?? "";
                if (desc.Length > 50) desc = desc[..47] + "...";

                table.AddRow(
                    index.ToString(),
                    $"[bold]{Markup.Escape(card.Name)}[/]",
                    $"[{typeColor}]{card.Type}[/]",
                    card.Suit.ToString(),
                    $"[{rarityColor}]{card.Rarity}[/]",
                    Markup.Escape(desc)
                );
                index++;
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]What would you like to do?[/]")
                    .HighlightStyle(new Style(Color.Gold1))
                    .AddChoices(new[] { "View Card Details", "Remove Card", "Back" }));

            if (action == "Back")
            {
                break;
            }
            else if (action == "View Card Details")
            {
                await ViewCardDetailsAsync(deckCards);
            }
            else if (action == "Remove Card")
            {
                await RemoveCardFromDeckAsync(deckCards);
            }
        }
    }

    private async Task ViewCardDetailsAsync(List<(string id, Card card)> deckCards)
    {
        if (!deckCards.Any())
        {
            AnsiConsole.MarkupLine("[red]No cards in deck.[/]");
            Console.ReadKey(true);
            return;
        }

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<(string id, Card card)>()
                .Title("[yellow]Select a card to view:[/]")
                .HighlightStyle(new Style(Color.Gold1))
                .UseConverter(x => $"{Markup.Escape(x.card.Name)} ({x.card.Suit})")
                .AddChoices(deckCards));

        AnsiConsole.Clear();
        DisplayCardDetails(selected.card);

        AnsiConsole.MarkupLine("\n[grey]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    private void DisplayCardDetails(Card card)
    {
        var rarityColor = card.Rarity switch
        {
            Rarity.Common => "white",
            Rarity.Uncommon => "green",
            Rarity.Rare => "blue",
            Rarity.Epic => "purple",
            Rarity.Legendary => "gold1",
            _ => "grey"
        };

        var typeColor = card.Type switch
        {
            CardType.Attack => "red",
            CardType.Defense => "blue",
            CardType.Buff => "green",
            CardType.Debuff => "purple",
            _ => "grey"
        };

        AnsiConsole.Write(new Rule($"[{rarityColor}]{Markup.Escape(card.Name)}[/]").RuleStyle("grey"));

        var panel = new Table().Border(TableBorder.Rounded).HideHeaders();
        panel.AddColumn("Property");
        panel.AddColumn("Value");

        panel.AddRow("[grey]Type[/]", $"[{typeColor}]{card.Type}[/]");
        panel.AddRow("[grey]Suit[/]", card.Suit.ToString());
        panel.AddRow("[grey]Rarity[/]", $"[{rarityColor}]{card.Rarity}[/]");
        panel.AddRow("[grey]Description[/]", Markup.Escape(card.Description ?? "No description"));

        if (card.ImmediateEffect != null)
        {
            panel.AddRow("[grey]Target[/]", card.ImmediateEffect.Target.ToString());
        }

        if (card.GrantsStatusTo != null)
        {
            var status = card.GrantsStatusTo.Status;
            panel.AddRow("[grey]Grants Status[/]", $"{Markup.Escape(status?.Name ?? "Unknown")} ({status?.Duration ?? 0} turns)");
        }

        AnsiConsole.Write(panel);
    }

    private async Task RemoveCardFromDeckAsync(List<(string id, Card card)> deckCards)
    {
        if (deckCards.Count <= 6)
        {
            AnsiConsole.MarkupLine("[red]You must keep at least 6 cards in your deck.[/]");
            AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
            Console.ReadKey(true);
            return;
        }

        // Add cancel option as a special entry
        var choices = new List<string> { "[grey]Cancel[/]" };
        choices.AddRange(deckCards.Select(x => $"{Markup.Escape(x.card.Name)} ({x.card.Suit} - {x.card.Rarity})"));

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]Select a card to remove:[/]")
                .HighlightStyle(new Style(Color.Gold1))
                .AddChoices(choices));

        if (selection == "[grey]Cancel[/]")
        {
            return;
        }

        // Find the selected card
        var selectedIndex = choices.IndexOf(selection) - 1; // -1 for Cancel option
        if (selectedIndex < 0 || selectedIndex >= deckCards.Count)
        {
            return;
        }

        var selected = deckCards[selectedIndex];
        var confirm = AnsiConsole.Confirm($"Remove [red]{Markup.Escape(selected.card.Name)}[/] from your deck?", false);
        if (confirm)
        {
            var updated = await _apiClient.RemoveCardsFromDeckAsync(_currentCharacter!.Id, new List<string> { selected.id });
            if (updated != null)
            {
                _currentCharacter = updated;
                AnsiConsole.MarkupLine($"[green]{Markup.Escape(selected.card.Name)} removed from deck.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Failed to remove card.[/]");
            }
            await Task.Delay(500);
        }
    }

    private async Task AllocateStatsAsync()
    {
        int available = GetAvailableStatPoints(_currentCharacter!);

        if (available <= 0)
        {
            AnsiConsole.MarkupLine("[yellow]No stat points available to allocate.[/]");
            AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
            Console.ReadKey(true);
            return;
        }

        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[yellow]Allocate Stats[/]").RuleStyle("grey"));
        AnsiConsole.MarkupLine($"[green]Available stat points: {available}[/]");
        AnsiConsole.MarkupLine($"Current stats: BonusHP={_currentCharacter!.BonusHP}, Attack={_currentCharacter.Attack}, Defense={_currentCharacter.Defense}, Speed={_currentCharacter.Speed}");
        AnsiConsole.WriteLine();

        var bonusHP = AnsiConsole.Prompt(
            new TextPrompt<int>($"[green]Bonus HP[/] (current: {_currentCharacter.BonusHP}, increments of {_serverSettings.HpPerStatPoint}):")
                .DefaultValue(_currentCharacter.BonusHP));

        var attack = AnsiConsole.Prompt(
            new TextPrompt<int>($"[red]Attack[/] (current: {_currentCharacter.Attack}):")
                .DefaultValue(_currentCharacter.Attack));

        var defense = AnsiConsole.Prompt(
            new TextPrompt<int>($"[blue]Defense[/] (current: {_currentCharacter.Defense}):")
                .DefaultValue(_currentCharacter.Defense));

        var speed = AnsiConsole.Prompt(
            new TextPrompt<int>($"[yellow]Speed[/] (current: {_currentCharacter.Speed}, min: 1):")
                .DefaultValue(_currentCharacter.Speed));

        if (speed < 1)
        {
            AnsiConsole.MarkupLine("[red]Speed must be at least 1![/]");
            Console.ReadKey(true);
            return;
        }

        try
        {
            _currentCharacter = await _apiClient.UpdateCharacterStatsAsync(
                _currentCharacter.Id, attack, defense, speed, bonusHP);
            AnsiConsole.MarkupLine("[green]Stats updated successfully![/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
        }

        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    private async Task AllocateStatsOnLevelUpAsync(int availablePoints)
    {
        int attack = _currentCharacter!.Attack;
        int defense = _currentCharacter.Defense;
        int speed = _currentCharacter.Speed;
        int bonusHP = _currentCharacter.BonusHP;
        int remaining = availablePoints;

        while (remaining > 0)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[yellow]Allocate Stat Points[/]").RuleStyle("grey"));
            AnsiConsole.WriteLine();

            var statsTable = new Table().Border(TableBorder.Rounded);
            statsTable.AddColumn("Stat");
            statsTable.AddColumn("Current");
            statsTable.AddColumn("New");

            statsTable.AddRow("[green]HP[/]", $"+{_currentCharacter.BonusHP}", $"+{bonusHP}");
            statsTable.AddRow("[red]Attack[/]", _currentCharacter.Attack.ToString(), attack.ToString());
            statsTable.AddRow("[blue]Defense[/]", _currentCharacter.Defense.ToString(), defense.ToString());
            statsTable.AddRow("[yellow]Speed[/]", _currentCharacter.Speed.ToString(), speed.ToString());

            AnsiConsole.Write(statsTable);
            AnsiConsole.MarkupLine($"\n[green]Remaining points: {remaining}[/]");
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Add a point to:[/]")
                    .HighlightStyle(new Style(Color.Gold1))
                    .AddChoices(new[] { $"+{_serverSettings.HpPerStatPoint} HP", "+1 Attack", "+1 Defense", "+1 Speed", "Confirm & Save" }));

            switch (choice)
            {
                case "+1 Attack":
                    attack++;
                    remaining--;
                    break;
                case "+1 Defense":
                    defense++;
                    remaining--;
                    break;
                case "+1 Speed":
                    speed++;
                    remaining--;
                    break;
                case "Confirm & Save":
                    // Allow saving even with remaining points
                    remaining = 0;
                    break;
                default:
                    // HP choice
                    bonusHP += _serverSettings.HpPerStatPoint;
                    remaining--;
                    break;
            }
        }

        // Save the stats
        try
        {
            _currentCharacter = await _apiClient.UpdateCharacterStatsAsync(_currentCharacter.Id, attack, defense, speed, bonusHP);
            AnsiConsole.MarkupLine("[green]Stats updated successfully![/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error saving stats: {Markup.Escape(ex.Message)}[/]");
        }

        await Task.Delay(500);
    }

    private async Task DeleteCharacterAsync()
    {
        var confirm = AnsiConsole.Confirm($"[red]Are you sure you want to delete {Markup.Escape(_currentCharacter!.Name)}?[/]", false);

        if (confirm)
        {
            await _apiClient.DeleteCharacterAsync(_currentCharacter.Id);
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(_currentCharacter.Name)} has been deleted.[/]");
            _currentCharacter = null;
            AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
            Console.ReadKey(true);
        }
    }

    private async Task StartBattleAsync()
    {
        try
        {
            var startResult = await _apiClient.StartBattleAsync(_currentCharacter!.Id);
            var battleUI = new BattleUI(_apiClient, startResult.BattleState, startResult.BattleId, _serverSettings.AutoBattleWatchDelayMs);
            await battleUI.RunBattleAsync();

            // Finalize battle and get results
            var result = await _apiClient.FinalizeBattleAsync(startResult.BattleId);

            // Show battle results
            if (result != null)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Rule("[yellow]Battle Results[/]").RuleStyle("grey"));

                var resultsTable = new Table().Border(TableBorder.Rounded);
                resultsTable.AddColumn("Stat");
                resultsTable.AddColumn("Value");

                resultsTable.AddRow("Outcome", result.PlayerWon ? "[green]Victory![/]" : "[red]Defeat[/]");
                resultsTable.AddRow("XP Gained", $"[cyan]+{result.XPGained}[/]");
                resultsTable.AddRow("MMR Change", result.MMRChange >= 0 ? $"[green]+{result.MMRChange}[/]" : $"[red]{result.MMRChange}[/]");

                if (result.LevelsGained > 0)
                {
                    resultsTable.AddRow("Level Up!", $"[gold1]Now Level {result.NewLevel}![/]");
                }

                AnsiConsole.Write(resultsTable);
                AnsiConsole.WriteLine();

                // Handle level-ups with booster pack selection and stat allocation
                if (result.LevelsGained > 0)
                {
                    for (int i = 0; i < result.LevelsGained; i++)
                    {
                        await SelectBoosterPackCardsAsync(result.NewLevel - result.LevelsGained + i + 1);
                    }

                    // Refresh character to get updated stats
                    _currentCharacter = await _apiClient.GetCharacterAsync(_currentCharacter!.Id);

                    // Prompt for stat allocation
                    int availablePoints = GetAvailableStatPoints(_currentCharacter!);
                    if (availablePoints > 0)
                    {
                        AnsiConsole.Clear();
                        AnsiConsole.Write(new FigletText("Stat Points!").Color(Color.Cyan1).Centered());
                        AnsiConsole.MarkupLine($"[green]You have {availablePoints} stat points to allocate![/]");
                        AnsiConsole.WriteLine();

                        await AllocateStatsOnLevelUpAsync(availablePoints);
                    }
                }

                AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
                Console.ReadKey(true);
            }

            // Refresh character data
            _currentCharacter = await _apiClient.GetCharacterAsync(_currentCharacter!.Id);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error starting battle: {Markup.Escape(ex.Message)}[/]");
            AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
            Console.ReadKey(true);
        }
    }

    private async Task SelectBoosterPackCardsAsync(int level)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText($"Level {level}!").Color(Color.Gold1).Centered());
        AnsiConsole.MarkupLine("[yellow]You earned a booster pack![/]");
        AnsiConsole.WriteLine();

        // Generate a booster pack
        var pack = await _apiClient.GeneratePackAsync(_currentCharacter!.Id);
        if (pack == null || !pack.Cards.Any())
        {
            AnsiConsole.MarkupLine("[red]Failed to generate booster pack.[/]");
            return;
        }

        const int maxActions = 3;
        var cardsToAdd = new List<(int packIndex, Card card)>();
        var cardsToRemove = new List<string>();
        var selectedPackIndices = new HashSet<int>();

        int ActionsUsed() => cardsToAdd.Count + cardsToRemove.Count;
        int ActionsRemaining() => maxActions - ActionsUsed();

        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[yellow]Level {level} Booster Pack[/]").RuleStyle("grey"));
            AnsiConsole.MarkupLine($"[cyan]Actions: {ActionsRemaining()}/{maxActions} remaining[/]");
            AnsiConsole.MarkupLine("[grey]Each action can ADD a pack card OR REMOVE a deck card[/]");
            AnsiConsole.WriteLine();

            // Display booster pack cards
            var sortedCards = pack.Cards.OrderBy(c => c.Rarity).ThenBy(c => c.Type).ThenBy(c => c.Name).ToList();

            var packTable = new Table().Border(TableBorder.Rounded);
            packTable.AddColumn("#");
            packTable.AddColumn("Card");
            packTable.AddColumn("Type");
            packTable.AddColumn("Suit");
            packTable.AddColumn("Rarity");
            packTable.AddColumn("Description");

            int index = 1;
            foreach (var card in sortedCards)
            {
                var rarityColor = GetRarityColor(card.Rarity);
                var typeColor = GetTypeColor(card.Type);
                var isSelected = selectedPackIndices.Contains(index - 1);

                var desc = card.Description ?? "";
                if (desc.Length > 40) desc = desc[..37] + "...";

                var nameDisplay = isSelected
                    ? $"[green]+[/] [{rarityColor}]{Markup.Escape(card.Name)}[/]"
                    : $"  [{rarityColor}]{Markup.Escape(card.Name)}[/]";

                packTable.AddRow(
                    index.ToString(),
                    nameDisplay,
                    $"[{typeColor}]{card.Type}[/]",
                    card.Suit.ToString(),
                    $"[{rarityColor}]{card.Rarity}[/]",
                    Markup.Escape(desc)
                );
                index++;
            }

            AnsiConsole.Write(packTable);

            // Show current actions
            if (cardsToAdd.Any() || cardsToRemove.Any())
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]Pending actions:[/]");
                foreach (var (_, card) in cardsToAdd)
                {
                    var rarityColor = GetRarityColor(card.Rarity);
                    AnsiConsole.MarkupLine($"  [green]+ADD[/] [{rarityColor}]{Markup.Escape(card.Name)}[/]");
                }
                foreach (var cardId in cardsToRemove)
                {
                    var card = await _apiClient.GetCardAsync(cardId);
                    var name = card?.Name ?? cardId;
                    AnsiConsole.MarkupLine($"  [red]-REM[/] {Markup.Escape(name)}");
                }
            }

            AnsiConsole.WriteLine();

            // Build menu options
            var menuChoices = new List<string>();

            if (ActionsRemaining() > 0)
            {
                menuChoices.Add("Add Card from Pack");
                menuChoices.Add("Remove Card from Deck");
            }

            if (cardsToAdd.Any())
            {
                menuChoices.Add("Undo Add");
            }

            if (cardsToRemove.Any())
            {
                menuChoices.Add("Undo Remove");
            }

            menuChoices.Add("View Card Details");

            if (cardsToAdd.Any() || cardsToRemove.Any())
            {
                menuChoices.Add($"Confirm ({cardsToAdd.Count} add, {cardsToRemove.Count} remove)");
            }

            menuChoices.Add("Done (use remaining actions or skip)");

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Choose action:[/]")
                    .HighlightStyle(new Style(Color.Gold1))
                    .AddChoices(menuChoices));

            if (action == "Add Card from Pack")
            {
                var availableIndices = Enumerable.Range(0, sortedCards.Count)
                    .Where(i => !selectedPackIndices.Contains(i))
                    .ToList();

                if (!availableIndices.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No more cards available to add.[/]");
                    await Task.Delay(500);
                    continue;
                }

                var selectedIndex = AnsiConsole.Prompt(
                    new SelectionPrompt<int>()
                        .Title("[yellow]Select a card to add:[/]")
                        .HighlightStyle(new Style(Color.Gold1))
                        .UseConverter(i =>
                        {
                            var c = sortedCards[i];
                            var rarityColor = GetRarityColor(c.Rarity);
                            var typeColor = GetTypeColor(c.Type);
                            return $"#{i + 1} [{rarityColor}]{Markup.Escape(c.Name)}[/] [{typeColor}]({c.Type})[/] - {c.Suit}";
                        })
                        .AddChoices(availableIndices));

                selectedPackIndices.Add(selectedIndex);
                cardsToAdd.Add((selectedIndex, sortedCards[selectedIndex]));
            }
            else if (action == "Remove Card from Deck")
            {
                await SelectDeckCardToRemoveAsync(cardsToRemove);
            }
            else if (action == "Undo Add")
            {
                var addedIndices = cardsToAdd.Select(c => c.packIndex).ToList();
                var indexToUndo = AnsiConsole.Prompt(
                    new SelectionPrompt<int>()
                        .Title("[yellow]Select card to undo:[/]")
                        .HighlightStyle(new Style(Color.Gold1))
                        .UseConverter(i =>
                        {
                            var c = sortedCards[i];
                            return $"#{i + 1} {Markup.Escape(c.Name)} ({c.Suit})";
                        })
                        .AddChoices(addedIndices));

                selectedPackIndices.Remove(indexToUndo);
                cardsToAdd.RemoveAll(c => c.packIndex == indexToUndo);
            }
            else if (action == "Undo Remove")
            {
                // Show cards marked for removal
                var removalCards = new List<(string id, Card card)>();
                foreach (var cardId in cardsToRemove)
                {
                    var card = await _apiClient.GetCardAsync(cardId);
                    if (card != null) removalCards.Add((cardId, card));
                }

                var toUndo = AnsiConsole.Prompt(
                    new SelectionPrompt<(string id, Card card)>()
                        .Title("[yellow]Select removal to undo:[/]")
                        .HighlightStyle(new Style(Color.Gold1))
                        .UseConverter(x => $"{Markup.Escape(x.card.Name)} ({x.card.Suit})")
                        .AddChoices(removalCards));

                cardsToRemove.Remove(toUndo.id);
            }
            else if (action == "View Card Details")
            {
                var allIndices = Enumerable.Range(0, sortedCards.Count).ToList();
                var indexToView = AnsiConsole.Prompt(
                    new SelectionPrompt<int>()
                        .Title("[yellow]Select a card to view:[/]")
                        .HighlightStyle(new Style(Color.Gold1))
                        .UseConverter(i =>
                        {
                            var c = sortedCards[i];
                            return $"#{i + 1} {Markup.Escape(c.Name)} ({c.Suit} - {c.Rarity})";
                        })
                        .AddChoices(allIndices));

                AnsiConsole.Clear();
                DisplayCardDetails(sortedCards[indexToView]);
                AnsiConsole.MarkupLine("\n[grey]Press any key to continue...[/]");
                Console.ReadKey(true);
            }
            else if (action.StartsWith("Confirm"))
            {
                break;
            }
            else if (action.StartsWith("Done"))
            {
                break;
            }
        }

        // Apply changes
        if (cardsToRemove.Any())
        {
            await _apiClient.RemoveCardsFromDeckAsync(_currentCharacter!.Id, cardsToRemove);
            AnsiConsole.MarkupLine($"[red]Removed {cardsToRemove.Count} card(s) from deck.[/]");
        }

        if (cardsToAdd.Any())
        {
            var cardIds = cardsToAdd.Select(c => c.card.Id).ToList();
            await _apiClient.AddCardsToDeckAsync(_currentCharacter!.Id, cardIds);
            AnsiConsole.MarkupLine($"[green]Added {cardsToAdd.Count} card(s) to deck![/]");
        }

        if (!cardsToAdd.Any() && !cardsToRemove.Any())
        {
            AnsiConsole.MarkupLine("[grey]No changes made.[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    private async Task SelectDeckCardToRemoveAsync(List<string> cardsToRemove)
    {
        // Load current deck cards (excluding already marked for removal)
        var deckCards = new List<(string id, Card card, int index)>();
        int idx = 1;
        foreach (var cardId in _currentCharacter!.DeckCardIds)
        {
            if (cardsToRemove.Contains(cardId)) continue;

            var card = await _apiClient.GetCardAsync(cardId);
            if (card != null)
            {
                deckCards.Add((cardId, card, idx));
            }
            idx++;
        }

        if (!deckCards.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No more cards available to remove.[/]");
            await Task.Delay(500);
            return;
        }

        // Check minimum deck size
        int currentDeckSize = _currentCharacter.DeckCardIds.Count - cardsToRemove.Count;
        if (currentDeckSize <= 6)
        {
            AnsiConsole.MarkupLine("[red]Cannot remove more cards. Minimum deck size is 6.[/]");
            await Task.Delay(1000);
            return;
        }

        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[yellow]Remove Card from Deck[/]").RuleStyle("grey"));
        AnsiConsole.MarkupLine($"[grey]Deck: {currentDeckSize} cards (min: 6)[/]");
        AnsiConsole.WriteLine();

        // Use tuple directly to handle duplicate card names
        var cancelOption = (id: "", card: (Card?)null, index: 0);
        var allChoices = new List<(string id, Card? card, int index)> { cancelOption };
        allChoices.AddRange(deckCards.Select(x => (x.id, (Card?)x.card, x.index)));

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<(string id, Card? card, int index)>()
                .Title("[yellow]Select card to remove:[/]")
                .HighlightStyle(new Style(Color.Gold1))
                .PageSize(15)
                .UseConverter(x => x.card == null
                    ? "[grey]Cancel[/]"
                    : $"[grey]#{x.index}[/] {Markup.Escape(x.card.Name)} ({x.card.Suit} - {x.card.Rarity})")
                .AddChoices(allChoices));

        if (selection.card != null)
        {
            cardsToRemove.Add(selection.id);
        }
    }

    private static string GetRarityColor(Rarity rarity) => rarity switch
    {
        Rarity.Common => "white",
        Rarity.Uncommon => "green",
        Rarity.Rare => "blue",
        Rarity.Epic => "purple",
        Rarity.Legendary => "gold1",
        _ => "grey"
    };

    private static string GetTypeColor(CardType type) => type switch
    {
        CardType.Attack => "red",
        CardType.Defense => "blue",
        CardType.Buff => "green",
        CardType.Debuff => "purple",
        _ => "grey"
    };
}
