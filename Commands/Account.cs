using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using MetalPriceConsole.Models;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace MetalPriceConsole.Commands;

public class AccountCommand : AsyncCommand<AccountCommand.Settings>
{
    private readonly ApiServer _apiServer;
    private static readonly string[] columns = new[] { "" };

    public AccountCommand(ApiServer apiServer)
    {
        _apiServer = apiServer;
    }

    public class Settings : BaseCommandSettings
    {
        // There are no special settings for this command
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        string url = _apiServer.BaseUrl + "stat";
        if (settings.Debug)
        {
            if (!DebugDisplay.Print(settings, _apiServer, url))
                return 0;
        }
        //AnsiConsole.WriteLine();
        //AnsiConsole.WriteLine();
        // Process Window
        var table = new Table().Centered();
        table.HideHeaders();
        table.BorderColor(Color.Green3);
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

                Update(
                    70,
                    () =>
                        table.Columns[0].Footer(
                            $"[red bold]Status[/] [green bold]Retrieving Account Information[/]"
                        )
                );
                // Content
                Update(70, () => table.Columns[0].Footer($"[green]Calling {url}[/]"));
                if (settings.TokenOverride != null)
                    _apiServer.Token = settings.TokenOverride;
                Account account;
                try
                {
                    if(!settings.Fake)
                        account = await AccountDetails.GetDetailsAsync(_apiServer, url);
                    else
                    {
                        string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        string file = Path.Combine(path, "Account.sample");
                        string cache = File.ReadAllText(file);
                        account = JsonSerializer.Deserialize<Account>(cache);

                    }
                }
                catch ( Exception ex ) 
                {
                    Update(70, () => table.AddRow($"[red]Request Error: {ex.Message} Calling Url: {_apiServer.BaseUrl}stat[/]"));
                    return;
                }

                table.Columns[0].Centered();
                Update(70, () => table.AddRow($"[yellow]     Requests Today[/] [yellow]{account.RequestsToday}[/]"));
                Update(70, () => table.AddRow($"[yellow] Requests Yesterday[/] [yellow]{account.RequestsYesterday}[/]"));
                Update(70, () => table.AddRow($"[yellow]Requests This Month[/] [yellow]{account.RequestsMonth}[/]"));
                Update(70, () => table.AddRow($"[yellow]Requests Last Month[/] [yellow]{account.RequestsLastMonth}[/]"));

                _ = int.TryParse(_apiServer.MonthlyAllowance, out int allowance);
                Update(
                    70,
                    () =>
                    table.AddRow(
                        $"[green bold italic]Remaining WebAPI Requests: {allowance - account.RequestsMonth}[/]"));
                Update(70, () => table.Columns[0].Footer($"[blue]Finished Retrieving Account Details From {url}[/]"));
            });
        return 0;
    }
}
