using System;
using System.Reflection;
using System.Threading;
using Spectre.Console;
using Spectre.Console.Cli;
using MetalPriceConsole.Models;

namespace MetalPriceConsole;

public class DebugDisplay
{
    internal static bool Print(CommandSettings settings, ApiServer server, string Url)
    {
        return Print(settings, server, "", Url);
    }
    internal static bool Print(CommandSettings settings, ApiServer server, string connectionString, string Url)
    {
        // Debug Window
        var table = new Table().Centered();
        table.BorderColor(Color.BlueViolet);
        table.Border(TableBorder.DoubleEdge);
        table.Expand();

        // Columns
        table.AddColumn("Setting Key");
        table.AddColumn("Value");

        // Column alignment
        table.Columns[0].RightAligned();
        table.Columns[1].RightAligned();

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
                    table.AddRow($"Database Connection:", $"{connectionString}");

                table.AddRow($"Token:", $"{server.Token}");
                table.AddRow($"Url:", $"{Url}");
            }
            foreach (PropertyInfo property in properties)
            {
                object value = property.GetValue(settings);

                if (property.PropertyType == typeof(bool))
                    table.AddRow($"{property.Name}", $"[yellow]{(bool)value}[/]");
                else
                    if (value is not null)
                    table.AddRow($"{property.Name}", $"{value}");
            }
            AnsiConsole.Write(table);
            if (AnsiConsole.Confirm("Continue?"))
                Title.Print();
            else
                return false;

        }
        return true;
    }
}
