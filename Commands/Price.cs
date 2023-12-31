using System.Xml.Xsl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
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

public class PriceCommand : AsyncCommand<PriceCommand.Settings>
{
    private readonly ApiServer _apiServer;
    private readonly string _connectionString;
    private readonly ILogger _logger;
    private static readonly string[] columns = new[] { "" };

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

        [CommandOption("--date <date>")]
        [Description("Date To Get Price For")]
        [DefaultValue("")]
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
        // We always get the previous day so that we have the closing price as the default.
        var date = settings.Date == null ? DateTime.Now.AddDays(-1).ToString("yyyyMMdd") : settings.Date.Replace("-","");
        settings.GetPrice = true;
        string url = _apiServer.BaseUrl;
        if (settings.GetSilver)
        {
            url += $"{_apiServer.Silver}/{settings.Currency}/{date}";
            settings.GetGold = false;
        }
        else if (settings.GetPalladium)
        {
            url += $"{_apiServer.Palladium}/{settings.Currency}"; //{date}";
            settings.GetGold = false;
        }
        else if (settings.GetPlatinum)
        {
            url += $"{_apiServer.Platinum}/{settings.Currency}"; //{date}";
            settings.GetGold = false;
        }
        else
            url += $"{_apiServer.Gold}/{settings.Currency}/{date}";
        // Platinum and Palladium do not support specify date
        // Developer stated they would be adding it 
        // For now, we have the if else if statements above
        // url += settings.Currency + date;
        if (settings.Debug)
        {
            DebugDisplay.Print(settings, _apiServer, url);
        }
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
                else if (settings.GetPalladium)
                    metal = "Palladium";
                else if (settings.GetPlatinum)
                    metal = "Platinum";
                Update(
                    70,
                    () =>
                        table.Columns[0].Footer(
                            $"[red bold]Status[/] [green bold]Checking for business day before retrieving {metal} Price For {settings.Date}[/]"
                        )
                );
                int day = 0;
                DateTime date = DateTime.Parse(settings.Date);
                Update(
                    70,
                    () =>
                        table.Columns[0].Footer(
                            $"[red bold]Status[/] [green bold]Checking if {date} is a holiday....[/]"
                        )
                );
                bool isHoliday = new USAPublicHoliday().IsPublicHoliday(date);
                if (!isHoliday && date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                    day = 1;
                Update(
                   70,
                   () =>
                       table.AddRow(
                           $":check_mark: [green bold]Calculated {day} Days to get {metal} prices for...[/]"
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
                Account account;
                using (HttpClient client = new())
                {
                    client.DefaultRequestHeaders.Add("x-access-token", _apiServer.Token);
                    using (HttpRequestMessage request = new(HttpMethod.Get, _apiServer.BaseUrl + "stat"))
                    {
                        try
                        {
                            Update(
                                70,
                                () =>
                                    table.Columns[0].Footer(
                                        $"[red bold]Status[/] [green bold]Checking Number Of API Calls Remaining...[/]"
                                    )
                            );
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
                    var willBeLeft = (monthlyAllowance - account.RequestsMonth) - day;
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
                                    $":plus: [red bold]Retrieving {metal} Price for {settings.Date}...[/]"
                                )
                        );
                        if (!isHoliday && date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                        {
                            MetalPrice metalPrice;
                            if (!settings.Fake)
                            {
                                using (var request = new HttpRequestMessage(HttpMethod.Get, $"{url}"))
                                {
                                    try
                                    {
//                                      Update(70, () => table.Columns[0].Footer($"[green]Calling {url}[/]"));
                                        Update(70, () => table.Columns[0].Footer($"[green]Calling {request.RequestUri.AbsoluteUri}[/]"));
                                        HttpResponseMessage response = await client.SendAsync(request);
                                        response.EnsureSuccessStatusCode();
                                        Update(70, () => table.Columns[0].Footer($"[green]Reading Response, Status {response.StatusCode}[/]"));
                                        var result = await response.Content.ReadAsStreamAsync();
                                        Update(70, () => table.Columns[0].Footer($"[green]Deserializing {result}[/]"));
                                        metalPrice = await JsonSerializer.DeserializeAsync<MetalPrice>(result);
                                        if(settings.Cache)
                                            Database.CacheData(metalPrice, _apiServer.CacheFile);
                                    }
                                    catch (Exception ex)
                                    {
                                        Update(70, () => table.AddRow($"[red]Deserialization Error: {ex.Message}[/]", $"[red]Calling Url: {_apiServer.BaseUrl}stat[/]"));
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                string cache = await File.ReadAllTextAsync("SingleDay.sample");
                                using MemoryStream stream = new(Encoding.UTF8.GetBytes(cache));
                                List<MetalPrice> metalPrices = await JsonSerializer.DeserializeAsync<List<MetalPrice>>(stream);
                                metalPrice = metalPrices[0];
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
                                                    $":check_mark: [green bold]Saved Gold Price For {metalPrice.Date:yyyy-MM-dd}...[/]"
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
                                                    $":stop_sign: [red bold]Could Not Save Gold Price For {metalPrice.Date:yyyy-MM-dd}, data was cached...[/]"
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
                                        table.Columns[0].Footer(
                                            $"[red bold]Error[/] [red bold italic]No data Deserialized...[/]"
                                        )
                                );
                            }
                        }
                        else
                        {
                            Update(
                                70,
                                () =>
                                    table.Columns[0].Footer(
                                        $":stop_sign: [red bold]There was no data available...[/]"
                                    )
                            );
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
                }
            });
        return Task.FromResult(0);
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (settings.Date == "")
            settings.Date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        if (!DateTime.TryParse(settings.Date, out _))
            return ValidationResult.Error($"Invalid date - {settings.Date}");
        return base.Validate(context, settings);
    }
}
