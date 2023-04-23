using GoldPriceConsole.Commands;
using Spectre.Console.Cli;

namespace GoldPriceConsole
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
                    .AddCommand<AccountCommand>("account")
                    .WithDescription("Retrieves account information.");

                config
                    .AddCommand<HistoryCommand>("history")
                    .WithDescription(
                        "Retrieves historical gold prices. Use --save to save to the database.\r\nWeekends and holidays are skipped because markets are closed."
                    )
                    .WithExample(
                        new[]
                        {
                        "history",
                        "--start",
                        "YYYY-MM-DD",
                        "--end",
                        "YYYY-MM-DD",
                        "--debug",
                        "--hidden"
                        }
                    );

                config
                    .AddCommand<PriceCommand>("price")
                    .WithDescription(
                        "Retrieves the current gold price. Use --save to save to database. Weekends and holidays are skipped."
                    )
                    .WithExample(
                        new[] { "price", "--date", "YYYY-MM-DD", "--save", "--debug", "--hidden" }
                );
                config
                    .AddCommand<AccountCommand>("acct")
                    .WithDescription("Retrieves Account Statistics.")
                    .WithExample(new[] { "acct", "--debug", "--hidden" });

                config
                    .AddCommand<StatusCommand>("status")
                    .WithDescription("Retrieves WebApi Status.")
                    .WithExample(new[] { "status", "--debug", "--hidden" });

                config
                    .AddCommand<RestoreCommand>("restore")
                    .WithDescription("Restores Cache File.")
                    .WithExample(new[] { "restore", "--debug", "--hidden" });

#if DEBUG
                config.PropagateExceptions();
#endif
            });
            return app;
        }
    }
}