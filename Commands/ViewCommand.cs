using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MetalPriceConsole.Commands.Settings;
using MetalPriceConsole.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MetalPriceConsole.Commands;

public class ViewCommand : BasePriceCommand<ViewCommand.Settings>
{
    private readonly ConnectionStrings _connectionStrings;
    private readonly ApiServer _apiServer;
    private string _defaultDB;

    public ViewCommand(ApiServer apiServer, ConnectionStrings connectionStrings) : base(apiServer, connectionStrings)
    {
        _apiServer = apiServer;
        _connectionStrings = connectionStrings;
    }

    public class Settings : PriceCommandSettings
    {
        // There are no special settings for this command

    }
    protected override async Task<int> ExecuteDerivedAsync(CommandContext context, PriceCommandSettings settings)
    {
        string file = Path.Combine("", settings.CacheFile);
        // We want to support multiple metals for view, so let's not use base which is singular
        //string metalName = base.MetalName;
        //string metal = base.Metal;
        string metalName = string.Empty;
        string metal = string.Empty;
        if (!settings.GetAll)
        {
            metalName += settings.GetGold ? "[yellow]Gold[/] " : string.Empty;
            metal += settings.GetGold ? _apiServer.Gold : string.Empty;
            metalName += settings.GetSilver ? "[silver]Silver[/] " : string.Empty;
            metal += settings.GetSilver ? _apiServer.Silver : string.Empty;
            metalName += settings.GetPalladium ? "[grey]Palladium[/] " : string.Empty;
            metal += settings.GetPalladium ? _apiServer.Palladium : string.Empty;
            metalName += settings.GetPlatinum ? "[grey100]Platinum[/] " : string.Empty;
            metal += settings.GetPlatinum ? _apiServer.Platinum : string.Empty;
        }
        else
        {
            metalName = "[yellow]Gold[/] [silver]Silver[/] [grey]Palladium[/] [grey100]Platinum[/] ";
            metal = _apiServer.Gold + _apiServer.Silver + _apiServer.Palladium + _apiServer.Platinum;
        }

        _defaultDB = _connectionStrings.DefaultDB;

        // Default to show all data for any date
        DateTime startDate = settings.StartDate == string.Empty ? DateTime.MinValue : DateTime.Parse(settings.StartDate);
        // Ensure we're setting for the latest time for the date, so add a date which will be midnight and subtract 1 second.
        // We could just leave the date after adding a day and the filter would work, but we display the date as well which
        // would show a day later than what the user specifies.
        DateTime endDate = settings.EndDate == string.Empty ? DateTime.MaxValue : DateTime.Parse(settings.EndDate).AddDays(1).AddSeconds(-1);
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
                if (settings.Cache)
                {
                    Update(70, () => table.AddRow($"[red bold]Status[/] [green bold]Checking For Cache File[/]"));
                    if (!File.Exists(file))
                    {
                        Update(70, () => table.AddRow($"[red]No Cache File Exists ({file}). Exiting.[/]"));
                        return;
                    }

                    Update(70, () => table.AddRow($"[yellow]Loading Cache File From [/][green]{file}[/]"));
                    string cache;
                    using (StreamReader sr = new StreamReader(file))
                    {
                        cache = sr.ReadToEnd();
                    }
                    List<MetalPrice> tmpPrices = await JsonSerializer.DeserializeAsync<List<MetalPrice>>(new MemoryStream(Encoding.UTF8.GetBytes(cache)));

                    metalPrices = tmpPrices
                    .Where(x => metal.Contains(x.Metal) && x.Currency == settings.Currency && x.Date >= startDate && x.Date <= endDate).ToList();

                    Update(70, () => table.AddRow($"[yellow]Cache File Loaded[/] [green]{metalPrices.Count} of {tmpPrices.Count} Records For {metalName}In Currency {settings.Currency}[/]"));
                    Update(70, () => table.Columns[0].Footer("[blue]Cache File Loaded[/]"));
                }
                else
                {
                    Update(70, () => table.AddRow($"[red bold]Retrieving {metalName}Metals Data From Database From {settings.StartDate} to {settings.EndDate}[/]"));
                    metalPrices = Database.GetData(_defaultDB, metal, settings);
                    Update(70, () => table.AddRow($"[green bold]Finished Retrieving {metalPrices.Count} Rows Of Data[/]"));
                }
                if (metalPrices == null)
                    return;
                int rowCnt = 0;
                Update(70, () => table.AddRow($"[red bold]Display {metalName}Prices In {settings.Currency} From {startDate.ToShortDateString()} to {endDate.ToShortDateString()}[/]"));
/*
                foreach (MetalPrice metalPrice in metalPrices
                    .Where(x => metal.Contains(x.Metal) && x.Currency == settings.Currency && x.Date >= startDate && x.Date <= endDate)
                    .OrderBy(x => x.Date).ThenBy(x => x.Metal))
*/
                foreach (MetalPrice metalPrice in metalPrices)
                {
                    rowCnt++;
                    string displayName = string.Empty;
                    displayName = metalPrice.Metal switch
                    {
                        "XAU" => "Gold",
                        "XAG" => "Silver",
                        "XPD" => "Palladium",
                        "XPT" => "Platinum",
                        _ => "Undefined",
                    };
                    Update(
                        70,
                        () =>
                            table.AddRow(
                                $"  [red]{displayName} ({metalPrice.Metal})  {metalPrice.Date.ToShortDateString()}[/]      [green bold italic]Ounce Price: {metalPrice.Price:C} Previous Ounce Price: {metalPrice.PrevClosePrice:C}[/]"
                            )
                    );
                    rowCnt++;
                    Update(
                        70,
                        () =>
                            table.AddRow(
                                $"[green bold italic]24k gram: {metalPrice.PriceGram24k:C} 22k gram: {metalPrice.PriceGram22k:C} 21k gram: {metalPrice.PriceGram21k:C} 20k gram: {metalPrice.PriceGram20k:C} 18k gram: {metalPrice.PriceGram18k:C}[/]"
                            )
                    );
                    // More rows than we can display on screen?
                    //if (table.Rows.Count > Console.WindowHeight - 10)
                    if (rowCnt > 3)
                    {
                        table.Rows.RemoveAt(0);
                        table.Rows.RemoveAt(0);
                    }
                }

                Update(70, () => table.Columns[0].Footer($"[blue]Complete. Displayed {rowCnt} of {metalPrices.Count} Days Prices[/]"));
            });
        return 0;
    }
    public override ValidationResult Validate(CommandContext context, PriceCommandSettings settings)
    {
        return base.Validate(context, settings);
    }
}