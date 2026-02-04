using Spectre.Console;
using SuperDeck.Client.Networking;
using SuperDeck.Core.Models;
using SuperDeck.Core.Models.Enums;

namespace SuperDeck.Client.UI;

public class BattleUI
{
    private readonly ApiClient _apiClient;
    private BattleState _state;
    private readonly string _battleId;
    private readonly int _watchDelayMs;
    private int _lastLogIndex = 0;
    private bool _autoBattleEnabled = false;

    public BattleUI(ApiClient apiClient, BattleState initialState, string battleId, int watchDelayMs = 500)
    {
        _apiClient = apiClient;
        _state = initialState;
        _battleId = battleId;
        _watchDelayMs = watchDelayMs;
    }

    public async Task RunBattleAsync()
    {
        while (!_state.IsComplete)
        {
            AnsiConsole.Clear();
            RenderBattleState();

            if (_state.Phase == BattlePhase.Queue)
            {
                if (_autoBattleEnabled)
                {
                    await HandleAutoQueuePhaseAsync();
                }
                else
                {
                    await HandleQueuePhaseAsync();
                }
            }
            else
            {
                // Auto-refresh state for other phases
                await Task.Delay(Math.Max(_watchDelayMs / 2, 100));
                var newState = await _apiClient.GetBattleStateAsync(_battleId);
                if (newState != null)
                {
                    _state = newState;
                }
            }
        }

        DisplayBattleResult();
    }

    private async Task HandleAutoQueuePhaseAsync()
    {
        AnsiConsole.MarkupLine("[cyan][[AUTO-BATTLE]] Selecting cards...[/]");
        await Task.Delay(_watchDelayMs);

        var result = await _apiClient.AutoQueueCardsAsync(_battleId);
        if (result.Valid)
        {
            _state = result.BattleState;
        }
        else
        {
            // Fall back to manual mode on error
            AnsiConsole.MarkupLine($"[yellow]Auto-battle failed: {Markup.Escape(result.Message ?? "Unknown error")}. Switching to manual.[/]");
            _autoBattleEnabled = false;
            await Task.Delay(1000);
            await HandleQueuePhaseAsync();
        }
    }

    private void RenderBattleState()
    {
        // Header with HP bars
        var playerHpPercent = (double)_state.Player.CurrentHP / _state.Player.MaxHP * 100;
        var opponentHpPercent = (double)_state.Opponent.CurrentHP / _state.Opponent.MaxHP * 100;

        var headerTable = new Table().Centered();
        headerTable.Border(TableBorder.Rounded);
        headerTable.AddColumn(new TableColumn($"[bold blue]{Markup.Escape(_state.Player.Name)}[/]").Centered());
        headerTable.AddColumn(new TableColumn($"[grey]Round {_state.Round}[/]").Centered());
        headerTable.AddColumn(new TableColumn($"[bold red]{Markup.Escape(_state.Opponent.Name)}[/]").Centered());

        var playerHpColor = playerHpPercent > 50 ? "green" : playerHpPercent > 25 ? "yellow" : "red";
        var opponentHpColor = opponentHpPercent > 50 ? "green" : opponentHpPercent > 25 ? "yellow" : "red";

        // Show turn order indicator
        var playerFirst = _state.PlayerGoesFirst ? "[green]FIRST[/]" : "[grey]second[/]";
        var opponentFirst = !_state.PlayerGoesFirst ? "[green]FIRST[/]" : "[grey]second[/]";

        headerTable.AddRow(
            $"[{playerHpColor}]HP: {_state.Player.CurrentHP}/{_state.Player.MaxHP}[/]\n{playerFirst}",
            $"[grey]{_state.Phase}[/]",
            $"[{opponentHpColor}]HP: {_state.Opponent.CurrentHP}/{_state.Opponent.MaxHP}[/]\n{opponentFirst}"
        );

        AnsiConsole.Write(headerTable);
        AnsiConsole.WriteLine();

        // Show opponent's queue if revealed (Snoop card effect)
        if (_state.OpponentQueueRevealed && _state.OpponentQueue.Any())
        {
            var opponentQueueStr = string.Join(" → ", _state.OpponentQueue.Select(c => $"[orange1]{Markup.Escape(c.Name)}[/]"));
            AnsiConsole.MarkupLine($"[yellow]Opponent's Queue (revealed):[/] {opponentQueueStr}");
            AnsiConsole.WriteLine();
        }

        // Show opponent's hand if revealed (Mind Read card effect)
        if (_state.OpponentHandRevealed && _state.OpponentHand.Any())
        {
            var opponentHandStr = string.Join(", ", _state.OpponentHand.Select(c => $"[orange1]{Markup.Escape(c.Name)}[/]"));
            AnsiConsole.MarkupLine($"[yellow]Opponent's Hand (revealed):[/] {opponentHandStr}");
            AnsiConsole.WriteLine();
        }

        // Status effects
        if (_state.PlayerStatuses.Any() || _state.OpponentStatuses.Any())
        {
            var effectsTable = new Table().Centered();
            effectsTable.Border(TableBorder.None);
            effectsTable.AddColumn("Your Effects");
            effectsTable.AddColumn("Opponent Effects");

            var playerEffects = _state.PlayerStatuses.Any()
                ? string.Join(", ", _state.PlayerStatuses.Select(s => $"[cyan]{Markup.Escape(s.Name)}({s.RemainingDuration})[/]"))
                : "[grey]None[/]";
            var opponentEffects = _state.OpponentStatuses.Any()
                ? string.Join(", ", _state.OpponentStatuses.Select(s => $"[orange1]{Markup.Escape(s.Name)}({s.RemainingDuration})[/]"))
                : "[grey]None[/]";

            effectsTable.AddRow(playerEffects, opponentEffects);
            AnsiConsole.Write(effectsTable);
            AnsiConsole.WriteLine();
        }

        // Battle log (new entries)
        if (_state.BattleLog.Count > _lastLogIndex)
        {
            AnsiConsole.Write(new Rule("[grey]Battle Log[/]").RuleStyle("grey"));
            foreach (var log in _state.BattleLog.Skip(_lastLogIndex).Take(8))
            {
                var color = log.Contains("damage") ? "red" :
                           log.Contains("heal") ? "green" :
                           log.Contains("wins") || log.Contains("VICTORY") ? "gold1" :
                           "grey";
                AnsiConsole.MarkupLine($"[{color}]  {Markup.Escape(log)}[/]");
            }
            _lastLogIndex = _state.BattleLog.Count;
            AnsiConsole.WriteLine();
        }
    }

