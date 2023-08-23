using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MetalPriceConsole.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MetalPriceConsole.Commands;

public class RestoreCommand : AsyncCommand<RestoreCommand.Settings>
{
    private readonly string _connectionString;
    private readonly ApiServer _apiServer;
    private readonly ILogger _logger;

    public RestoreCommand(ConnectionStrings ConnectionString, ApiServer apiServer, ILogger<AccountCommand> logger)
    {
        _connectionString = ConnectionString.DefaultDB;
        _apiServer = apiServer;
        _logger = logger;
    }

    public class Settings : CommandSettings
    {
        [Description("Restores Cache File")]
        [DefaultValue(false)]
        public bool Restore { get; set; }

        [CommandOption("--debug")]
        [Description("Enable Debug Output")]
        [DefaultValue(false)]
        public bool Debug { get; set; }

        [CommandOption("--hidden")]
        [Description("Enable Secret Debug Output")]
        [DefaultValue(false)]
        public bool ShowHidden { get; set; }
    }
    public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        settings.Restore = true;
        if (settings.Debug)
        {
            DebugDisplay.Print(settings, _apiServer, "");
        }
        // Process Window
        var table = new Table().Centered();
        table.HideHeaders();
        table.BorderColor(Color.Yellow);
        table.Border(TableBorder.Rounded);
        table.Border(TableBorder.Simple);
        table.AddColumns(new[] { "" });
        table.Expand();

        // Animate
        AnsiConsole
            .Live(table)
            .AutoClear(false)
            .Overflow(VerticalOverflow.Ellipsis)
            .Cropping(VerticalOverflowCropping.Top)
            .Start(ctx =>
            {
                void Update(int delay, Action action)
                {
                    action();
                    ctx.Refresh();
                    Thread.Sleep(delay);
                }
                Update(
                    70,
                    () =>
                        table.Columns[0].Footer(
                            $"[red bold]Status[/] [green bold]Checking For Cache File[/]"
                        )
                );

                // Content
                if(!File.Exists("MetalPrice.cache"))
                { 
                    Update(70, () => table.AddRow($"[red]No Cache File Exists. Exiting.[/]"));
                    Update(70, () => table.Columns[0].Footer("[blue]No Cache File To Process. Finishing.[/]"));
                    return;
                }
                Update(70, () => table.AddRow($":hourglass_not_done: [yellow]Loading [/][green]metalPrice.cache[/]"));
                string cache = File.ReadAllText("MetalPrice.cache");
                var metalPrices = JsonSerializer.Deserialize<List<MetalPrice>>(cache);
                table.Columns[0].LeftAligned().Width(30).PadRight(20);
                Update(70, () => table.AddRow($":check_mark: [yellow]Cache File Loaded[/] [green]{metalPrices.Count} Records Loaded[/]"));
                Update(70, () => table.Columns[0].Footer("[blue]Cache File Loaded[/]"));

                table.Columns[0].LeftAligned();
                Update(70, () => table.AddRow($":hourglass_not_done: [yellow]Saving To Database[/]"));
                Update(
                    70,
                    () =>
                        table.AddRow(
                            $":plus: [red bold]Adding Data To Database...[/]"
                        )
                );
                int saved = 0;
                foreach (MetalPrice metalPrice in metalPrices)
                {
                    if (Database.Save(metalPrice, _connectionString))
                    {
                        Update(
                            70,
                            () =>
                                table.AddRow(
                                    $":check_mark: [green bold]Saved Gold Price For {metalPrice.Date:yyyy-MM-dd}...[/]"
                                )
                        );
                        saved++;
                    }
                }
                if (saved == metalPrices.Count)
                {
                    Update(
                        70,
                        () =>
                            table.AddRow(
                                $":minus: [red bold]Removing Cache File[/]"
                            )
                    );
                    File.Delete("metalPrice.cache");
                }
                Update(70, () => table.Columns[0].Footer("[blue]Complete[/]"));
            });
        return Task.FromResult(0);
    }
}