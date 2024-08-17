using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using MetalPriceConsole.Commands.Settings;
using MetalPriceConsole.Models;
using MySqlConnector;
using Spectre.Console;
using Spectre.Console.Cli;

namespace MetalPriceConsole.Commands;

public class TestDatabaseCommand : AsyncCommand<TestDatabaseCommand.Settings>
{
    private readonly string _connectionString;
    private readonly ApiServer _apiServer;

    public TestDatabaseCommand(ApiServer apiServer, ConnectionStrings ConnectionString)
    {
        _apiServer = apiServer;
        _connectionString = ConnectionString.DefaultDB;
    }

    public class Settings : BaseCommandSettings
    {
        [CommandOption("--db")]
        [Description("Override Configured DB For Testing")]
        [DefaultValue(null)]
        public string DBConnectionString { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        settings.DBConnectionString ??= _connectionString;
        if (settings.Debug)
        {
            if (!DebugDisplay.Print(settings, _apiServer, _connectionString, "N/A"))
                return 0;

        }
        var titleTable = new Table().Centered();
        // Borders
        titleTable.BorderColor(Color.Blue);
        titleTable.MinimalBorder();
        titleTable.SimpleBorder();
        titleTable.AddColumn(
            new TableColumn(
                new Markup(
                    "[yellow bold]Running Database Connection Configuration Test[/]"
                ).Centered()
            )
        );
        titleTable.BorderColor(Color.Blue);
        titleTable.Border(TableBorder.Rounded);
        titleTable.Expand();

        // Animate
        await AnsiConsole
            .Live(titleTable)
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

                Update(70, () => titleTable.AddRow($"[red bold] Testing Connection...[/]"));
                var conn = new MySqlConnection(settings.DBConnectionString);
                try
                {
                    await conn.OpenAsync();
                    Update(70, () => titleTable.AddRow($"[green bold] Connection Made Successfully...[/]"));
                }
                catch (Exception ex)
                {
                    Update(70, () => titleTable.AddRow($"[red bold]Error Connecting to Database: {ex.Message}[/]"));
                }
                finally
                {
                    Update(70, () => titleTable.AddRow($"[green bold] Cleaning up...[/]"));
                    if (conn.State == System.Data.ConnectionState.Open)
                        await conn.CloseAsync();
                    await conn.DisposeAsync();
                }

                Update(70, () => titleTable.AddRow("[green bold] Database Connection Test Complete[/]"));
            });
        return 0;
    }
}
