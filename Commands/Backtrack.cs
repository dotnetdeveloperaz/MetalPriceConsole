using System;
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GoldPriceConsole.Commands;

public class BacktrackCommand : Command<BacktrackCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("--start <startdate>")]
        [Description("Start Date.")]
        public string StartDate { get; set; }

        [CommandOption("--end <enddate>")]
        [Description("End Date")]
        public string EndDate { get; set; }

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
        //AnsiConsole.MarkupLine($"Startdate: {settings.StartDate} Enddate: {settings.EndDate}");
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        DateTime startDate;
        DateTime endDate;
        if(!DateTime.TryParse(settings.EndDate, out endDate))
            return ValidationResult.Error($"Invalid end date - {settings.EndDate}");
        if(!DateTime.TryParse(settings.StartDate, out startDate))
            return ValidationResult.Error($"Invalid start date - {settings.StartDate}");
        return base.Validate(context, settings);
    }
}
