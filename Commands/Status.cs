using System;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MetalPriceConsole.Commands.Settings;
using MetalPriceConsole.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MetalPriceConsole.Commands;

public class StatusCommand : AsyncCommand<StatusCommand.Settings>
{
    private readonly ApiServer _apiServer;

    public StatusCommand(ApiServer apiServer)
    {
        _apiServer = apiServer;
    }

    public class Settings : BaseCommandSettings
    {
        // There are no special settings for this command
    }

    public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        string url = _apiServer.BaseUrl + "status";
        if (settings.Debug)
        {
            DebugDisplay.Print(settings, _apiServer, url);
        }
        // Process Window
        var table = new Table().Centered();
        table.HideHeaders();
        table.BorderColor(Color.Yellow);
        table.Border(TableBorder.Rounded);
        table.Border(TableBorder.Simple);
        table.AddColumns(new[] { "", "" });
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
                            $"[red bold]Status[/] [green bold]Retrieving WebApi Status[/]"
                        )
                );
                // Content
                ApiStatus apiStatus;
                HttpClient client = new();
                if (settings.TokenOverride != null)
                    _apiServer.Token = settings.TokenOverride;
                client.DefaultRequestHeaders.Add("x-access-token", _apiServer.Token);
                using (HttpRequestMessage request = new(HttpMethod.Get, url))
                {
                    try
                    {
                        HttpResponseMessage response = client.Send(request);
                        response.EnsureSuccessStatusCode();
                        var result = response.Content.ReadAsStream();
                        apiStatus = JsonSerializer.Deserialize<ApiStatus>(result);
                    }
                    catch (Exception ex)
                    {
                        Update(70, () => table.AddRow($"[red]Error: {ex.Message}[/]", $"[red]Calling Url: {_apiServer.BaseUrl}stat[/]"));
                        return;
                    }
                }
                table.Columns[1].RightAligned().Width(30).PadRight(20);
                table.Columns[0].RightAligned();
                if(apiStatus.Result)
                    Update(70, () => table.AddRow($"[yellow]WebApi Status[/]", "[green]Service is Up[/]"));
                else
                    Update(70, () => table.AddRow($"[yellow]WebApi Status[/]", "[red]Service is Down[/]"));
                Update(70, () => table.Columns[0].Footer("[blue]Complete[/]"));
            });
        return Task.FromResult(0);
    }
}