using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GoldPriceConsole.Commands;

public class StatusCommand : Command<StatusCommand.Settings>
{
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
        return 0;
    }
}