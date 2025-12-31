using clypse.portal.setup.Extensions;
using Spectre.Console;

namespace clypse.portal.setup.Services.Orchestration;

public class SetupInteractiveMenuService : ISetupInteractiveMenuService
{
    public bool Run(AwsServiceOptions options)
    {
        return ConfigureAwsOptionsInteractively(options);
    }

    private static bool ConfigureAwsOptionsInteractively(AwsServiceOptions options)
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(
                new Rule("[yellow]Clypse AWS Setup[/]")
                    .RuleStyle("grey")
                    .LeftJustified());

            AnsiConsole.MarkupLine("[grey]Edit values before running setup. Bound from env vars under[/] [aqua]CLYPSE_SETUP[/][grey].[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(BuildOptionsTable(options));
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[aqua]Select an action[/]")
                    .PageSize(12)
                    .AddChoices(
                        "Edit BaseUrl",
                        "Edit AccessId",
                        "Edit SecretAccessKey",
                        "Edit Region",
                        "Edit ResourcePrefix",
                        "Edit PortalBuildOutputPath",
                        "Save options",
                        "Continue",
                        "Cancel"));

            switch (choice)
            {
                case "Edit BaseUrl":
                    options.BaseUrl = AnsiConsole.Prompt(
                        new TextPrompt<string>("[green]BaseUrl[/] (optional)")
                            .DefaultValue(options.BaseUrl ?? string.Empty)
                            .AllowEmpty());
                    break;

                case "Edit AccessId":
                    options.AccessId = AnsiConsole.Prompt(
                        new TextPrompt<string>("[green]AccessId[/]")
                            .DefaultValue(options.AccessId ?? string.Empty)
                            .AllowEmpty());
                    break;

                case "Edit SecretAccessKey":
                    {
                        var updated = AnsiConsole.Prompt(
                            new TextPrompt<string>("[green]SecretAccessKey[/] ([grey]leave blank to keep current[/])")
                                .Secret()
                                .AllowEmpty());

                        if (!string.IsNullOrEmpty(updated))
                        {
                            options.SecretAccessKey = updated;
                        }

                        break;
                    }

                case "Edit Region":
                    options.Region = AnsiConsole.Prompt(
                        new TextPrompt<string>("[green]Region[/]")
                            .DefaultValue(options.Region ?? string.Empty)
                            .AllowEmpty());
                    break;

                case "Edit ResourcePrefix":
                    options.ResourcePrefix = AnsiConsole.Prompt(
                        new TextPrompt<string>("[green]ResourcePrefix[/]")
                            .DefaultValue(options.ResourcePrefix ?? string.Empty)
                            .AllowEmpty());
                    break;

                case "Edit PortalBuildOutputPath":
                    options.PortalBuildOutputPath = AnsiConsole.Prompt(
                        new TextPrompt<string>("[green]PortalBuildOutputPath[/]")
                            .DefaultValue(options.PortalBuildOutputPath ?? string.Empty)
                            .AllowEmpty());
                    break;

                case "Save options":
                    {
                        var confirm = AnsiConsole.Confirm(
                            "This will persist values as [aqua]user environment variables[/] under [aqua]CLYPSE_SETUP__*[/].\n\n[yellow]This includes SecretAccessKey.[/] Continue?",
                            defaultValue: false);

                        if (!confirm)
                        {
                            break;
                        }

                        var saved = TrySaveOptionsToUserEnvironment(options, out var message);
                        if (saved)
                        {
                            AnsiConsole.MarkupLine($"[green]{Markup.Escape(message)}[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]{Markup.Escape(message)}[/]");
                        }

                        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
                        Console.ReadKey(true);
                        break;
                    }

                case "Continue":
                    if (!options.IsValid())
                    {
                        AnsiConsole.MarkupLine("[red]Options are not valid.[/] Please set [yellow]AccessId[/], [yellow]SecretAccessKey[/], [yellow]Region[/], and [yellow]ResourcePrefix[/].");
                        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
                        Console.ReadKey(true);
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(options.PortalBuildOutputPath))
                    {
                        var proceedWithoutBuild = AnsiConsole.Confirm(
                            "[yellow]PortalBuildOutputPath is empty.[/] Upload may fail. Continue anyway?",
                            defaultValue: false);

                        if (!proceedWithoutBuild)
                        {
                            break;
                        }
                    }

                    return true;

                case "Cancel":
                    return false;
            }
        }
    }

    private static Table BuildOptionsTable(AwsServiceOptions options)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[grey]Option[/]").LeftAligned())
            .AddColumn(new TableColumn("[grey]Value[/]").LeftAligned());

        table.AddRow("[blue]BaseUrl[/]", Markup.Escape(options.BaseUrl ?? string.Empty));
        table.AddRow("[blue]AccessId[/]", Markup.Escape(options.AccessId ?? string.Empty));
        table.AddRow("[blue]SecretAccessKey[/]", Markup.Escape((options.SecretAccessKey ?? string.Empty).Redact(3)));
        table.AddRow("[blue]Region[/]", Markup.Escape(options.Region ?? string.Empty));
        table.AddRow("[blue]ResourcePrefix[/]", Markup.Escape(options.ResourcePrefix ?? string.Empty));
        table.AddRow("[blue]PortalBuildOutputPath[/]", Markup.Escape(options.PortalBuildOutputPath ?? string.Empty));
        table.AddEmptyRow();
        table.AddRow(
            "[grey]InteractiveMode[/]",
            options.InteractiveMode ? "[green]true[/]" : "[red]false[/]");

        return table;
    }

    private static bool TrySaveOptionsToUserEnvironment(AwsServiceOptions options, out string message)
    {
        try
        {
            var target = OperatingSystem.IsWindows()
                ? EnvironmentVariableTarget.User
                : EnvironmentVariableTarget.Process;

            SetEnv("CLYPSE_SETUP__BaseUrl", options.BaseUrl, target);
            SetEnv("CLYPSE_SETUP__AccessId", options.AccessId, target);
            SetEnv("CLYPSE_SETUP__SecretAccessKey", options.SecretAccessKey, target);
            SetEnv("CLYPSE_SETUP__Region", options.Region, target);
            SetEnv("CLYPSE_SETUP__ResourcePrefix", options.ResourcePrefix, target);
            SetEnv("CLYPSE_SETUP__PortalBuildOutputPath", options.PortalBuildOutputPath, target);

            message = OperatingSystem.IsWindows()
                ? "Saved. Next run will load values from your user environment."
                : "Saved for the current process only (non-Windows OS cannot persist user env vars via .NET).";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Failed to save environment variables: {ex.Message}";
            return false;
        }
    }

    private static void SetEnv(string key, string? value, EnvironmentVariableTarget target)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value;
        Environment.SetEnvironmentVariable(key, normalized, target);
    }
}
