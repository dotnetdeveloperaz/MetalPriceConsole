using System;
using System.Data;
using System.IO;
using System.Reflection;
using Spectre.Console;

namespace MetalPriceConsole;

public class Title
{
    public static void Print()
    {
        AssemblyName assembly = typeof(Title).Assembly.GetName();
        var version = $"v{assembly.Version.Major}.{assembly.Version.Minor}";
        Console.Clear();
        var titleTable = new Table().Centered();
        titleTable.AddColumn(
            new TableColumn(
                new Markup(
                    $"[yellow]Metal :pick:  Price Console[/] {version}\r\n[green bold italic]Written By Scott Glasgow[/]"
                )
            ).Centered()
        );
        titleTable.BorderColor(Color.Yellow);
        titleTable.Border(TableBorder.Rounded);
        titleTable.Expand();

        AnsiConsole.Write(titleTable);

        if(File.Exists("MetalPrice.cache"))
        {
            var table = new Table().Centered();
            table.AddColumn(new TableColumn(new Markup("[red bold italic]Cache file exists. Use restore to load to database.[/]").Centered()));
            table.BorderColor(Color.Yellow);
            table.Border(TableBorder.Rounded);
            table.Expand();
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine("");
        }
    }
}