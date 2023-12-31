using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MetalPriceConsole.Models;
using Microsoft.Extensions.Logging;
using PublicHoliday;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MetalPriceConsole.Commands;

public class HistoryCommand : AsyncCommand<HistoryCommand.Settings>
{
    private readonly string _connectionString;
    private readonly ApiServer _apiServer;
    private readonly ILogger _logger;
    private static readonly string[] columns = new[] { "" };

    public HistoryCommand(ConnectionStrings ConnectionString, ApiServer apiServer, ILogger<AccountCommand> logger)
    {
        _connectionString = ConnectionString.DefaultDB;
        _apiServer = apiServer;
        _logger = logger;
    }

    public class Settings : CommandSettings
    {
        [CommandOption("--start <startdate>")]
        [Description("Start Date.")]
        public string StartDate { get; set; }

        [CommandOption("--end <enddate>")]
        [Description("End Date")]
        public string EndDate { get; set; }

        [CommandOption("--currency <USD>")]
        [Description("Specify The Currency")]
        [DefaultValue("")]
        public string Currency { get; set; }

        [CommandOption("--gold")]
        [Description("Get Gold Price - This is the default and is optional")]
        [DefaultValue(true)]
        public bool GetGold { get; set; }

        [CommandOption("--palladium")]
        [Description("Get Palladium Price")]
        [DefaultValue(false)]
        public bool GetPalladium { get; set; }   

        [CommandOption("--platinum")]
        [Description("Get Platinum Price")]
        [DefaultValue(false)]
        public bool GetPlatinum { get; set; }   

        [CommandOption("--silver")]
        [Description("Get Silver Price")]
        [DefaultValue(false)]
        public bool GetSilver { get; set; }

        [CommandOption("--debug")]
        [Description("Enable Debug Output")]
        [DefaultValue(false)]
        public bool Debug { get; set; }

        [CommandOption("--hidden")]
        [Description("Enable Secret Debug Output")]
        [DefaultValue(false)]
        public bool ShowHidden { get; set; }

        [CommandOption("--save")]
        [Description("Save Results")]
        [DefaultValue(false)]
        public bool Save { get; set; }

        [CommandOption("--cache")]
        [Description("Cache Results To File")]
        [DefaultValue(false)]
        public bool Cache { get; set; }

        [CommandOption("--fake")]
        [Description("Does Not Call WebApi")]
        [DefaultValue(false)]
        public bool Fake { get; set; }
    }
    public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        if (settings.Currency.Length == 0)
            settings.Currency = _apiServer.Currency;
        else
            settings.Currency += "/";    

        var warning = new Table().Centered();
        warning.AddColumn(
            new TableColumn(
                new Markup(
                    $"[red bold]API does not suppport historical data for Palladium or Platinum[/]"
                )
            ).Centered()
        );

        string url = _apiServer.BaseUrl;
        if (settings.GetSilver)
        {
            url += _apiServer.Silver;
            settings.GetGold = false;
        }
// Issue with web API using date which is needed for history
        else if (settings.GetPalladium)
        {
            url += _apiServer.Palladium;
            settings.GetGold = false;
            AnsiConsole.Write(warning);
            return Task.FromResult(-1);
        }
        else if (settings.GetPlatinum)
        {
            url += _apiServer.Platinum;
            settings.GetGold = false;
            AnsiConsole.Write(warning);
            return Task.FromResult(-1);
        }
        else
            url += _apiServer.Gold;
        url += $"/{settings.Currency}";
        if (settings.Debug)
        {
            DebugDisplay.Print(settings, _apiServer, url);
        }
        AnsiConsole.WriteLine();
        // Process Window
        var table = new Table().Centered();
        table.BorderColor(Color.Yellow);
        table.Border(TableBorder.Rounded);
        table.Border(TableBorder.Simple);
        table.AddColumns(columns);
        table.Expand();

