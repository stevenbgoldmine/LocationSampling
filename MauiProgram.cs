using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Serilog;
using LocationSampling.Services;

namespace LocationSampling
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.Console(Serilog.Events.LogEventLevel.Warning)
                .CreateLogger();

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                // Initialize the .NET MAUI Community Toolkit by adding the below line of code
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                }).AddConfigurationAndLogging();

            builder.Services.AddSingleton<MainPageViewModel>();
            builder.Services.AddTransient<MainPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif
            var app = builder.Build();

            // Initialize the ServiceLocator with the built service provider
            ServiceLocator.Initialize(app.Services);

            return builder.Build();
        }

        public static void AppShutdown()
        {
            Log.Logger.Information("App Shutdown");
            Log.CloseAndFlush();
        }

        public static MauiAppBuilder AddConfigurationAndLogging(this MauiAppBuilder builder)
        {
            var assembly = new EmbeddedFileProvider(typeof(App).Assembly, typeof(App).Namespace);
            var configBuilder = new ConfigurationBuilder().AddJsonFile(assembly, "appsettings.json", false, false);
#if DEBUG
            configBuilder.AddJsonFile(assembly, $"appsettings.Debug.json", true, false);
#else
            configBuilder.AddJsonFile(assembly, $"appsettings.json", true, false);
#endif
            var config = configBuilder.Build();

            builder.Configuration.AddConfiguration(config);

            // All logging goes in same log
            // - have in the past used logfactory to split up logs (eg. video / devices separate)
            // -> may be possible to split using filter but not sure imp right now
            // NB. 'Using' list maintained in appsettings to help Android find assemblies for each sink
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config) // Reads Serilog section
                .WriteTo.Async(f => f.File(Path.Combine(App.AppLogFolder, "log.txt"), shared: true,
                    //restrictedToMinimumLevel: LogEventLevel.Information, 
                    rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7,
                    fileSizeLimitBytes: 1048576, rollOnFileSizeLimit: true))
                .WriteTo.Console(); // TODO: may remove - ie. no console for deployed apps (may be useful for app debug?)
#if DEBUG
            logger.WriteTo.Debug(); // VS debug output
#endif

            // Configure on static inst to cover any cases where components use static logger rather than injected.
            // Prefer to use injected (picks up class name)
            Log.Logger = logger.CreateLogger();

            // add static serilog to logging pipeline
            builder.Logging.AddSerilog(Log.Logger, dispose: true);

            return builder;
        }

    }
}
