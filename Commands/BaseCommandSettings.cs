using System.ComponentModel;
using Spectre.Console.Cli;

namespace MetalPriceConsole.Commands
{
    public class BaseCommandSettings : CommandSettings
    {
        [CommandOption("--debug")]
        [Description("Enable Debug Output")]
        [DefaultValue(false)]
        public bool Debug { get; set; }

        [CommandOption("--hidden")]
        [Description("Enable User Secret Debug Output")]
        [DefaultValue(false)]
        public bool ShowHidden { get; set; }

        [CommandOption("--fake")]
        [Description("Does Not Call WebApi")]
        [DefaultValue(false)]
        public bool Fake { get; set; }
    }
}
