using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.IO;
using System.Text.Json;
using System.Threading;
using MetalPriceConsole.Models;
using Microsoft.Extensions.Logging;
using PublicHoliday;
using RestSharp;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MetalPriceConsole.Commands;

public class HistoryCommand : Command<HistoryCommand.Settings>
{
    private readonly string _connectionString;
    private readonly ApiServer _apiServer;
    private ILogger _logger;

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

        [CommandOption("--silver")]
        [Description("Get Silver Price")]
        [DefaultValue(false)]
        public bool GetSilver { get; set; }

        [CommandOption("--gold")]
        [Description("Get Gold Price - This is the default and is optional")]
        [DefaultValue(true)]
        public bool GetGold { get; set; }

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

        [CommandOption("--fake")]
        [Description("Does Not Call WebApi")]
        [DefaultValue(false)]
        public bool Fake { get; set; }
    }
    public override int Execute(CommandContext context, Settings settings)
    {
        if (settings.Debug)
        {
            DebugDisplay.Print(settings, _apiServer, _logger, _connectionString);
        }
        if (settings.GetSilver)
        {
            settings.GetGold = false;
            settings.GetSilver = true;
        }
        if (settings.Currency.Length == 0)
            settings.Currency = _apiServer.Currency;
        else
            settings.Currency += "/";    

        if (settings.Debug)
        {
            DebugDisplay.Print(settings, _apiServer, _logger);
        }
        AnsiConsole.WriteLine();
        // Process Window
        var table = new Table().Centered();
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

                var client = new RestClient(_apiServer.BaseUrl + "stat");
                var request = new RestRequest("", Method.Get);
                request.AddHeader("x-access-token", _apiServer.Token);
                request.AddHeader("Content-Type", "application/json");
                Account account;
                try
                {
                    RestResponse response = client.Execute(request);
                    account = JsonSerializer.Deserialize<Account>(response.Content);
                }
                catch (Exception ex)
                {
                    Update(70, () => table.AddRow($"[red]Error: {ex.Message}[/]", $"[red]Calling Url: {_apiServer.BaseUrl}stat[/]"));
                    return;
                }
                int.TryParse(_apiServer.MonthlyAllowance, out int monthlyAllowance);
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
                                    $":plus: [red bold]Retrieving {metal} Price for {date.ToString("yyyy-MM-dd")}...[/]"
                                )
                        );
                        MetalPrice metalPrice;
                        if (!settings.Fake)
                        {
                            string url = _apiServer.BaseUrl;
                            if (settings.GetSilver)
                                url += _apiServer.Silver;
                            else
                                url += _apiServer.Gold;
                            client = new RestClient(url + settings.Currency + date.ToString("yyyy-MM-dd"));
                            request = new RestRequest("", Method.Get);
                            request.AddHeader("x-access-token", _apiServer.Token);
                            request.AddHeader("Content-Type", "application/json");
                            RestResponse response = client.Execute(request);
                            metalPrice = JsonSerializer.Deserialize<MetalPrice>(response.Content);
                        }
                        else
                        {
                            string cache = File.ReadAllText("MultiDay.sample");
                            List<MetalPrice> metalPrices = JsonSerializer.Deserialize<List<MetalPrice>>(cache);
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
                                if (Database.Save(metalPrice, _connectionString))
                                {
                                    Update(
                                        70,
                                        () =>
                                            table.AddRow(
                                                $":check_mark: [green bold]Saved {metal} Price For {metalPrice.Date.ToString("yyyy-MM-dd")}...[/]"
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
                                                $":stop_sign: [red bold]Could Not Save {metal} Price For {metalPrice.Date.ToString("yyyy-MM-dd")}...[/]"
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
        return 0;
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
