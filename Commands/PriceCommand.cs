using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MetalPriceConsole.Commands.Settings;
using MetalPriceConsole.Models;
using PublicHoliday;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MetalPriceConsole.Commands;

public class PriceCommand : BasePriceCommand<PriceCommand.Settings>
{
    private string _defaultDB;
    private readonly ApiServer _apiServer;
    private readonly ConnectionStrings _connectionStrings;

    public PriceCommand(ApiServer apiServer, ConnectionStrings connectionStrings) : base(apiServer, connectionStrings)
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
        string metalName = base.MetalName;
        string metal = base.Metal;
        _defaultDB = _connectionStrings.DefaultDB;

        var table = new Table().Centered();
        // Borders
        table.BorderColor(Color.Blue);
        table.MinimalBorder();
        table.SimpleBorder();
        table.AddColumn(
            new TableColumn(
                new Markup(
                    "[yellow bold]Running Database Connection Configuration Test[/]"
                ).Centered()
            )
        );
        table.BorderColor(Color.Blue);
        table.Border(TableBorder.DoubleEdge);
        table.Expand();

        // Animate
        await AnsiConsole
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

                if (settings.StartDate == DateTime.Now.ToString("yyyy-MM-dd"))
                {
                    Update(70, () => table.AddRow($"[red bold]Date {settings.StartDate} Cannot Be Current Or Future Date[/]"));
                    return;
                }
                if (settings.StartDate != String.Empty)
                {
                    Update(
                        70,
                        () =>
                            table.AddRow(
                                $"[red bold]Status[/] [green bold]Calculating Non-Weekend/Holiday Dates For {settings.StartDate} to {settings.EndDate}[/]"
                            )
                    );
                }

                List<DateTime> days = GetNumberOfDays(settings.StartDate, settings.EndDate);

                string msg = days.Count == 1 ? $"[green bold]There is {days.Count} Day To Process For {metalName}...[/]" : $"[green bold]There are {days.Count} Days To Process For {metalName}...[/]";
                Update(70, () => table.AddRow(msg));

                if (days.Count < 1)
                {
                    Update(70, () => table.Columns[0].Footer(
                                $"[red bold]Status[/] [green bold]Completed, Nothing Processed. Holiday Or Weekend.[/]"
                            ));
                    return;
                }


