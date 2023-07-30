using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Logging;
using RestSharp;
using Spectre.Console;
using Spectre.Console.Cli;
using MetalPriceConsole.Models;

namespace MetalPriceConsole.Commands;

public class AccountCommand : Command<AccountCommand.Settings>
{
    private readonly ApiServer _apiServer;
    private ILogger _logger;

    public AccountCommand(ApiServer apiServer, ILogger<AccountCommand> logger)
    {
        _apiServer = apiServer;
        _logger = logger;
    }

    public class Settings : CommandSettings
    {
        [Description("Get Account Statistics.")]
        [DefaultValue(false)]
        public bool Account { get; set; }

        [CommandOption("--debug")]
        [Description("Enable Debug Output")]
        [DefaultValue(false)]
        public bool Debug { get; set; }

        [CommandOption("--hidden")]
        [Description("Enable Secret Debug Output")]
        [DefaultValue(false)]
        public bool ShowHidden { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        settings.Account = true;
        string url = _apiServer.BaseUrl + "stat";
        if (settings.Debug)
        {
            DebugDisplay.Print(settings, _apiServer, url);
        }
        AnsiConsole.WriteLine();
        // Process Window
        var table = new Table().Centered();
//        table.HideHeaders();
        table.BorderColor(Color.Yellow);
        table.Border(TableBorder.Rounded);
        table.Border(TableBorder.Simple);
        table.AddColumns(new[] { "", ""});
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
 //               Update(230, () => table.AddColumn(""));
 //               Update(230, () => table.AddColumn(""));
                // Borders
                // Footers
                Update(
                    70,
                    () =>
                        table.Columns[0].Footer(
                            $"[red bold]Status[/] [green bold]Retrieving Account Information[/]"
                        )
                );
                // Content
                var client = new RestClient(url);
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
                table.Columns[1].RightAligned().Width(30).PadRight(20);
                table.Columns[0].RightAligned();
                Update(70, () => table.AddRow($"[yellow]Requests Today[/]", $"[yellow]{account.RequestsToday}[/]"));
                Update(70, () => table.AddRow($"[yellow]Requests Yesterday[/]", $"[yellow]{account.RequestsYesterday}[/]"));
                Update(70, () => table.AddRow($"[yellow]Requests This Month[/]", $"[yellow]{account.RequestsMonth}[/]"));
                Update(70, () => table.AddRow($"[yellow]Requests Last Month[/]", $"[yellow]{account.RequestsLastMonth}[/]"));

                int allowance = 0;
                int.TryParse(_apiServer.MonthlyAllowance, out allowance);
                Update(
                    70,
                    () =>
                    table.AddRow(
                        $":check_mark: [green bold italic]Remaining WebAPI Requests:[/]", 
                        $"[green bold italic]{allowance - account.RequestsMonth}[/]"));
                Update(70, () => table.Columns[0].Footer("[blue]Complete[/]"));
            });
        return 0;
    }
}
