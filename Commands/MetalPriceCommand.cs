using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MetalPriceConsole.Models;
using PublicHoliday;
using Spectre.Console;
using Spectre.Console.Cli;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MetalPriceConsole.Commands;

internal class MetalPriceCommand : AsyncCommand<MetalPriceCommand.Settings>
{
    private readonly ApiServer _apiServer;
    private readonly string _connectionString;
    private static readonly string[] columns = new[] { "" };

    public MetalPriceCommand(ApiServer apiServer, ConnectionStrings connectionStrings)
    {
        _apiServer = apiServer;
        _connectionString = connectionStrings.DefaultDB;
    }

    public class Settings : PriceCommandSettings
    {
        [CommandOption("--start <date>")]
        [Description("Date Or Start Date To Get Price(s) For")]
        [DefaultValue("")]
        public string StartDate { get; set; }

        [CommandOption("--end <date>")]
        [Description("End Date To Get Price(s) For - Not Required For Single Day")]
        [DefaultValue("")]
        public string EndDate { get; set; }

    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        string url = _apiServer.BaseUrl;
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
        }
        else if (settings.GetPlatinum)
        {
            metalName = "Platinum";
            metal = _apiServer.Platinum;
        }
        else
        {
            metal = _apiServer.Gold;
            settings.GetGold = true;
        }
        if (settings.Debug)
        {
            if (!DebugDisplay.Print(settings, _apiServer, url))
                return 0;
        }
        // Process Window
        var table = new Table().Centered();
        table.HideHeaders();
        table.BorderColor(Color.Yellow);
        table.Border(TableBorder.Rounded);
        table.AddColumns(columns);
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
                Update(
                    70,
                    () =>
                        table.AddRow(
                            $"[red bold]Status[/] [green bold]Calculating Non-Weekend/Holiday Dates For {settings.StartDate} to {settings.EndDate}[/]"
                        )
                );
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
                    if (willBeLeft > 0)
                    {
                        List<MetalPrice> metalPrices = new();
                        Update(70, () => table.AddRow($"[green bold]Will Leave {willBeLeft} Calls After Running...[/]"));

                        foreach (DateTime date in days)
                        {
                            int i = 0;
                            Update(70, () => table.AddRow($"[yellow bold]Retrieving {metal} Price for {date:yyyy-MM-dd}...[/]"));

                            string url = $"{_apiServer.BaseUrl}{metal}/{settings.Currency}/{date:yyyyMMdd}";
                            Update(70, () => table.AddRow($"[green]Calling {url}[/]"));
                            MetalPrice metalPrice = await GetPriceAsync(url, _apiServer.Token);
                            if (metalPrice != null)
                            {
                                Update(
                                    70,
                                    () =>
                                        table.AddRow(
                                            $"       [green bold italic]Current Ounce Price: {metalPrice.Price:C} Previous Ounce Price: {metalPrice.PrevClosePrice:C}[/]"
                                        )
                                );
                                Update(
                                    70,
                                    () =>
                                        table.AddRow(
                                            $"            [green bold italic] 24k gram: {metalPrice.PriceGram24k:C} 22k gram: {metalPrice.PriceGram22k:C} 21k gram: {metalPrice.PriceGram21k:C} 20k gram: {metalPrice.PriceGram20k:C} 18k gram: {metalPrice.PriceGram18k:C}[/]"
                                        )
                                );
                                metalPrices.Add( metalPrice );
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
                            if (!Database.Save(metalPrices, _connectionString, _apiServer.CacheFile))
                                Update(70, () => table.AddRow($"[red bold]Error Saving Data To Database. Saved To Cache File.[/]"));
                            else
                                Update(70, () => table.AddRow($"[green bold]Data Saved To Database.[/]"));
                        Update(70, () => table.Columns[0].Footer($"[green bold]Completed. Processed {days.Count} Days With Total Of {metalPrices.Count}.[/]"));
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
        if (settings.StartDate == "")
            settings.StartDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        if (!DateTime.TryParse(settings.StartDate, out _))
            return ValidationResult.Error($"Invalid date - {settings.StartDate}");
        if (settings.EndDate == "")
            settings.EndDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        if (!DateTime.TryParse(settings.EndDate, out _))
            return ValidationResult.Error($"Invalid date - {settings.EndDate}");
        return base.Validate(context, settings);
    }
}