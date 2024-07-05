using System;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using MetalPriceConsole.Commands.Settings;
using MetalPriceConsole.Models;

namespace MetalPriceConsole.Commands;

public abstract class BasePriceCommand<TSettings> : AsyncCommand<PriceCommandSettings>
{
    private readonly ApiServer _apiServer;
    private readonly ConnectionStrings _connectionStrings;
    private string _metalName = "Gold";
    private string _metal = "XAU";
    private static readonly string[] columns = new[] { "" };

    public string MetalName {  get { return _metalName; } }
    public string Metal { get { return _metal; } }

    public BasePriceCommand(ApiServer apiServer, ConnectionStrings connectionStrings)
    {
        _apiServer = apiServer;
        _connectionStrings = connectionStrings;
    }

    protected abstract Task<int> ExecuteDerivedAsync(CommandContext context, PriceCommandSettings settings);

    public override async Task<int> ExecuteAsync(CommandContext context, PriceCommandSettings settings)
    {
        Title.Print();
        settings.Currency ??= _apiServer.Currency;
        settings.CacheFile ??= _apiServer.CacheFile;
        if (settings.GetSilver)
        {
            _metalName = "Silver";
            _metal = _apiServer.Silver;
        }
        else if (settings.GetPalladium)
        {
            _metalName = "Palladium";
            _metal = _apiServer.Palladium;
            // Historical data is not supported yet, so we can only get the current day
            // Commenting out, will move to PriceCommand as ViewCommand inherits and
            // we can obviously query the database or cache file by date
            //settings.StartDate = String.Empty;
            //settings.EndDate = String.Empty;
        }
        else if (settings.GetPlatinum)
        {
            _metalName = "Platinum";
            _metal = _apiServer.Platinum;
            // Historical data is not supported yet, so we can only get one day
            // Commenting out, will move to PriceCommand as ViewCommand inherits and
            // we can obviously query the database or cache file by date
            //settings.StartDate = String.Empty;
            //settings.EndDate = String.Empty;
        }
        else if (settings.GetGold)
        {
            _metal = _apiServer.Gold;
            settings.GetGold = true;
        }
        string url = $"{_apiServer.BaseUrl}{_metal}/{settings.Currency}";
        if (settings.Debug)
        {
            string connectionString = _connectionStrings.DefaultDB == null ? "" : _connectionStrings.DefaultDB;
            if (!DebugDisplay.Print(settings, _apiServer, connectionString, url))
                return 0;
        }

        // Derived classes provide their specific implementation
        return await ExecuteDerivedAsync(context, settings);
    }
    public override ValidationResult Validate(CommandContext context, PriceCommandSettings settings)
    {

        if (!context.Name.Contains("view"))
        { 
            if (settings.StartDate == "")
                settings.StartDate = DateTime.Now.ToString("yyyy-MM-dd");
            if (!DateTime.TryParse(settings.StartDate, out _))
                return ValidationResult.Error($"Invalid date - {settings.StartDate}");
            if (settings.EndDate == "")
                settings.EndDate = settings.StartDate;
            if (!DateTime.TryParse(settings.EndDate, out _))
                return ValidationResult.Error($"Invalid date - {settings.EndDate}");
        }

        settings.GetAll = !settings.GetGold && !settings.GetSilver && !settings.GetPalladium && !settings.GetPlatinum;
        return base.Validate(context, settings);
    }
}
