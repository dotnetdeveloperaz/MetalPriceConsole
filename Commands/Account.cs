using System;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using MetalPriceConsole.Models;

namespace MetalPriceConsole.Commands;

public class AccountCommand : AsyncCommand<AccountCommand.Settings>
{
    private readonly ApiServer _apiServer;
    private static readonly string[] columns = new[] { "", ""};

    public AccountCommand(ApiServer apiServer)
    {
        _apiServer = apiServer;
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

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
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
        table.BorderColor(Color.Yellow);
        table.Border(TableBorder.Rounded);
        table.Border(TableBorder.Simple);
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
                Account account;
                HttpClient client = new();
                client.DefaultRequestHeaders.Add("x-access-token", _apiServer.Token);
                using (HttpRequestMessage request = new(HttpMethod.Get, url))
                {
                    try
                    {
                        HttpResponseMessage response = await client.SendAsync(request);
                        response.EnsureSuccessStatusCode();
                        var result = await response.Content.ReadAsStreamAsync();
                        account = JsonSerializer.Deserialize<Account>(result);
                    }
                    catch (Exception ex)
                    {
                        Update(70, () => table.AddRow($"[red]Error: {ex.Message}[/]", $"[red]Calling Url: {_apiServer.BaseUrl}stat[/]"));
                        return;
                    }
                }
                table.Columns[1].RightAligned().Width(30).PadRight(20);
                table.Columns[0].RightAligned();
                Update(70, () => table.AddRow($"[yellow]Requests Today[/]", $"[yellow]{account.RequestsToday}[/]"));
                Update(70, () => table.AddRow($"[yellow]Requests Yesterday[/]", $"[yellow]{account.RequestsYesterday}[/]"));
                Update(70, () => table.AddRow($"[yellow]Requests This Month[/]", $"[yellow]{account.RequestsMonth}[/]"));
                Update(70, () => table.AddRow($"[yellow]Requests Last Month[/]", $"[yellow]{account.RequestsLastMonth}[/]"));

                _ = int.TryParse(_apiServer.MonthlyAllowance, out int allowance);
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
