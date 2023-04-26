using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security.Principal;
using System.Threading;
using GoldPriceConsole.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PublicHoliday;
using RestSharp;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GoldPriceConsole.Commands;

public class PriceCommand : Command<PriceCommand.Settings>
{
    private readonly ApiServer _apiServer;
    private readonly string _connectionString;
    private ILogger _logger;

    public PriceCommand(ApiServer apiServer, ILogger<PriceCommand> logger, ConnectionStrings connectionStrings)
    {
        _apiServer = apiServer;
        _logger = logger;
        _connectionString = connectionStrings.DefaultDB;
    }

    public class Settings : CommandSettings
    {
        [Description("Get Current Price.")]
        [DefaultValue(false)]
        public bool GetPrice { get; set; }

        [CommandOption("--date <date>")]
        [Description("Date To Get Price For")]
        public string Date { get; set; }

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
        if (settings.Date == null)
            settings.Date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        settings.GetPrice = true;
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
                Update(
                    70,
                    () =>
                        table.Columns[0].Footer(
                            $"[red bold]Status[/] [green bold]Retrieving Gold Price For {settings.Date}[/]"
                        )
                );
                int day = 0;
                DateTime date = DateTime.Parse(settings.Date);
                bool isHoliday = new USAPublicHoliday().IsPublicHoliday(date);
                if (!isHoliday && date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                    day = 1;
                Update(
                   70,
                   () =>
                       table.AddRow(
                           $":check_mark: [green bold]Calculated {day} Days To Get Gold Prices For...[/]"
                       )
                );
                if (day < 1)
                {
                    Update(
                        70,
                        () =>
                            table.AddRow(
                                $":check_mark: [red bold]There are {day} Days To Process...[/]"
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
                var willBeLeft = (monthlyAllowance - account.requests_month) - day;
                if (willBeLeft > 0)
                {
                    Update(
                        70,
                        () =>
                            table.AddRow(
                                $":check_mark: [green bold]Will Leave {willBeLeft} Calls After Running...[/]"
                            )
                    );
                    Update(
                        70,
                        () =>
                            table.AddRow(
                                $":plus: [red bold]Retrieving Gold Price for {settings.Date}...[/]"
                            )
                    );
                    if (!isHoliday && date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                    {
                        GoldPrice goldPrice;
                        if (!settings.Fake)
                        {
                            client = new RestClient(_apiServer.BaseUrl + _apiServer.DefaultMetal + settings.Date);
                            request = new RestRequest("", Method.Get);
                            request.AddHeader("x-access-token", _apiServer.Token);
                            request.AddHeader("Content-Type", "application/json");
                            RestResponse response = client.Execute(request);
                            goldPrice = JsonConvert.DeserializeObject<GoldPrice>(response.Content);
                        }
                        else
                        {
                            string cache = File.ReadAllText("SingleDay.sample");
                            List<GoldPrice> goldPrices = JsonConvert.DeserializeObject<List<GoldPrice>>(cache);
                            goldPrice = goldPrices[0];
                        }
                        if (goldPrice != null)
                        {
                            Update(
                                70,
                                () =>
                                    table.AddRow(
                                        $":check_mark: [green bold italic]Current Ounce Price: {goldPrice.price:C} Previous Ounce Price: {goldPrice.prev_close_price:C}[/]"
                                    )
                            );
                            Update(
                                70,
                                () =>
                                    table.AddRow(
                                        $"           :check_mark: [green bold italic] 24k gram: { goldPrice.price_gram_24k:C} 22k gram: { goldPrice.price_gram_22k:C} 21k gram: { goldPrice.price_gram_21k:C} 20k gram: { goldPrice.price_gram_20k:C} 18k gram: { goldPrice.price_gram_18k:C}[/]"
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
                                $"[red bold]Status[/] [red bold italic]Aborting Retrieving Gold Price For {settings.Date}...[/]"
                            )
                    );
                    return;
                }
                Update(70, () => table.Columns[0].Footer("[blue]Complete[/]"));
            });
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        DateTime date;
        if (!DateTime.TryParse(settings.Date, out date))
            return ValidationResult.Error($"Invalid date - {settings.Date}");
        return base.Validate(context, settings);
    }
}
