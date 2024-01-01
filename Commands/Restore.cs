using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
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

    public RestoreCommand(ConnectionStrings ConnectionString, ApiServer apiServer)
    {
        _connectionString = ConnectionString.DefaultDB;
        _apiServer = apiServer;
    }

    public class Settings : BaseCommandSettings
    {
        // There are no special settings for this command

    }
    public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        if (settings.Debug)
        {
            DebugDisplay.Print(settings, _apiServer, _connectionString, "N/A");
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
                string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string file = Path.Combine(path, _apiServer.CacheFile);
                // Content
                if (!File.Exists(file))
                { 
                    Update(70, () => table.AddRow($"[red]No Cache File Exists. Exiting.[/]"));
                    Update(70, () => table.Columns[0].Footer("[blue]No Cache File To Process. Finishing.[/]"));
                    return;
                }
                Update(70, () => table.AddRow($":hourglass_not_done: [yellow]Loading [/][green]{_apiServer.CacheFile}[/]"));
                string cache = File.ReadAllText(file);
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
                    Update(
                        70,
                        () =>
                            table.AddRow(
                                $"      :check_mark: [green bold italic]Current Ounce Price: {metalPrice.Price:C} Previous Ounce Price: {metalPrice.PrevClosePrice:C}[/]"
                            )
                    );
                    Update(
                        70,
                        () =>
                            table.AddRow(
                                $"           :check_mark: [green bold italic] 24k gram: {metalPrice.PriceGram24k:C} 22k gram: {metalPrice.PriceGram22k:C} 21k gram: {metalPrice.PriceGram21k:C} 20k gram: {metalPrice.PriceGram20k:C} 18k gram: {metalPrice.PriceGram18k:C}[/]"
                            )
                    );
                    if (Database.Save(metalPrice, _connectionString, _apiServer.CacheFile))
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
                                $":minus: [red bold]Removing Cache File {file}[/]"
                            )
                    );
                    File.Delete(file);
                }
                Update(70, () => table.Columns[0].Footer("[blue]Complete[/]"));
            });
        return Task.FromResult(0);
    }
}