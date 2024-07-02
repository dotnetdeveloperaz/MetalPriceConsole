using System;
using System.Threading.Tasks;
using MetalPriceConsole.Commands.Settings;
using MetalPriceConsole.Models;
using Spectre.Console.Cli;

namespace MetalPriceConsole.Commands;

public class BaseCommand : AsyncCommand<BaseCommandSettings>
{
    private readonly ApiServer _apiServer;
    private static readonly string[] columns = new[] { "" };

    public BaseCommand(ApiServer apiServer)
    {
        _apiServer = apiServer;
    }
    protected async Task BaseExecuteAsync(CommandContext context, BaseCommandSettings settings)
    {
        // Run Common Tasks
        Title.Print();
        if (settings.Debug)
        {
            if (!DebugDisplay.Print(settings, _apiServer, "N/A"))
                return;
        }
        await Console.Out.WriteLineAsync("Execute Common Functionality");
    }
    public override async Task<int> ExecuteAsync(CommandContext context, BaseCommandSettings settings)
    { 
        // Run common functionality
        await BaseExecuteAsync(context, settings);

        // Derived classes provide their specific implementation
        return await ExecuteDerivedAsync(context, settings);  
    }
    protected virtual async Task<int> ExecuteDerivedAsync(CommandContext context, BaseCommandSettings settings)
    {
        // Default Implementation to be overridden by derived classes
        await Console.Out.WriteLineAsync("BaseCommand");
        return 0;
    }
}
