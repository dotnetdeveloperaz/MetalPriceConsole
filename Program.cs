using GoldPriceConsole.Commands;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Newtonsoft.Json;
using PublicHoliday;
using RestSharp;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using GoldPriceConsole.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GoldPriceConsole;

class Program
{
    public static async Task Main(string[] args)
    {
        Title.Print();

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
        var logging = config.GetSection("Logging");
        var database = config.GetSection("ConnectionStrings");
        var apiServer = config.GetSection("ApiServer");
        var services = new ServiceCollection();
        services.AddSingleton(new ApiServer() { Token = apiServer["Token"], BaseUrl = apiServer["BaseUrl"], 
            DefaultMetal = apiServer["DefaultMetal"], MonthlyAllowance = apiServer["MonthlyAllowance"] });
        services.AddSingleton(new ConnectionStrings() { DefaultDB = database["DefaultDB"] });
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConfiguration(config.GetSection("Logging"));
            loggingBuilder.AddEventSourceLogger();
        });

        services.AddSingleton<GoldPriceEventSource>();
        services.BuildServiceProvider();
        return services;
    }
}
