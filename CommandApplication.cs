using MetalPriceConsole.Commands;
using Spectre.Console.Cli;

namespace MetalPriceConsole
{
    public class CommandApplication
    {
        public static CommandApp Initialize(CommandApp app)
        {
            // Configure Spectre Cli
            app.Configure(config =>
            {
                config.ValidateExamples();

                config
                    .AddCommand<PriceCommand>("price")
                    .WithDescription(
                        "Gets the specified Metal Price for the days specified. "
                        + "Default is current day, and gold.\n"
                        + "Does not save results. Use --save to save to configured database OR "
                        + "--save --cache to save to configured cachefile. " 
                        + "Use with --cachefile </path/filename to override.\n"
                    )
                    .WithExample(
                        new[]
                        {
                            "price",
                            "--start",
                            "YYYY-MM-DD",
                            "--end",
                            "YYYY-MM-DD",
                            "--currency",
                            "USD",
                            "--gold",
                            "--palladium",
                            "--platinum",
                            "--silver",
                            "--fake",
                            "--save",
                            "--cache",
                            "--cachefile",
                            "<file>",
                            "--debug",
                            "--token",
                            "<token>"
                        }
                     );

                config
                    .AddCommand<ViewCommand>("view")
                    .WithDescription(
                        "Works like the Price command except it displays data from " 
                        + "the configured database or from the configured cachefile with --cache. "
                        + "Use with --cachefile </path/filename to override.\n"
                    )
                    .WithExample(
                        new[]
                        {
                            "view",
                            "--start",
                            "YYYY-MM-DD",
                            "--end",
                            "YYYY-MM-DD",
                            "--currency",
                            "USD",
                            "--gold",
                            "--palladium",
                            "--platinum",
                            "--silver",
                            "--fake",
                            "--cache",
                            "--cachefile",
                            "<file>",
                            "--debug",
                            "--token",
                            "<token>"
                        }
                     );

                config
                    .AddCommand<RestoreCommand>("restore")
                    .WithDescription(
                        "Restores cachefile to the configured database and deletes the cachefile.\n"
                        + "To override configured cache file, use the --cachefile </path/filename> switch.\n"
                        )
                    .WithExample(new[] { "restore", "--cachefile", "<filename>", "--debug", "--hidden" });

                config
                    .AddCommand<CacheStatsCommand>("cachestats")
                    .WithAlias("cstats")
                    .WithDescription(
                        "Displays the cachefile statistics, number of records for each metal, start and end dates.\n" 
                        + "To override configured cache file, use the --cachefile </path/filename> switch.\n"
                        )
                    .WithExample(new[] { "cachestats", "--cachefile",  "<filename>" });

                config
                    .AddCommand<TestDatabaseCommand>("testdb")
                    .WithDescription(
                        "Tests the configured database connection.\n"
                        + "Use the --db \"<YourConnectionString>\" (Quotes Required!) to test connectionstrings for diagnosing.\n"
                        + "This swtich is NOT available with any other command.\n"
                        )
                    .WithExample(new[] { "testdb", "--db", "'<YourDBConnectionString>'", "--debug", "--hidden" });

                config
                    .AddCommand<AccountCommand>("account")
                    .WithAlias("acct")
                    .WithDescription("Retrieves WebApi account information, such as number of calls made, etc.")
                    .WithExample(new[] { "account", "--fake", "--debug", "--hidden", "--token", "<token>" });

                config
                    .AddCommand<StatusCommand>("status")
                    .WithDescription("Retrieves WebApi Status.")
                    .WithExample(new[] { "status", "--debug", "--hidden", "--token", "<token>" });

#if DEBUG
                config.PropagateExceptions();
#endif
            });
            return app;
        }
    }
}
