using Spectre.Console.Cli;
using System.ComponentModel;

namespace MetalPriceConsole.Commands
{
    public class PriceCommandSettings : BaseCommandSettings
    {
        [CommandOption("--currency <USD>")]
        [Description("Specify The Currency")]
        [DefaultValue("")]
        public string Currency { get; set; }

        [CommandOption("--gold")]
        [Description("Get Gold Price - This is the default and is optional")]
        [DefaultValue(true)]
        public bool GetGold { get; set; }

        [CommandOption("--palladium")]
        [Description("Get Palladium Price")]
        [DefaultValue(false)]
        public bool GetPalladium { get; set; }

        [CommandOption("--platinum")]
        [Description("Get Platinum Price")]
        [DefaultValue(false)]
        public bool GetPlatinum { get; set; }

        [CommandOption("--silver")]
        [Description("Get Silver Price")]
        [DefaultValue(false)]
        public bool GetSilver { get; set; }

        [CommandOption("--save")]
        [Description("Save Results")]
        [DefaultValue(false)]
        public bool Save { get; set; }

        [CommandOption("--cache")]
        [Description("Cache Results To File")]
        [DefaultValue(false)]
        public bool Cache { get; set; }
    }
}