using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Core;
using Serilog;
using Serilog.Events;
using CounterStrikeSharp.API;

namespace SurfTimer;

public class Injection : IPluginServiceCollection<SurfTimer>
{
    private static readonly string LogDirectory = $"{Server.GameDirectory}/csgo/addons/counterstrikesharp/logs";

    public void ConfigureServices(IServiceCollection services)
    {
        var fileName = $"log-SurfTimer-.txt"; // Date seems to be automatically appended so we leave it out
        var filePath = Path.Combine(LogDirectory, fileName);

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .WriteTo.File(
                path: filePath,
                restrictedToMinimumLevel: LogEventLevel.Verbose,
                rollingInterval: RollingInterval.Day
            )
            .CreateLogger();

        // Show the full path to the log file
        Console.WriteLine($"[SurfTimer] Logging to file: {filePath}");
        Log.Information("[SurfTimer] Logging to file: {LogFile}", filePath);

        // Register Serilog as a logging provider for Microsoft.Extensions.Logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // Register Dependencies
        services.AddScoped<ReplayRecorder>(); // Multiple instances for different players
        services.AddScoped<CurrentRun>(); // Multiple instances for different players
        services.AddScoped<ReplayPlayer>(); // Multiple instances for different players
        services.AddScoped<PersonalBest>(); // Multiple instances for different players
        services.AddScoped<PlayerStats>(); // Multiple instances for different players
        services.AddScoped<PlayerProfile>(); // Multiple instances for different players
        services.AddSingleton<Map>(); // Single instance for 1 Map object
    }
}

/*
public class Injection : IPluginServiceCollection<SurfTimer>
{
    public void ConfigureServices(IServiceCollection serviceCollection)
    {
        // Register Logging
        serviceCollection.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });


        // Register Dependencies
        serviceCollection.AddScoped<ReplayRecorder>(); // Multiple instances for different players
        serviceCollection.AddScoped<CurrentRun>(); // Multiple instances for different players
        serviceCollection.AddSingleton<Map>();  // Single instance for 1 Map object
    }
}
*/