using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
