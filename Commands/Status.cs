using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using MetalPriceConsole.Models;
using Microsoft.Extensions.Logging;
using RestSharp;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MetalPriceConsole.Commands;

public class StatusCommand : Command<StatusCommand.Settings>
{
    private readonly ApiServer _apiServer;
    private ILogger _logger;

    public StatusCommand(ApiServer apiServer, ILogger<AccountCommand> logger)
    {
        _apiServer = apiServer;
        _logger = logger;
    }

    public class Settings : CommandSettings
    {
        [Description("Get WebApi Status.")]
        [DefaultValue(false)]
        public bool Status { get; set; }

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
        settings.Status = true;
        if (settings.Debug)
        {
            DebugDisplay.Print(settings, _apiServer, _logger);
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
                var client = new RestClient(_apiServer.BaseUrl + "status");
                var request = new RestRequest("", Method.Get);
                request.AddHeader("x-access-token", _apiServer.Token);
                request.AddHeader("Content-Type", "application/json");
                RestResponse response;
                ApiStatus apiStatus;
                try
                {
                    response = client.Execute(request);
                    apiStatus = JsonSerializer.Deserialize<ApiStatus>(response.Content);
                }
                catch (Exception ex)
                {
                    Update(70, () => table.AddRow($"[red]Error: {ex.Message}[/]", $"[red]Calling Url: {_apiServer.BaseUrl}status[/]"));
                    return;
                }
                table.Columns[1].RightAligned().Width(30).PadRight(20);
                table.Columns[0].RightAligned();
                if(apiStatus.Result)
                    Update(70, () => table.AddRow($"[yellow]WebApi Status[/]", "[green]Service is Up[/]"));
                else
                    Update(70, () => table.AddRow($"[yellow]WebApi Status[/]", "[red]Service is Down[/]"));
                Update(70, () => table.Columns[0].Footer("[blue]Complete[/]"));
            });
        return 0;
    }
}