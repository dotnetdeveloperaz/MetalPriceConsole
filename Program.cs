using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Threading.Tasks;
using MetalPriceConsole.Models;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Reflection;

namespace MetalPriceConsole;

class Program
{
    public static async Task Main(string[] args)
    {
        // Configuration
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<Program>()
            .Build();

        IServiceCollection serviceCollection = ConfigureServices(config);
        var registrar = new TypeRegistrar(serviceCollection);

        var app = new CommandApp(registrar);
        app.Configure(configure => CommandApplication.Initialize(app));

        Title.Print();

        try
        {
            await app.RunAsync(args);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        }
        if (args.Length == 0)
            return;
    }
    public static IServiceCollection ConfigureServices(IConfiguration config)
    {
        //var logging = config.GetSection("Logging");
        string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string file = Path.Combine(path, "MetalPrice.cache");
        var database = config.GetSection("ConnectionStrings");
        var apiServer = config.GetSection("ApiServer");
        var services = new ServiceCollection();
        services.AddSingleton(new ApiServer() { 
            Token = apiServer["Token"], CacheFile = apiServer["CacheFile"], CacheFileExits = File.Exists(file),
            BaseUrl = apiServer["BaseUrl"], Gold = apiServer["Gold"], Palladium = apiServer["Palladium"], 
            Platinum = apiServer["Platinum"], Silver = apiServer["Silver"], Currency= apiServer["Currency"], 
            HistoricalStartDate = apiServer["HistoricalStartDate"], MonthlyAllowance = apiServer["MonthlyAllowance"] 
        });
        
        services.AddSingleton(new ConnectionStrings() { DefaultDB = database["DefaultDB"] });
/*
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConfiguration(config.GetSection("Logging"));
            loggingBuilder.AddEventSourceLogger();
        });

        services.AddSingleton<MetalPriceEventSource>();
*/
        services.BuildServiceProvider();
        return services;
    }
}
