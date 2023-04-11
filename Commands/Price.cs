using System;
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GoldPriceConsole.Commands;

public class PriceCommand : Command<PriceCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Get Current Price.")]
        [DefaultValue(false)]
        public bool GetPrice { get; set; }

        [CommandOption("--date <date>")]
        [Description("Date To Get Price For")]
        public string Date { get; set; }

        [CommandOption("--debug")]
        [Description("Enable Debug Output")]
        [DefaultValue(false)]
        public bool Debug { get; set; }

        [CommandOption("--hidden")]
        [Description("Enable Secret Debug Output")]
        [DefaultValue(false)]
        public bool ShowHidden { get; set; }

        [CommandOption("--save")]
        [Description("Save Results")]
        [DefaultValue(false)]
        public bool Save { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        settings.GetPrice = true;
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        DateTime date;
        if (!DateTime.TryParse(settings.Date, out date))
            return ValidationResult.Error($"Invalid date - {settings.Date}");
        return base.Validate(context, settings);
    }
}