                Update(70, () => table.Columns[0].Footer($"[green]Calling {_apiServer.BaseUrl}stat For Account Information[/]"));
                Account account;
                try
                {
                    if (settings.TokenOverride != null)
                        _apiServer.Token = settings.TokenOverride;
                    // Need to decide if we want to get account details when fake is used. Guessing yes to show actual use.
                    string accountUrl = _apiServer.BaseUrl + "stat";
                    Update(70, () => table.AddRow($"[red bold]Status[/] [green]Retrieving Account Details...[/]"));
                    if (!settings.Fake)
                        account = await AccountDetails.GetDetailsAsync(_apiServer, accountUrl);
                    else
                    {
                        string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        string file = Path.Combine(path, "Account.sample");
                        string cache = File.ReadAllText(file);
                        account = JsonSerializer.Deserialize<Account>(cache);
                    }

                    _ = int.TryParse(_apiServer.MonthlyAllowance, out int monthlyAllowance);
                    var willBeLeft = (monthlyAllowance - account.RequestsMonth) - days.Count;
                    Update(70, () => table.Columns[0].Footer($"[green]Calls Remaining Is {monthlyAllowance - account.RequestsMonth}[/]"));
                    if (willBeLeft > 0)
                    {
                        List<MetalPrice> metalPrices = new();
                        Update(70, () => table.AddRow($"[green bold]Will Leave {willBeLeft} Calls After Running...[/]"));

                        foreach (DateTime date in days)
                        {
                            int i = 0;
                            Update(70, () => table.AddRow($"[yellow bold]Retrieving {metal} Price for {date:yyyy-MM-dd}...[/]"));

                            string url = $"{_apiServer.BaseUrl}{metal}/{settings.Currency}";
                            // Platinum and Palladium do not support historical so dates cannot be used.
                            if (settings.StartDate != String.Empty)
                                url += $"/{date:yyyyMMdd}";
                            Update(70, () => table.AddRow($"[green]Calling {url}[/]"));

                            /// TODO Need to add support for --fake calls
                            MetalPrice metalPrice = await GetPriceAsync(url, _apiServer.Token);
                            if (metalPrice != null)
                            {
                                Update(
                                    70,
                                    () =>
                                        table.AddRow(
                                            $"{metalPrice.Date.ToShortDateString()}       [green bold italic]Current Ounce Price: {metalPrice.Price:C} Previous Ounce Price: {metalPrice.PrevClosePrice:C}[/]"
                                        )
                                );
                                Update(
                                    70,
                                    () =>
                                        table.AddRow(
                                            $"            [green bold italic] 24k gram: {metalPrice.PriceGram24k:C} 22k gram: {metalPrice.PriceGram22k:C} 21k gram: {metalPrice.PriceGram21k:C} 20k gram: {metalPrice.PriceGram20k:C} 18k gram: {metalPrice.PriceGram18k:C}[/]"
                                        )
                                );
                                metalPrices.Add(metalPrice);
                            }
                            i++;
                            // More rows than we can display Remove first one
                            if (table.Rows.Count > Console.WindowHeight - 10)
                                table.Rows.RemoveAt(0);
                        }
                        if (settings.Cache)
                            if (!Database.CacheData(metalPrices, _apiServer.CacheFile))
                                Update(70, () => table.AddRow($"[red bold]Error Caching Data[/]"));
                            else
                                Update(70, () => table.AddRow($"[green bold]Cache File Written.[/]"));
                        if (settings.Save)
                            if (!Database.Save(metalPrices, _defaultDB, _apiServer.CacheFile))
                                Update(70, () => table.AddRow($"[red bold]Error Saving Data To Database. Saved To Cache File.[/]"));
                            else
                                Update(70, () => table.AddRow($"[green bold]Data Saved To Database.[/]"));
                        Update(70, () => table.Columns[0].Footer($"[green bold]Completed. Processed {days.Count} Days With Total Of {metalPrices.Count}.[/]"));
                    }
                    else
                    {
                        Update(70, () => table.AddRow($"[red bold]Not Enough Calls Left ({monthlyAllowance - account.RequestsMonth}). This Requires {days.Count} Total Calls.[/]"));
                    }
                }
                catch (Exception ex)
                {
                    Update(70, () => table.AddRow($"[red]Request Error: {ex.Message} Calling Url: {_apiServer.BaseUrl}stat[/]"));
                    return;
                }
            });
        return 0;
    }

    private static async Task<MetalPrice> GetPriceAsync(string url, string token)
    {
        MetalPrice metalPrice;
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("x-access-token", token);
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStreamAsync();
                metalPrice = await JsonSerializer.DeserializeAsync<MetalPrice>(result);
            }
        }
        return metalPrice;
    }

    private static List<DateTime> GetNumberOfDays(string startDate, string endDate)
    {
        bool isHoliday = false;
        List<DateTime> dates = new();
        if (startDate == String.Empty || endDate == String.Empty)
        {
            isHoliday = new USAPublicHoliday().IsPublicHoliday(DateTime.Now);
            if (!isHoliday)
                dates.Add(DateTime.Now);
            return dates;
        }
        DateTime start = DateTime.Parse(startDate);
        DateTime end = DateTime.Parse(endDate);
        var res = DateTime.Compare(end, DateTime.Now);

        // We do not want the end date to be the current date or future date.
        if (res > 0)
            end = DateTime.Now.AddDays(-1);

        while (start <= end)
        {
            isHoliday = new USAPublicHoliday().IsPublicHoliday(start);
            if (!isHoliday && start.DayOfWeek != DayOfWeek.Saturday && start.DayOfWeek != DayOfWeek.Sunday)
                dates.Add(start);
            start = start.AddDays(1);
        }
        return dates;
    }
    public override ValidationResult Validate(CommandContext context, PriceCommandSettings settings)
    {
        if (settings.StartDate == "")
            settings.StartDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        if (!DateTime.TryParse(settings.StartDate, out _))
            return ValidationResult.Error($"Invalid date - {settings.StartDate}");
        if (settings.EndDate == "")
            settings.EndDate = settings.StartDate;
        if (!DateTime.TryParse(settings.EndDate, out _))
            return ValidationResult.Error($"Invalid date - {settings.EndDate}");
        return base.Validate(context, settings);
    }
}