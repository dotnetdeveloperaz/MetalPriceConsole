using System;
using System.ComponentModel;
using System.Threading;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Spectre.Console;
using Spectre.Console.Cli;
using MetalPriceConsole.Models;
using Newtonsoft.Json.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

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
        if (settings.Debug)
        {
            DebugDisplay.Print(settings, _apiServer, _logger);
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
                table.Columns[1].RightAligned().Width(30).PadRight(20);
                table.Columns[0].RightAligned();
                Update(70, () => table.AddRow($"[yellow]Requests Today[/]", $"[yellow]{account.requests_today}[/]"));
                Update(70, () => table.AddRow($"[yellow]Requests Yesterday[/]", $"[yellow]{account.requests_yesterday}[/]"));
                Update(70, () => table.AddRow($"[yellow]Requests This Month[/]", $"[yellow]{account.requests_month}[/]"));
                Update(70, () => table.AddRow($"[yellow]Requests Last Month[/]", $"[yellow]{account.requests_last_month}[/]"));

                int allowance = 0;
                int.TryParse(_apiServer.MonthlyAllowance, out allowance);
                Update(
                    70,
                    () =>
                    table.AddRow(
                        $":check_mark: [green bold italic]Remaining WebAPI Requests:[/]", 
                        $"[green bold italic]{allowance - account.requests_month}[/]"));
                Update(70, () => table.Columns[0].Footer("[blue]Complete[/]"));
            });
        return 0;
    }
}