    private async Task HandleQueuePhaseAsync()
    {
        AnsiConsole.Write(new Rule($"[yellow]Queue Phase - Select {_state.CurrentPlayerQueueSlots} cards[/]").RuleStyle("grey"));
        AnsiConsole.MarkupLine($"[grey]Queued: {_state.PlayerQueue.Count}/{_state.CurrentPlayerQueueSlots}[/]");
        AnsiConsole.WriteLine();

        // Display hand with indices
        if (_state.PlayerHand.Any())
        {
            var handTable = new Table();
            handTable.Border(TableBorder.Rounded);
            handTable.AddColumn("#");
            handTable.AddColumn("Card");
            handTable.AddColumn("Type");
            handTable.AddColumn("Description");

            for (int i = 0; i < _state.PlayerHand.Count; i++)
            {
                var card = _state.PlayerHand[i];
                var typeColor = card.Type switch
                {
                    CardType.Attack => "red",
                    CardType.Defense => "blue",
                    CardType.Buff => "green",
                    CardType.Debuff => "purple",
                    _ => "grey"
                };

                handTable.AddRow(
                    $"[bold]{i + 1}[/]",
                    $"[bold]{Markup.Escape(card.Name)}[/]",
                    $"[{typeColor}]{card.Type}[/]",
                    Markup.Escape(card.Description?.Length > 40 ? card.Description[..37] + "..." : card.Description ?? "")
                );
            }

            AnsiConsole.Write(handTable);
        }
        else
        {
            AnsiConsole.MarkupLine("[grey]No cards in hand[/]");
        }

        // Display current queue
        if (_state.PlayerQueue.Any())
        {
            AnsiConsole.WriteLine();
            var queueStr = string.Join(" → ", _state.PlayerQueue.Select(c => $"[cyan]{Markup.Escape(c.Name)}[/]"));
            AnsiConsole.MarkupLine($"[yellow]Your Queue:[/] {queueStr}");
        }

        AnsiConsole.WriteLine();

        // Build choices using tuples to handle duplicate card names
        var menuChoices = new List<(string action, int handIndex, string display)>();

        if (_state.PlayerQueue.Count < _state.CurrentPlayerQueueSlots && _state.PlayerHand.Any())
        {
            for (int i = 0; i < _state.PlayerHand.Count; i++)
            {
                var card = _state.PlayerHand[i];
                menuChoices.Add(("queue", i, $"[grey]#{i + 1}[/] Queue [bold]{Markup.Escape(card.Name)}[/] ({card.Type})"));
            }
        }

        menuChoices.Add(("confirm", -1, "Confirm Queue (fill rest with Wait)"));
        menuChoices.Add(("toggle_auto", -1, "[cyan]Enable Auto-Battle[/]"));
        menuChoices.Add(("details", -1, "View Card Details"));
        menuChoices.Add(("log", -1, "View Battle Log"));
        menuChoices.Add(("forfeit", -1, "[red]Forfeit[/]"));

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<(string action, int handIndex, string display)>()
                .Title("[yellow]Choose action:[/]")
                .HighlightStyle(new Style(Color.Cyan1))
                .UseConverter(x => x.display)
                .AddChoices(menuChoices));

