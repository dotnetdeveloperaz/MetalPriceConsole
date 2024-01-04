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
                    .AddCommand<MetalPriceCommand>("metalprice")
                    .WithAlias("price")
                    .WithDescription("Get Metal Price")
                    .WithExample(
                        new[]
                        {
                            "metalprice",
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
                            "--debug",
                            "--token",
                            "<token>"
                        }
                     );

                config
                    .AddCommand<AccountCommand>("account")
                    .WithAlias("acct")
                    .WithDescription("Retrieves account information.")
                    .WithExample(new[] { "account", "--fake", "--debug", "--hidden", "--token", "<token>" })
                    .WithExample(new[] { "acct", "--fake", "--debug", "--hidden", "--token", "<token>" });

                config
                    .AddCommand<StatusCommand>("status")
                    .WithDescription("Retrieves WebApi Status.")
                    .WithExample(new[] { "status", "--debug", "--hidden", "--token", "<token>" });

                config
                    .AddCommand<RestoreCommand>("restore")
                    .WithDescription("Restores Cache File.")
                    .WithExample(new[] { "restore", "--debug", "--hidden" });

                config
                    .AddCommand<TestDatabaseCommand>("testdb")
                    .WithDescription("Tests The Database Connection.")
                    .WithExample(new[] { "testdb", "--debug", "--hidden" });
#if DEBUG
                config.PropagateExceptions();
#endif
            });
            return app;
        }
    }
}
