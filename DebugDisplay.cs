using System;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using GoldPriceConsole.Models;
using GoldPriceConsole.Commands;
using System.Security.Principal;
using Newtonsoft.Json.Linq;

namespace GoldPriceConsole;

public class DebugDisplay
{
    internal static void Print(CommandSettings settings, ApiServer server, ILogger logger)
    {
        Print(settings, server, logger, "");
    }
    internal static void Print(CommandSettings settings, ApiServer server, ILogger logger, string connectionString)
    {
        // Debug Window
        var table = new Table().Centered();
        //table.HideHeaders();
        table.Expand();

        // Animate
        AnsiConsole
            .Live(table)
            .AutoClear(false)
            .Overflow(VerticalOverflow.Ellipsis)
            .Cropping(VerticalOverflowCropping.Top)
            .Start(ctx =>
            {
                void Update(int delay, Action action)
                {
                    action();
                    ctx.Refresh();
                    Thread.Sleep(delay);
                }

                // Columns
                Update(70, () => table.AddColumn(""));
                Update(70, () => table.AddColumn(""));

                // Column alignment
                Update(70, () => table.Columns[0].RightAligned());
                Update(70, () => table.Columns[1].RightAligned());

                // Borders
                Update(70, () => table.BorderColor(Color.Yellow));
                Update(70, () => table.MinimalBorder());
                Update(70, () => table.SimpleBorder());

                bool isDebug = false;
                bool showHidden = false;
                PropertyInfo[] properties = settings.GetType().GetProperties();
                foreach (PropertyInfo property in properties)
                {
                    object value = property.GetValue(settings);
                    if (property.Name == "Debug")
                        isDebug = (bool)value;
                    if (property.Name == "ShowHidden")
                        showHidden = (bool)value;
                }
                if (isDebug)
                {
                    if (showHidden)
                    {
                        if (connectionString != String.Empty)
                            Update(70, () => table.AddRow($"Database Connection:", $"{connectionString}"));
                        Update(70, () => table.AddRow($"Token:", $"{server.Token}"));
                    }
                    foreach (PropertyInfo property in properties)
                    {
                        object value = property.GetValue(settings);
 
                        if (property.PropertyType == typeof(bool))
                            Update(70, () => table.AddRow($"{property.Name}", $"[yellow]{(bool)value}[/]"));
                        else
                            if ((object)value != null)
                            Update(70, () => table.AddRow($"{property.Name}", $"{value}"));
                    }
                }
            });
    }
}