        switch (choice.action)
        {
            case "queue":
                var result = await _apiClient.SubmitActionAsync(
                    _battleId,
                    new PlayerAction { Action = "queue_card", HandIndex = choice.handIndex });

                if (result.Valid)
                {
                    _state = result.BattleState;
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(result.Message ?? "Unknown error")}[/]");
                    await Task.Delay(1000);
                }
                break;

            case "confirm":
                var confirmResult = await _apiClient.SubmitActionAsync(
                    _battleId,
                    new PlayerAction { Action = "confirm_queue" });

                if (confirmResult.Valid)
                {
                    _state = confirmResult.BattleState;
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(confirmResult.Message ?? "Unknown error")}[/]");
                    await Task.Delay(1000);
                }
                break;

            case "toggle_auto":
                var confirmAuto = AnsiConsole.Confirm("[cyan]Enable auto-battle? AI will select cards for you.[/]", true);
                if (confirmAuto)
                {
                    _autoBattleEnabled = true;
                    await _apiClient.ToggleAutoBattleAsync(_battleId, true);
                    AnsiConsole.MarkupLine("[cyan]Auto-battle enabled![/]");
                    await Task.Delay(500);
                }
                break;

            case "details":
                ViewCardDetails();
                break;

            case "log":
                ViewFullBattleLog();
                break;

            case "forfeit":
                var confirm = AnsiConsole.Confirm("[red]Are you sure you want to forfeit?[/]", false);
                if (confirm)
                {
                    _state = await _apiClient.ForfeitBattleAsync(_battleId);
                }
                break;
        }
    }

    private void ViewCardDetails()
    {
        if (!_state.PlayerHand.Any())
        {
            AnsiConsole.MarkupLine("[grey]No cards in hand.[/]");
            Console.ReadKey(true);
            return;
        }

        // Use index-based selection to handle duplicate card names
        var cardChoices = new List<(int index, Card? card)> { (-1, null) }; // Back option
        for (int i = 0; i < _state.PlayerHand.Count; i++)
        {
            cardChoices.Add((i, _state.PlayerHand[i]));
        }

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<(int index, Card? card)>()
                .Title("[yellow]Select a card to view:[/]")
                .HighlightStyle(new Style(Color.Gold1))
                .UseConverter(x => x.card == null ? "[grey]Back[/]" : $"[grey]#{x.index + 1}[/] {Markup.Escape(x.card.Name)} ({x.card.Type})")
                .AddChoices(cardChoices));

        if (selected.card == null) return;

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

    private void ViewFullBattleLog()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[yellow]Full Battle Log[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();

        if (!_state.BattleLog.Any())
        {
            AnsiConsole.MarkupLine("[grey]No battle log entries yet.[/]");
        }
        else
        {
            int lineNum = 1;
            foreach (var log in _state.BattleLog)
            {
                var color = log.Contains("damage") ? "red" :
                           log.Contains("heal") || log.Contains("restore") ? "green" :
                           log.Contains("Round") || log.Contains("---") ? "yellow" :
                           log.Contains("wins") || log.Contains("VICTORY") ? "gold1" :
                           "grey";
                AnsiConsole.MarkupLine($"[grey]{lineNum,3}.[/] [{color}]{Markup.Escape(log)}[/]");
                lineNum++;
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    private void DisplayBattleResult()
    {
        AnsiConsole.Clear();

        var won = _state.WinnerId == _state.Player.Id;

        if (won)
        {
            AnsiConsole.Write(new FigletText("VICTORY!").Color(Color.Green).Centered());
            AnsiConsole.MarkupLine("[green]Congratulations! You won the battle![/]");
        }
        else
        {
            AnsiConsole.Write(new FigletText("DEFEAT").Color(Color.Red).Centered());
            AnsiConsole.MarkupLine("[red]Better luck next time![/]");
        }

        AnsiConsole.WriteLine();

        // Final stats
        var statsTable = new Table().Centered();
        statsTable.Border(TableBorder.Rounded);
        statsTable.AddColumn("Stat");
        statsTable.AddColumn("Value");

        statsTable.AddRow("Rounds", _state.Round.ToString());
        statsTable.AddRow("Your Final HP", $"{_state.Player.CurrentHP}/{_state.Player.MaxHP}");
        statsTable.AddRow("Opponent Final HP", $"{_state.Opponent.CurrentHP}/{_state.Opponent.MaxHP}");

        AnsiConsole.Write(statsTable);
        AnsiConsole.WriteLine();

        // Show last battle log entries
        AnsiConsole.Write(new Rule("[grey]Battle Summary[/]").RuleStyle("grey"));
        foreach (var log in _state.BattleLog.TakeLast(10))
        {
            AnsiConsole.MarkupLine($"[grey]  {Markup.Escape(log)}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        Console.ReadKey(true);
    }
}
