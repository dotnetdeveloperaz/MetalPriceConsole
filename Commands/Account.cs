using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GoldPriceConsole.Commands;

public class AccountCommand : Command<AccountCommand.Settings>
{
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
        return 0;
    }
}
