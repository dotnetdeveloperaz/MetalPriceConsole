using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MetalPriceConsole.Models;
using MySqlConnector;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MetalPriceConsole.Commands;

public class ViewCommand : AsyncCommand<ViewCommand.Settings>
{
    private readonly string _connectionString;
    private readonly ApiServer _apiServer;

    public ViewCommand(ConnectionStrings ConnectionString, ApiServer apiServer)
    {
        _connectionString = ConnectionString.DefaultDB;
        _apiServer = apiServer;
    }

    public class Settings : PriceCommandSettings
    {
        // There are no special settings for this command

    }
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        if (settings.Currency.Length > 0)
            _apiServer.Currency = settings.Currency;
        string metalName = "Gold";
        string metal;
        if (settings.GetSilver)
        {
            metalName = "Silver";
            metal = _apiServer.Silver;
        }
        else if (settings.GetPalladium)
        {
            metalName = "Palladium";
            metal = _apiServer.Palladium;
            // Historical data is not supported yet, so we can only get the current day
            settings.StartDate = String.Empty;
            settings.EndDate = String.Empty;
        }
        else if (settings.GetPlatinum)
        {
            metalName = "Platinum";
            metal = _apiServer.Platinum;
            // Historical data is not supported yet, so we can only get one day
            settings.StartDate = String.Empty;
            settings.EndDate = String.Empty;
        }
        else
        {
            metal = _apiServer.Gold;
            settings.GetGold = true;
        }

        if (settings.Debug)
        {
            if (!DebugDisplay.Print(settings, _apiServer, "N/A"))
                return 0;
        }
        DateTime startDate = DateTime.Parse(settings.StartDate);
        DateTime endDate = DateTime.Parse (settings.EndDate);   
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
                // Content
                List<MetalPrice> metalPrices = null;
                if(settings.Cache)
                {
                    Update(70, () => table.AddRow($"[red bold]Status[/] [green bold]Checking For Cache File[/]"));
                    string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string file = Path.Combine(path, _apiServer.CacheFile);
                    if (!File.Exists(file) && settings.Cache)
                    {
                        Update(70, () => table.AddRow($"[red]No Cache File Exists ({file}). Exiting.[/]"));
                        return;
                    }

                    Update(70, () => table.AddRow($"[yellow]Loading {metalName} Data From [/][green]{file}[/]"));
                    string cache;
                    using (StreamReader sr = new StreamReader(file))
                    {
                        cache = sr.ReadToEnd();
                    }
                    metalPrices = await JsonSerializer.DeserializeAsync<List<MetalPrice>>(new MemoryStream(Encoding.UTF8.GetBytes(cache)));
                    
                    Update(70, () => table.AddRow($"[yellow]Cache File Loaded[/] [green]{metalPrices.Count} Records[/]"));
                    Update(70, () => table.Columns[0].Footer("[blue]Cache File Loaded[/]"));
                }
                else
                {
                    Update(70, () => table.AddRow($"[red bold]Retrieving {metalName} Data From Database[/]"));
                    metalPrices =  Database.GetData(_connectionString, metal, settings);
                    Update(70, () => table.AddRow("[green bold]Finished Retrieving Data[/]")); 
                }
                if (metalPrices == null)
                    return;
                int rowCnt = 0;
                foreach (MetalPrice metalPrice in metalPrices
                    .Where(x => x.Metal == metal && x.Currency == settings.Currency && x.Date >= startDate && x.Date <= endDate)
                    .OrderBy(x => x.Date))
                {
                    rowCnt++;

                    Update(
                        70,
                        () =>
                            table.AddRow(
                                $"    [red]{metalName} - {metalPrice.Date.ToShortDateString()}[/]      [green bold italic]Ounce Price: {metalPrice.Price:C} Previous Ounce Price: {metalPrice.PrevClosePrice:C}[/]"
                            )
                    );
                    Update(
                        70,
                        () =>
                            table.AddRow(
                                $"               [green bold italic] 24k gram: {metalPrice.PriceGram24k:C} 22k gram: {metalPrice.PriceGram22k:C} 21k gram: {metalPrice.PriceGram21k:C} 20k gram: {metalPrice.PriceGram20k:C} 18k gram: {metalPrice.PriceGram18k:C}[/]"
                            )
                    );
                    // More rows than we can display on screen?
                    //if (table.Rows.Count > Console.WindowHeight - 10)
                    if (rowCnt > 8)
                    {
                        table.Rows.RemoveAt(0);
                        table.Rows.RemoveAt(0);
                    }
                }

                Update(70, () => table.Columns[0].Footer($"[blue]Complete. Displayed {rowCnt} of {metalPrices.Count} Days Prices[/]"));
            });
        return 0;
    }
}