        // Animate
        AnsiConsole
            .Live(table)
            .AutoClear(false)
            .Overflow(VerticalOverflow.Ellipsis)
            .Cropping(VerticalOverflowCropping.Top)
            .StartAsync(async ctx =>
            {
                void Update(int delay, Action action)
                {
                    action();
                    ctx.Refresh();
                    Thread.Sleep(delay);
                }
                string metal = "Gold";
                if (settings.GetSilver)
                    metal = "Silver";
                Update(
                    70,
                    () =>
                        table.Columns[0].Footer(
                            $"[red bold]Status[/] [green bold]Retrieving {metal} Prices For {settings.StartDate} To {settings.EndDate}[/]"
                        )
                );
                List<DateTime> days = GetNumberOfDays(settings.StartDate, settings.EndDate);

                if (days.Count < 1)
                {
                    Update(
                        70,
                        () =>
                            table.AddRow(
                                $":check_mark: [green bold]There are {days.Count} Days To Process...[/]"
                            )
                    );
                    Update(
                        70,
                        () =>
                            table.Columns[0].Footer(
                                $"[red bold]Status[/] [green bold]Completed, Nothing Processed. Holiday Or Weekend.[/]"
                            )
                    );
                    return;
                }

                Account account;
                HttpClient client = new();
                client.DefaultRequestHeaders.Add("x-access-token", _apiServer.Token);
                using (HttpRequestMessage request = new(HttpMethod.Get, $"{_apiServer.BaseUrl}stat"))
                {
                    try
                    {
                        HttpResponseMessage response = await client.SendAsync(request);
                        response.EnsureSuccessStatusCode();
                        var result = await response.Content.ReadAsStreamAsync();
                        account = await JsonSerializer.DeserializeAsync<Account>(result);
                    }
                    catch (Exception ex)
                    {
                        Update(70, () => table.AddRow($"[red]Error: {ex.Message}[/]", $"[red]Calling Url: {_apiServer.BaseUrl}stat[/]"));
                        return;
                    }
                }
                _ = int.TryParse(_apiServer.MonthlyAllowance, out int monthlyAllowance);
                var willBeLeft = (monthlyAllowance - account.RequestsMonth) - days.Count;
                if (willBeLeft > 0)
                {
                    Update(
                        70,
                        () =>
                            table.AddRow(
                                $":check_mark: [green bold]Will Leave {willBeLeft} Calls After Running...[/]"
                            )
                    );
                    foreach (DateTime date in days)
                    {
                        int i = 0;
                        Update(
                            70,
                            () =>
                                table.AddRow(
                                    $":plus: [red bold]Retrieving {metal} Price for {date:yyyy-MM-dd}...[/]"
                                )
                        );
                        MetalPrice metalPrice;
                        if (!settings.Fake)
                        {
                            // The API doesn't like date with Palladium or Platinum for some reason
                            // even though the documents show it should.
                            using HttpRequestMessage request = new(HttpMethod.Get, $"{url}/{date:yyyy-MM-dd}");
                            try
                            {
                                HttpResponseMessage response = await client.SendAsync(request);
                                response.EnsureSuccessStatusCode();
                                var result = await response.Content.ReadAsStreamAsync();
                                metalPrice = await JsonSerializer.DeserializeAsync<MetalPrice>(result);
                                if(settings.Cache)
                                    Database.CacheData(metalPrice, _apiServer.CacheFile);
                            }
                            catch (Exception ex)
                            {
                                Update(70, () => table.AddRow($"[red]Error: {ex.Message}[/]", $"[red]Calling Url: {_apiServer.BaseUrl}stat[/]"));
                                return;
                            }
                         }
                        else
                        {
                            // We need to change this outside of the loop. Bad logic loading this multiple times
                            List<MetalPrice> metalPrices;
                            string cache = await File.ReadAllTextAsync("MultiDay.sample");
                            using (MemoryStream stream = new(Encoding.UTF8.GetBytes(cache)))
                            {
                                metalPrices = await JsonSerializer.DeserializeAsync<List<MetalPrice>>(stream);
                            }
                            if (i == days.Count)
                                i = 0;
                            else
                                i++;
                            metalPrice = metalPrices[i];
                        }
                        if (metalPrice != null)
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
                            if (settings.Save)
                            {
                                Update(
                                    70,
                                    () =>
                                        table.AddRow(
                                            $":plus: [red bold]Adding Data To Database...[/]"
                                        )
                                );
                                if (Database.Save(metalPrice, _connectionString, _apiServer.CacheFile))
                                {
                                    Update(
                                        70,
                                        () =>
                                            table.AddRow(
                                                $":check_mark: [green bold]Saved {metal} Price For {metalPrice.Date:yyyy-MM-dd}...[/]"
                                            )
                                    );
                                }
                                else
                                {
                                    // Caching is taken care of in the Save method on failure
                                    Update(
                                        70,
                                        () =>
                                            table.AddRow(
                                                $":stop_sign: [red bold]Could Not Save {metal} Price For {metalPrice.Date:yyyy-MM-dd}...[/]"
                                            )
                                    );
                                }
                            }
                        }
                        // More rows than we want?
                        if (table.Rows.Count > Console.WindowHeight - 15)
                        {
                            // Remove the first one
                            table.Rows.RemoveAt(0);
                        }
                    }
                }
                else
                {
                    Update(
                        70,
                        () =>
                            table.AddRow(
                                $":bomb: [red bold italic]There are not enough API Calls left...[/]"
                            )
                    );
                    Update(
                        70,
                        () =>
                            table.Columns[0].Footer(
                                $"[red bold]Status[/] [red bold italic]Aborting Retrieving {metal} Price For {settings.StartDate} To {settings.EndDate}[/]"
                            )
                    );
                    return;
                }
                Update(70, () => table.Columns[0].Footer("[blue]Complete[/]"));
            });
        return Task.FromResult(0);
    }
    private static List<DateTime> GetNumberOfDays(string startDate, string endDate)
    {
        DateTime start = DateTime.Parse(startDate);
        DateTime end = DateTime.Parse(endDate);
        List<DateTime> dates = new();
        var res = DateTime.Compare(end, DateTime.Now);

        // We do not want the end date to be the current date or future date.
        if (res > 0)
            end = DateTime.Now.AddDays(-1);

        while (start <= end)
        {
            bool isHoliday = new USAPublicHoliday().IsPublicHoliday(start);
            if (!isHoliday && start.DayOfWeek != DayOfWeek.Saturday && start.DayOfWeek != DayOfWeek.Sunday)
                dates.Add(start);
            start = start.AddDays(1);
        }
        return dates;
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (!DateTime.TryParse(settings.EndDate, out _))
            return ValidationResult.Error($"Invalid end date - {settings.EndDate}");
        if (!DateTime.TryParse(settings.StartDate, out _))
            return ValidationResult.Error($"Invalid start date - {settings.StartDate}");
        return base.Validate(context, settings);
    }
}
