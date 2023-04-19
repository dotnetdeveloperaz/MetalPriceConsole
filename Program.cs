using GoldPriceConsole.Commands;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Newtonsoft.Json;
using PublicHoliday;
using RestSharp;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace GoldPriceConsole;

class Program
{
    static bool getAccount = false;
    static bool getStatus = false;
    static bool getPrice = false;
    static bool isDebug = false;
    static bool showHidden = false;
    static bool doBackTrack = false;
    static bool doSave = false;
    static DateTime startDate;
    static DateTime endDate;
    static string stat = "stat";
    static string status = "status";
    static string priceDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
    static string connectionString;
    static Account _account;
    static Status _status;
    static GoldPrice _goldPrice;
    static string url;
    static string gold;
    static string token;
    static int monthlyAllowance;

    public static async Task Main(string[] args)
    {
        Console.Clear();
        var titleTable = new Table().Centered();
        titleTable.AddColumn(
            new TableColumn(
                new Markup(
                    "[yellow]Gold :pick:  Price Console[/] \r\n[green bold italic]Copyright © 2023 Scott Glasgow[/]"
                )
            ).Centered()
        );
        titleTable.BorderColor(Color.Yellow);
        titleTable.Border(TableBorder.Rounded);
        titleTable.Expand();

        AnsiConsole.Write(titleTable);

        // Configuration
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<Program>()
            .Build();
        connectionString = config.GetSection("DefaultDB").Value;
        token = config.GetSection("Token").Value;
        url = config.GetSection("BaseURL").Value;
        gold = config.GetSection("DefaultMetal").Value;
        int allowance;
        if (int.TryParse(config.GetSection("MonthlyAllowance").Value, out allowance))
            monthlyAllowance = allowance;

        // Configure Spectre Cli
        var app = new CommandApp();
        app.Configure(config =>
        {
            config.ValidateExamples();

            config
                .AddCommand<AccountCommand>("account")
                .WithDescription("Gets account information.");

            config
                .AddDelegate<BacktrackCommand.Settings>("backtrack", BackTrack)
                .WithDescription(
                    "Backtracks historical gold prices. Use --save to save to the database.\r\nWeekends and holidays are skipped because markets are closed."
                )
                .WithExample(
                    new[]
                    {
                        "backtrack",
                        "--start",
                        "YYYY-MM-DD",
                        "--end",
                        "YYYY-MM-DD",
                        "--debug",
                        "--hidden"
                    }
                );

            config
                .AddDelegate<PriceCommand.Settings>("price", GetPrice)
                .WithDescription(
                    "Gets the current gold price. Use --save to save to database. Weekends and holidays are skipped."
                )
                .WithExample(
                    new[] { "price", "--date", "YYYY-MM-DD", "--save", "--debug", "--hidden" }
                );

            config
                .AddDelegate<AccountCommand.Settings>("acct", GetAccount)
                .WithDescription("Gets Account Statistics.")
                .WithExample(new[] { "acct", "--debug", "--hidden" });

            config
                .AddDelegate<StatusCommand.Settings>("status", GetStatus)
                .WithDescription("Gets WebApi Status.")
                .WithExample(new[] { "status", "--debug", "--hidden" });

            config.PropagateExceptions();
        });
        try
        {
            await app.RunAsync(args);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return;
        }
        if (args.Length == 0)
            return;
        // Debug Window
        var table = new Table().Centered();
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

                // Columns
                Update(230, () => table.AddColumn(""));
                Update(230, () => table.AddColumn(""));

                // Column alignment
                Update(230, () => table.Columns[0].RightAligned());
                Update(230, () => table.Columns[1].RightAligned());

                // Footers
                Update(70, () => table.Columns[0].Footer("[red bold]Load Configuration Status[/]"));

                Update(400, () => table.Columns[1].Footer("[blue]Configuration loaded...[/]"));

                // Borders
                Update(230, () => table.BorderColor(Color.Yellow));
                Update(230, () => table.MinimalBorder());
                Update(230, () => table.SimpleBorder());
                Update(230, () => table.SimpleHeavyBorder());

                if (isDebug)
                {
                    // Column titles
                    Update(70, () => table.Columns[0].Header("[bold]Setting[/]"));
                    Update(70, () => table.Columns[1].Header("[bold]Value[/]"));
                    // Rows
                    Update(70, () => table.AddRow("Save", $"[yellow]{doSave}[/]"));
                    Update(
                        70,
                        () => table.AddRow("Backtrack Price History", $"[yellow]{doBackTrack}[/]")
                    );
                    if (doBackTrack)
                    {
                        Update(70, () => table.AddRow("Start Date", $"[yellow]{startDate}[/]"));
                        Update(70, () => table.AddRow("End Date", $"[yellow]{endDate}[/]"));
                    }
                    Update(70, () => table.AddRow("Get Current Price", $"[yellow]{getPrice}[/]"));
                    Update(70, () => table.AddRow("Debug", $"[yellow]{isDebug}[/]"));
                    Update(70, () => table.AddRow("Show Hidden", $"[yellow]{showHidden}[/]"));
                    Update(70, () => table.AddRow("Web API Status", $"[yellow]{getStatus}[/]"));
                    Update(
                        70,
                        () =>
                            table.AddRow(
                                "Web API Monthly Allowance",
                                $"[yellow]{monthlyAllowance}[/]"
                            )
                    );
                    Update(
                        70,
                        () => table.AddRow("Get Account Statistics", $"[yellow]{getAccount}[/]")
                    );
                    Update(70, () => table.AddRow("Web API Url", $"[yellow]{url}[/]"));
                    if (showHidden)
                    {
                        Update(
                            70,
                            () =>
                                table.AddRow(
                                    "Database Connectionstring",
                                    $"[yellow]{connectionString}[/]"
                                )
                        );
                        Update(
                            70,
                            () => table.AddRow("Web API Access Token", $"[yellow]{token}[/]")
                        );
                    }
                }
                Update(70, () => table.Columns[1].Footer("[blue]Complete[/]"));
            });
        // Process Window
        var logTable = new Table().Centered();
        logTable.Expand();

        // Animate
        await AnsiConsole
            .Live(logTable)
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

                var itemProcess = "";
                if (getAccount)
                {
                    itemProcess = "Account Information";
                }
                else if (getStatus)
                {
                    itemProcess = "WebAPI Status";
                }
                else if (doBackTrack)
                {
                    itemProcess = "BackTrack";
                }
                else if (getPrice)
                {
                    itemProcess = "Last Price";
                }
                else
                    itemProcess = "There was an error.";

                Update(230, () => logTable.AddColumn(""));
                Update(70, () => logTable.Columns[0].Header("[green bold]Progress Status[/]"));
                // Footers
                Update(
                    70,
                    () =>
                        logTable.Columns[0].Footer(
                            $"[red bold]Status[/] [green bold]Processing {itemProcess}[/]"
                        )
                );
                // Borders
                Update(230, () => logTable.BorderColor(Color.Yellow));
                Update(230, () => logTable.MinimalBorder());
                Update(230, () => logTable.SimpleBorder());
                Update(230, () => logTable.SimpleHeavyBorder());
                // Content

                Update(
                    70,
                    () =>
                        logTable.AddRow(
                            ":hourglass_not_done: [red bold]Calculating number of days...[/]"
                        )
                );

                Update(
                    70,
                    () =>
                        logTable.AddRow(
                            ":hourglass_not_done: [red bold]Verifying Account Has Enough Calls Left...[/]"
                        )
                );
                await GetAccountInformation();
                int willBeLeft = 0;
                if (doBackTrack)
                {
                    List<DateTime> days;
                    days = GetNumberOfDays(startDate, endDate);
                    Update(
                       70,
                       () =>
                           logTable.AddRow(
                               $":check_mark: [green bold]Calculated {days.Count} Days To Get Gold Prices For...[/]"
                           )
                   );
                    if (days.Count < 1)
                    {
                        Update(
                            70,
                            () =>
                                logTable.Columns[0].Footer(
                                    $"[red bold]Status[/] [green bold]Completed {itemProcess}[/]"
                                )
                        );
                        return;
                    }
                    willBeLeft = (monthlyAllowance - _account.requests_month) - days.Count;
                    if (willBeLeft > 0)
                    {
                        Update(
                            70,
                            () =>
                                logTable.AddRow(
                                    $":check_mark: [green bold]Will Leave {willBeLeft} Calls After Running...[/]"
                                )
                        );
                        Update(
                            70,
                            () =>
                                logTable.AddRow(
                                    $":hourglass_not_done: [red bold]Start Processing Gold Prices from {startDate.ToString("yyyy-MM-dd")} to {endDate.ToString("yyyy-MM-dd")}...[/]"
                                )
                        );
                        var current = days[0];
                        while (current <= days[days.Count - 1])
                        {
                            if (GetGoldPrice(current.ToString("yyyy-MM-dd")))
                            {
                                current = current.AddDays(1);
                                if (_goldPrice != null)
                                {
                                    Update(
                                        70,
                                        () =>
                                            logTable.AddRow(
                                                $":check_mark: [yellow bold italic]{_goldPrice.date.ToString("yyyy-MM-dd")} Price: {_goldPrice.price:C} Previous Price: {_goldPrice.prev_close_price:C}...[/]"
                                            )
                                    );
                                    if (doSave)
                                    {
                                        Update(
                                            70,
                                            () =>
                                                logTable.AddRow(
                                                    $":plus: [red bold]Adding Data To Database...[/]"
                                                )
                                        );
                                        await Save(_goldPrice);
                                        Update(
                                            70,
                                            () =>
                                                logTable.AddRow(
                                                    $":check_mark: [green bold]Saved Gold Price For {_goldPrice.date.ToString("yyyy-MM-dd")}...[/]"
                                                )
                                        );
                                    }
                                }
                            }
                        }
                        Update(
                            70,
                            () =>
                                logTable.AddRow(
                                    $":check_mark: [green bold]Finished Retrieving Gold Prices from {startDate.ToString("yyyy-MM-dd")} to {endDate.ToString("yyyy-MM-dd")}...[/]"
                                )
                        );
                    }
                    else
                    {
                        Update(
                            70,
                            () =>
                                logTable.AddRow(
                                    $":bomb: [red bold italic]There are not enough API Calls left...[/]"
                                )
                        );
                        Update(
                            70,
                            () =>
                                logTable.Columns[0].Footer(
                                    $"[red bold]Status[/] [red bold italic]Aborting {itemProcess}...[/]"
                                )
                        );
                        return;
                    }
                }
                if (getPrice)
                {
                    int day = 0;
                    DateTime date = DateTime.Parse(priceDate);
                    bool isHoliday = new USAPublicHoliday().IsPublicHoliday(date);
                    if (!isHoliday && date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                        day = 1;
                    Update(
                       70,
                       () =>
                           logTable.AddRow(
                               $":check_mark: [green bold]Calculated {day} Days To Get Gold Prices For...[/]"
                           )
                    );
                    if (day < 1)
                    {
                        Update(
                            70,
                            () =>
                                logTable.AddRow(
                                    $":check_mark: [red bold]There are {day} Days To Process...[/]"
                                )
                        );
                        Update(
                            70,
                            () =>
                                logTable.Columns[0].Footer(
                                    $"[red bold]Status[/] [green bold]Completed {itemProcess}[/]"
                                )
                        );
                        return;
                    }
                    willBeLeft = (monthlyAllowance - _account.requests_month) - day;
                    if (willBeLeft > 0)
                    {
                        Update(
                            70,
                            () =>
                                logTable.AddRow(
                                    $":check_mark: [green bold]Will Leave {willBeLeft} Calls After Running...[/]"
                                )
                        );
                        Update(
                            70,
                            () =>
                                logTable.AddRow(
                                    $":hourglass_not_done: [red bold]Start Processing Current Gold Price For {DateTime.Parse(priceDate).ToString("yyyy-MM-dd")}...[/]"
                                )
                        );
                        Update(
                            70,
                            () =>
                                logTable.AddRow(
                                    $":plus: [red bold]Retrieving Gold Price for {DateTime.Parse(priceDate).ToString("yyyy-MM-dd")}...[/]"
                                )
                        );
                        if (GetGoldPrice(DateTime.Parse(priceDate).ToString("yyyy-MM-dd")))
                        {
                            Update(
                                70,
                                () =>
                                    logTable.AddRow(
                                        $":check_mark: [green bold italic]Current Price: {_goldPrice.price:C} Previous Price: {_goldPrice.prev_close_price:C}[/]"
                                    )
                            );
                            if (doSave)
                            {
                                Update(
                                    70,
                                    () =>
                                        logTable.AddRow(
                                            $":plus: [red bold]Adding Data To Database...[/]"
                                        )
                                );
                                await Save(_goldPrice);
                                Update(
                                    70,
                                    () =>
                                        logTable.AddRow(
                                            $":check_mark: [green bold]Saved Gold Price For {_goldPrice.date.ToString("yyyy-MM-dd")}...[/]"
                                        )
                                );
                            }
                        }
                    }
                    else
                    {
                        Update(
                            70,
                            () =>
                                logTable.AddRow(
                                    $":bomb: [red bold italic]There are not enough API Calls left...[/]"
                                )
                        );
                        Update(
                            70,
                            () =>
                                logTable.Columns[0].Footer(
                                    $"[red bold]Status[/] [red bold italic]Aborting {itemProcess}...[/]"
                                )
                        );
                        return;
                    }
                }
                if (getAccount)
                {
                    Update(
                        70,
                        () =>
                            logTable.AddRow(
                                $":hourglass_not_done: [red bold]Start Processing Get Account Information...[/]"
                            )
                    );
                    await GetAccountInformation();
                    Update(
                        70,
                        () =>
                            logTable.AddRow(
                                $":check_mark: [green bold]Finished Getting Account Information...[/]"
                            )
                    );
                    Update(
                        70,
                        () =>
                            logTable.AddRow(
                                $"[yellow]    Requests Today: {_account.requests_today} Requests Yesterday: {_account.requests_yesterday}\r\n    Requests This Month: {_account.requests_month} Requests Last Month: {_account.requests_last_month}[/]"
                            )
                    );
                    Update(
                        70,
                        () =>
                            logTable.AddRow(
                                $":check_mark: [green bold italic]Remaining WebAPI Requests: {monthlyAllowance - _account.requests_month}[/]"
                            )
                    );
                }
                if (getStatus)
                {
                    Update(
                        70,
                        () =>
                            logTable.AddRow(
                                $":hourglass_not_done: [red bold]Start Processing Get WebAPI Status...[/]"
                            )
                    );
                    await GetWebApiStatus();
                    Update(
                        70,
                        () =>
                            logTable.AddRow(
                                $":check_mark: [green bold italic]WebAPI Available: {_status.result}[/]"
                            )
                    );
                }
                Update(
                    70,
                    () =>
                        logTable.Columns[0].Footer(
                            $"[red bold]Status[/] [green bold]Completed {itemProcess}[/]"
                        )
                );
            }
        );
    }

    private static async Task GetAccountInformation()
    {
        var client = new RestClient(url + stat);
        var request = new RestRequest("", Method.Get);
        request.AddHeader("x-access-token", token);
        request.AddHeader("Content-Type", "application/json");
        RestResponse response = await client.ExecuteAsync(request);
        _account = JsonConvert.DeserializeObject<Account>(response.Content);
    }

    private static async Task GetWebApiStatus()
    {
        var client = new RestClient(url + status);
        var request = new RestRequest("", Method.Get);
        request.AddHeader("x-access-token", token);
        request.AddHeader("Content-Type", "application/json");
        RestResponse response = await client.ExecuteAsync(request);
        _status = JsonConvert.DeserializeObject<Status>(response.Content);
    }

    private static bool GetGoldPrice(string date)
    {
        var processDate = DateTime.Parse(date);
        bool isHoliday = new USAPublicHoliday().IsPublicHoliday(processDate);
        if (
            !isHoliday
            && processDate.DayOfWeek != DayOfWeek.Saturday
            && processDate.DayOfWeek != DayOfWeek.Sunday
        )
        {
            var client = new RestClient(url + gold + date);
            var request = new RestRequest("", Method.Get);
            request.AddHeader("x-access-token", token);
            request.AddHeader("Content-Type", "application/json");
            RestResponse response = client.Execute(request);
            _goldPrice = JsonConvert.DeserializeObject<GoldPrice>(response.Content);
            return true;
        }
        return false;
    }

    private static List<DateTime> GetNumberOfDays(DateTime start, DateTime end)
    {
        List<DateTime> dates = new();
        var res = DateTime.Compare(end, DateTime.Now);

        // We do not want the end date to be the current date or future date.
        if (res > 0)
        {
            end = DateTime.Now.AddDays(-1);
            endDate = end;
        }

        while (start <= end)
        {
            bool isHoliday = new USAPublicHoliday().IsPublicHoliday(start);
            if (!isHoliday && start.DayOfWeek != DayOfWeek.Saturday && start.DayOfWeek != DayOfWeek.Sunday)
                dates.Add(start);
            start = start.AddDays(1);
        }
        return dates;
    }

    private static int BackTrack(CommandContext context, BacktrackCommand.Settings settings)
    {
        doBackTrack = true;
        showHidden = settings.ShowHidden;
        isDebug = settings.Debug;
        startDate = DateTime.Parse(settings.StartDate);
        endDate = DateTime.Parse(settings.EndDate);
        doSave = settings.Save;
        return 0;
    }

    private static int GetPrice(CommandContext context, PriceCommand.Settings settings)
    {
        getPrice = true;
        showHidden = settings.ShowHidden;
        isDebug = settings.Debug;
        doSave = settings.Save;
        if (settings.Date != null)
            priceDate = settings.Date;
        return 0;
    }

    private static int GetAccount(CommandContext context, AccountCommand.Settings settings)
    {
        getAccount = true;
        showHidden = settings.ShowHidden;
        isDebug = settings.Debug;
        return 0;
    }

    private static int GetStatus(CommandContext context, StatusCommand.Settings settings)
    {
        getStatus = true;
        showHidden = settings.ShowHidden;
        isDebug = settings.Debug;
        return 0;
    }

    static async Task Save(GoldPrice goldPrice)
    {
        if (goldPrice.date.Year < 1900)
            return;
        MySqlConnection sqlConnection = new MySqlConnection(connectionString);
        MySqlCommand sqlCommand = new MySqlCommand("usp_AddGoldPrice", sqlConnection);
        sqlCommand.CommandType = CommandType.StoredProcedure;
        try
        {
            sqlConnection.Open();
            sqlCommand.Parameters.AddWithValue("price", goldPrice.price);
            sqlCommand.Parameters.AddWithValue("prev_price", goldPrice.prev_close_price);
            sqlCommand.Parameters.AddWithValue("ratedate", goldPrice.date.ToString("yyyy/MM/dd"));
            sqlCommand.Parameters.AddWithValue("chg", goldPrice.ch);
            sqlCommand.Parameters.AddWithValue("chg_pct", goldPrice.chp);
            var recs = await sqlCommand.ExecuteNonQueryAsync();
        }
        catch (MySqlException ex)
        {
            Console.WriteLine("Could not insert new gold rate.");
            Console.WriteLine("Exception: {0}", ex.Message);
        }
        finally
        {
            if (sqlConnection.State == ConnectionState.Open)
                sqlConnection.Close();
            sqlCommand.Dispose();
            sqlConnection.Dispose();
        }
    }
}
