using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using GoldPriceConsole.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PublicHoliday;
using RestSharp;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GoldPriceConsole.Commands;

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
    }
    public override int Execute(CommandContext context, Settings settings)
    {
        if (settings.Debug)
        {
            DebugDisplay.Print(settings, _apiServer, _logger, _connectionString);
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
                Update(
                    70,
                    () =>
                        table.Columns[0].Footer(
                            $"[red bold]Status[/] [green bold]Retrieving Gold Prices For {settings.StartDate} To {settings.EndDate}[/]"
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
                    account = JsonConvert.DeserializeObject<Account>(response.Content);
                }
                catch (Exception ex)
                {
                    Update(70, () => table.AddRow($"[red]Error: {ex.Message}[/]", $"[red]Calling Url: {_apiServer.BaseUrl}stat[/]"));
                    return;
                }
                int monthlyAllowance = 0;
                int.TryParse(_apiServer.MonthlyAllowance, out monthlyAllowance);
                var willBeLeft = (monthlyAllowance - account.requests_month) - days.Count;
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
                        Update(
                            70,
                            () =>
                                table.AddRow(
                                    $":plus: [red bold]Retrieving Gold Price for {date.ToString("yyyy-MM-dd")}...[/]"
                                )
                        );
                        client = new RestClient(_apiServer.BaseUrl + _apiServer.DefaultMetal + date.ToString("yyyy-MM-dd"));
                        request = new RestRequest("", Method.Get);
                        request.AddHeader("x-access-token", _apiServer.Token);
                        request.AddHeader("Content-Type", "application/json");
                        RestResponse response = client.Execute(request);
                        GoldPrice goldPrice = JsonConvert.DeserializeObject<GoldPrice>(response.Content);
                        if (goldPrice != null)
                        {
                            Update(
                                70,
                                () =>
                                    table.AddRow(
                                        $":check_mark: [green bold italic]Price: {goldPrice.price:C} Previous Days Price: {goldPrice.prev_close_price:C}[/]"
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
                                if (Database.Save(goldPrice, _connectionString))
                                {
                                    Update(
                                        70,
                                        () =>
                                            table.AddRow(
                                                $":check_mark: [green bold]Saved Gold Price For {goldPrice.date.ToString("yyyy-MM-dd")}...[/]"
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
                                                $":stop_sign: [red bold]Could Not Save Gold Price For {goldPrice.date.ToString("yyyy-MM-dd")}...[/]"
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
                                $"[red bold]Status[/] [red bold italic]Aborting Retrieving Gold Price For {settings.StartDate} To {settings.EndDate}[/]"
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
        DateTime startDate;
        DateTime endDate;
        if (!DateTime.TryParse(settings.EndDate, out endDate))
            return ValidationResult.Error($"Invalid end date - {settings.EndDate}");
        if (!DateTime.TryParse(settings.StartDate, out startDate))
            return ValidationResult.Error($"Invalid start date - {settings.StartDate}");
        return base.Validate(context, settings);
    }
}
