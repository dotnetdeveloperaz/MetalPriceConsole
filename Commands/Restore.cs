using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MetalPriceConsole.Commands.Settings;
using MetalPriceConsole.Models;
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
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        if (settings.Debug)
        {
            if (!DebugDisplay.Print(settings, _apiServer, "N/A"))
                return 0;
        }
        // We will modify this to use the cache file switch. Going to make this a different issue.
        //string file = Path.Combine("", settings.CacheFile);
        // Process Window
        var table = new Table().Centered();
        table.HideHeaders();
        table.BorderColor(Color.Yellow);
        table.Border(TableBorder.Rounded);
        table.AddColumns(new[] { "" });
        table.Expand();

        // Animate
        await AnsiConsole
            .Live(table)
            .AutoClear(false)
            .Overflow(VerticalOverflow.Ellipsis)
            .Cropping(VerticalOverflowCropping.Top)
            .Start(async ctx =>
            {
                void Update(int delay, Action action)
                {
                    action();
                    ctx.Refresh();
                    Thread.Sleep(delay);
                }
                Update(70, () => table.AddRow($"[red bold]Status[/] [green bold]Checking For Cache File[/]"));
                string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string file = Path.Combine(path, _apiServer.CacheFile);
                // Content

                if (!File.Exists(file))

                { 
                    Update(70, () => table.AddRow($"[red]No Cache File Exists ({file}). Exiting.[/]"));
                    return;
                }

                Update(70, () => table.AddRow($"[yellow]Loading [/][green]{file}[/]"));
                string cache;
                using (StreamReader sr = new StreamReader(file)) 
                { 
                    cache = sr.ReadToEnd();
                }
                List<MetalPrice> metalPrices = await JsonSerializer.DeserializeAsync<List<MetalPrice>>(new MemoryStream(Encoding.UTF8.GetBytes(cache)));

                Update(70, () => table.AddRow($"[yellow]Cache File Loaded[/] [green]{metalPrices.Count} Records Loaded[/]"));
                Update(70, () => table.Columns[0].Footer("[blue]Cache File Loaded[/]"));

                int saved = 0;
                foreach (MetalPrice metalPrice in metalPrices)
                {
                    Update(
                        70,
                        () =>
                            table.AddRow(
                                $"      [green bold italic]Current Ounce Price: {metalPrice.Price:C} Previous Ounce Price: {metalPrice.PrevClosePrice:C}[/]"
                            )
                    );
                    Update(
                        70,
                        () =>
                            table.AddRow(
                                $"           [green bold italic] 24k gram: {metalPrice.PriceGram24k:C} 22k gram: {metalPrice.PriceGram22k:C} 21k gram: {metalPrice.PriceGram21k:C} 20k gram: {metalPrice.PriceGram20k:C} 18k gram: {metalPrice.PriceGram18k:C}[/]"
                            )
                    );
                    if (Database.Save(metalPrice, _connectionString, _apiServer.CacheFile))
                    {
                        Update(
                            70,
                            () =>
                                table.AddRow(
                                    $"[green bold]Saved Gold Price For {metalPrice.Date:yyyy-MM-dd}...[/]"
                                )
                        );
                        saved++;
                    }
                    // More rows than we can display?
                    if (table.Rows.Count > Console.WindowHeight - 30)
                    {
                        // Remove the first one
                        table.Rows.RemoveAt(0);
                    }

                }
                if (saved == metalPrices.Count)
                {
                    Update(
                        70,
                        () =>
                            table.AddRow(
                                $"[red bold]Removing Cache File {file}[/]"
                            )
                    );

                    File.Delete(file);
                }
                Update(70, () => table.AddRow("[blue]Complete[/]"));
            });
        return 0;
    }
}