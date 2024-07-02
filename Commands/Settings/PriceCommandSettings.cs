using System.ComponentModel;
using Spectre.Console.Cli;

namespace MetalPriceConsole.Commands.Settings
{
    public class PriceCommandSettings : BaseCommandSettings
    {
        [CommandOption("--currency <USD>")]
        [Description("Specify The Currency")]
        [DefaultValue("USD")]
        public string Currency { get; set; }

        [CommandOption("--start <date>")]
        [Description("Date Or Start Date To Get Price(s) For")]
        [DefaultValue("")]
        public string StartDate { get; set; }

        [CommandOption("--end <date>")]
        [Description("End Date To Get Price(s) For - Not Required For Single Day")]
        [DefaultValue("")]
        public string EndDate { get; set; }

        public bool GetAll { get; set; } = false;

        [CommandOption("--gold")]
        [Description("Get Gold Price - This is the default and is optional")]
        [DefaultValue(false)]
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

        [CommandOption("--file")]
        [Description("Cache File to Use - Override Default")]
        [DefaultValue(null)]
        public string CacheFile { get; set; } = null;
    }
}