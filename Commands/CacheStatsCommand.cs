using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MetalPriceConsole.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using MetalPriceConsole.Commands.Settings;
using System.Linq;

namespace MetalPriceConsole.Commands
{
    public class CacheStatsCommand : AsyncCommand<CacheStatsCommand.Settings>
    {
        private readonly ApiServer _apiServer;
        public CacheStatsCommand(ApiServer apiServer)
        {
            _apiServer = apiServer;
        }

        public class Settings : BaseCommandSettings
        {
            [CommandOption("--cachefile")]
            [Description("Cache File to Use - Override Default")]
            [DefaultValue(null)]
            public string CacheFile { get; set; } = null;

        }
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            settings.CacheFile ??= _apiServer.CacheFile;
            if (settings.Debug)
            {
                if (!DebugDisplay.Print(settings, _apiServer, "N/A"))
                    return 0;
            }
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
                    string file = Path.Combine(path, settings.CacheFile);
                    // Content

                    if (!File.Exists(file))

                    {
                        Update(70, () => table.AddRow($"[red]No Cache File Exists ({file}). Exiting.[/]"));
                        return;
                    }

                    Update(70, () => table.AddRow($"[yellow]Loading [/][green]{file}[/]"));
                    string cache;
                    string goldStartDate = String.Empty;
                    string goldEndDate = String.Empty;
                    string silverStartDate = String.Empty;
                    string silverEndDate = string.Empty;
                    string platinumStartDate = String.Empty;
                    string platinumEndDate = String.Empty;
                    string palladiumStartDate = String.Empty;
                    string palladiumEndDate = String.Empty;
 
                    using (StreamReader sr = new StreamReader(file))
                    {
                        cache = sr.ReadToEnd();
                    }
                    List<MetalPrice> metalPrices = await JsonSerializer.DeserializeAsync<List<MetalPrice>>(new MemoryStream(Encoding.UTF8.GetBytes(cache)));
                    Update(70, () => table.AddRow($"[yellow]Cache File Loaded[/] [green]{metalPrices.Count} Records Loaded[/]"));
                    Update(70, () => table.Columns[0].Footer("[blue]Cache File Loaded[/]"));
                    int goldCount = metalPrices.Where(x => x.Metal == "XAU").ToList().Count();
                    if (goldCount > 0)
                    {
                        var goldDateStart = metalPrices
                            .Where(x => x.Metal == "XAU")
                            .OrderBy(x => x.Date)
                            .FirstOrDefault();
                        goldStartDate = goldDateStart.Date.ToString("yyyy-MM-dd");
                        var goldDateEnd = metalPrices
                            .Where(x => x.Metal == "XAU")
                            .OrderByDescending(x => x.Date)
                            .FirstOrDefault();
                        goldEndDate = goldDateEnd.Date.ToString("yyyy-MM-dd");
                    }
                    int silverCount = metalPrices.Where(x => x.Metal == "XAG").ToList().Count();
                    if (silverCount > 0)
                    {
                        var silverDateStart = metalPrices
                            .Where(x => x.Metal == "XAG")
                            .OrderBy(x => x.Date)
                            .FirstOrDefault();
                        silverStartDate = silverDateStart.Date.ToString("yyyy-MM-dd");
                        var silverDateEnd = metalPrices
                            .Where(x => x.Metal == "XAG")
                            .OrderByDescending(x => x.Date)
                            .FirstOrDefault();
                        silverEndDate = silverDateEnd.Date.ToString("yyyy-MM-dd");
                    }
                    int platinumCount = metalPrices.Where(x => x.Metal == "XPT").ToList().Count();
                    if (platinumCount > 0)
                    {
                        var platinumDateStart = metalPrices
                            .Where(x => x.Metal == "XPT")
                            .OrderBy(x => x.Date)
                            .FirstOrDefault();
                        platinumStartDate = platinumDateStart.Date.ToString("yyyy-MM-dd");
                        var platinumDateEnd = metalPrices
                            .Where(x => x.Metal == "XPT")
                            .OrderByDescending(x => x.Date)
                            .FirstOrDefault();
                        platinumEndDate = platinumDateEnd.Date.ToString("yyyy-MM-dd");
                    }
                    int palladiumCount = metalPrices.Where(x => x.Metal == "XPD").ToList().Count();
                    if (palladiumCount > 0)
                    {
                        var palladiumDateStart = metalPrices
                            .Where(x => x.Metal == "XPD")
                            .OrderBy(x => x.Date)
                            .FirstOrDefault();
                        palladiumStartDate = palladiumDateStart.Date.ToString("yyyy-MM-dd");
                        var palladiumDateEnd = metalPrices
                            .Where(x => x.Metal == "XPD")
                            .OrderByDescending(x => x.Date)
                            .FirstOrDefault();
                        palladiumEndDate = palladiumDateEnd.Date.ToString("yyyy-MM-dd");
                    }
                    if (goldCount > 0)
                        Update(70, () => table.AddRow($"[green]     Gold Records: {goldCount}  Start: {goldStartDate}     End: {goldEndDate}[/]"));
                    if (silverCount > 0)
                        Update(70, () => table.AddRow($"[green]   Silver Records: {silverCount}  Start: {silverStartDate}     End: {silverEndDate}[/]"));
                    if (platinumCount > 0)
                        Update(70, () => table.AddRow($"[green] Platinum Records: {platinumCount}  Start: {platinumStartDate}     End: {platinumEndDate}[/]"));
                    if (palladiumCount > 0)
                        Update(70, () => table.AddRow($"[green]Palladium Records: {palladiumCount}  Start: {palladiumStartDate}     End: {palladiumEndDate}[/]"));
                    Update(70, () => table.Columns[0].Footer("[blue]Complete[/]"));
                });
            return 0;
        }
    }
}