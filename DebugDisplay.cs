using System;
using System.Reflection;
using System.Threading;
using Spectre.Console;
using Spectre.Console.Cli;
using MetalPriceConsole.Models;

namespace MetalPriceConsole;

public class DebugDisplay
{
    internal static void Print(CommandSettings settings, ApiServer server, string Url)
    {
        Print(settings, server, "", Url);
    }
    internal static void Print(CommandSettings settings, ApiServer server, string connectionString, string Url)
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
                        Update(70, () => table.AddRow($"Url:", $"{Url}"));
                    }
                    foreach (PropertyInfo property in properties)
                    {
                        object value = property.GetValue(settings);
 
                        if (property.PropertyType == typeof(bool))
                            Update(70, () => table.AddRow($"{property.Name}", $"[yellow]{(bool)value}[/]"));
                        else
                            if (value is not null)
                                Update(70, () => table.AddRow($"{property.Name}", $"{value}"));
                    }
                }
            });
    }
}
