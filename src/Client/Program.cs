using Spectre.Console;
using SuperDeck.Client.Networking;
using SuperDeck.Client.UI;

// Parse command-line arguments
string? presetServerUrl = null;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--server" && i + 1 < args.Length)
    {
        presetServerUrl = args[i + 1];
        i++;
    }
}

// Also check environment variable
presetServerUrl ??= Environment.GetEnvironmentVariable("SUPERDECK_SERVER");

AnsiConsole.Clear();
AnsiConsole.Write(new FigletText("SuperDeck").Color(Color.Gold1).Centered());
AnsiConsole.MarkupLine("[grey]A Deck-Building Card Game[/]");
AnsiConsole.WriteLine();

EmbeddedServerManager? serverManager = null;
ApiClient? apiClient = null;

try
{
    string? mode;
    string? serverUrl;

    if (presetServerUrl != null)
    {
        // Skip mode selection and URL prompt when preconfigured
        mode = "Online";
        serverUrl = presetServerUrl;
    }
    else
    {
        mode = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]Select Mode[/]")
                .HighlightStyle(new Style(Color.Gold1))
                .AddChoices(new[]
                {
                    "Offline (Local Server)",
                    "Online (Connect to Server)"
                }));

        serverUrl = mode.StartsWith("Online")
            ? AnsiConsole.Ask<string>(
                "[green]Enter server URL[/] [grey](e.g., http://localhost:5000)[/]:",
                "http://localhost:5000")
            : null;
    }

    if (mode.StartsWith("Online"))
    {

        AnsiConsole.MarkupLine($"[yellow]Connecting to server at {Markup.Escape(serverUrl!)}...[/]");
        apiClient = new ApiClient(serverUrl!);

        if (!await apiClient.IsHealthyAsync())
        {
            AnsiConsole.MarkupLine("[red]Failed to connect to server![/]");
            AnsiConsole.MarkupLine("[grey]Press any key to exit...[/]");
            Console.ReadKey(true);
            return;
        }

        AnsiConsole.MarkupLine("[green]Connected to server![/]");
        AnsiConsole.WriteLine();

        // Login/Register loop
        while (!apiClient.IsAuthenticated)
        {
            var authChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Account[/]")
                    .HighlightStyle(new Style(Color.Gold1))
                    .AddChoices(new[] { "Login", "Register", "Exit" }));

            if (authChoice == "Exit")
            {
                return;
            }

            var username = AnsiConsole.Ask<string>("[green]Username:[/]");
            var password = AnsiConsole.Prompt(
                new TextPrompt<string>("[green]Password:[/]")
                    .Secret());

            if (authChoice == "Register")
            {
                var confirmPassword = AnsiConsole.Prompt(
                    new TextPrompt<string>("[green]Confirm Password:[/]")
                        .Secret());

                if (password != confirmPassword)
                {
                    AnsiConsole.MarkupLine("[red]Passwords do not match![/]");
                    continue;
                }

                var (success, error) = await apiClient.RegisterAsync(username, password);
                if (success)
                {
                    AnsiConsole.MarkupLine($"[green]Welcome, {Markup.Escape(username)}! Account created successfully.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Registration failed: {Markup.Escape(error ?? "Unknown error")}[/]");
                }
            }
            else // Login
            {
                var (success, error) = await apiClient.LoginAsync(username, password);
                if (success)
                {
                    AnsiConsole.MarkupLine($"[green]Welcome back, {Markup.Escape(username)}![/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Login failed: {Markup.Escape(error ?? "Unknown error")}[/]");
                }
            }
        }

        // Show player stats
        var playerInfo = await apiClient.GetCurrentPlayerAsync();
        if (playerInfo != null)
        {
            AnsiConsole.WriteLine();
            var statsTable = new Table().Border(TableBorder.Rounded);
            statsTable.AddColumn("[cyan]Your Stats[/]");
            statsTable.AddColumn("[cyan]Value[/]");
            statsTable.AddRow("Total Battles", playerInfo.TotalBattles.ToString());
            statsTable.AddRow("Wins", $"[green]{playerInfo.TotalWins}[/]");
            statsTable.AddRow("Losses", $"[red]{playerInfo.TotalLosses}[/]");
            statsTable.AddRow("Highest MMR", $"[gold1]{playerInfo.HighestMMR}[/]");
            AnsiConsole.Write(statsTable);
        }
    }
    else
    {
        // Start embedded server
        serverManager = new EmbeddedServerManager();

        AnsiConsole.MarkupLine("[yellow]Starting local server...[/]");
        AnsiConsole.MarkupLine("[grey](First startup may take longer while compiling)[/]");
        AnsiConsole.WriteLine();

        try
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("yellow"))
                .StartAsync("Initializing server...", async ctx =>
                {
                    await serverManager.StartAsync(output =>
                    {
                        // Update status based on output
                        if (output.Contains("Building"))
                        {
                            ctx.Status("[yellow]Building server...[/]");
                        }
                        else if (output.Contains("Compiling") || output.Contains("Restore"))
                        {
                            ctx.Status("[yellow]Compiling...[/]");
                        }
                        else if (output.Contains("Starting") || output.Contains("starting"))
                        {
                            ctx.Status("[yellow]Starting server...[/]");
                        }
                        else if (output.Contains("Now listening") || output.Contains("started"))
                        {
                            ctx.Status("[green]Server ready![/]");
                        }
                        else if (output.Contains("[Error]"))
                        {
                            ctx.Status($"[red]{Markup.Escape(output)}[/]");
                        }
                    });
                });

            AnsiConsole.MarkupLine($"[green]Local server started at {serverManager.BaseUrl}[/]");
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to start server: {Markup.Escape(ex.Message)}[/]");
            if (serverManager.LastError != null)
            {
                AnsiConsole.MarkupLine($"[red]Last error: {Markup.Escape(serverManager.LastError)}[/]");
            }
            AnsiConsole.MarkupLine("[grey]Press any key to exit...[/]");
            Console.ReadKey(true);
            return;
        }

        apiClient = new ApiClient(serverManager.BaseUrl);
    }

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
    Console.ReadKey(true);

    // Run the game
    var gameRunner = new GameRunner(apiClient);
    await gameRunner.RunAsync();
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
    AnsiConsole.MarkupLine("[grey]Press any key to exit...[/]");
    Console.ReadKey(true);
}
finally
{
    // Logout if authenticated
    if (apiClient?.IsAuthenticated == true)
    {
        await apiClient.LogoutAsync();
    }

    // Cleanup
    apiClient?.Dispose();

    if (serverManager != null)
    {
        AnsiConsole.MarkupLine("[yellow]Shutting down server...[/]");
        serverManager.Dispose();
    }
}

AnsiConsole.MarkupLine("[grey]Thanks for playing SuperDeck![/]");
