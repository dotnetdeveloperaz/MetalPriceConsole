using System;
using System.ComponentModel;
using System.Data;
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
                    "[yellow bold]Running Database Configuration Test[/]"
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

                Update(70, () => titleTable.AddRow($"[blue bold]Testing Connection...[/]"));
                var conn = new MySqlConnection(settings.DBConnectionString);
                var sqlCommand = new MySqlCommand();
                sqlCommand.Connection = conn;
                sqlCommand.CommandType = CommandType.Text;
                try
                {
                    await conn.OpenAsync();
                    Update(70, () => titleTable.AddRow($"[green bold]Connection Made Successfully...[/]"));

                    // Want to add tests to ensure table exists, and both stored procedures exist.
                    string procs = "SELECT COUNT(*) FROM information_schema.ROUTINES WHERE ROUTINE_NAME LIKE 'usp_%MetalPrice%';";
                    string table = "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_NAME = 'MetalPrices';";

                    Update(70, () => titleTable.AddRow($"[blue bold]Verifying Table Exists...[/]"));
                    sqlCommand.CommandText = table;
                    var recs = sqlCommand.ExecuteScalar();
                    if (recs.ToString() == "1")
                        Update(70, () => titleTable.AddRow($"[green bold]Verified Table Exists....[/]"));
                    else
                        Update(70, () => titleTable.AddRow($"[red bold]Table DOES NOT Exists....[/]"));

                    sqlCommand.CommandText = procs;
                    recs = sqlCommand.ExecuteScalar();

                    Update(70, () => titleTable.AddRow($"[blue bold]Verifying The 2 Stored Procedures Exist...[/]"));
                    if (recs.ToString() == "2")
                        Update(70, () => titleTable.AddRow($"[green bold]Verified {recs} Stored Procedures Exist...[/]"));
                    else
                        Update(70, () => titleTable.AddRow($"[red bold]Both Stored Procedures DO NOT Exists, Count {recs}....[/]"));
                                    
                }
                catch (Exception ex)
                {
                    Update(70, () => titleTable.AddRow($"[red bold]Error Connecting to Database: {ex.Message}[/]"));
                }
                finally
                {
                    Update(70, () => titleTable.AddRow($"[green bold]Cleaning up...[/]"));
                    sqlCommand.Dispose();
                    if (conn.State == System.Data.ConnectionState.Open)
                        await conn.CloseAsync();
                    await conn.DisposeAsync();
                }

                Update(70, () => titleTable.AddRow("[green bold]Database Connection Test Complete[/]"));
            });
        return 0;
    }
}
