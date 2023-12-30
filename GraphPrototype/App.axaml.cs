using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ScottPlot.Avalonia;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using System;
using System.IO;

namespace GraphPrototype
{
    public class App : Application
    {
        public static string LogPath => Path.Combine(Directory.GetCurrentDirectory(), "log.txt");

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            SetupLogger();
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static void SetupLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithExceptionDetails()
                .MinimumLevel.Information()
                .WriteTo.File(LogPath, LogEventLevel.Verbose, "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", null, 1073741824L, null, buffered: false, shared: false, null, RollingInterval.Day, rollOnFileSizeLimit: false, 31)
                .CreateLogger();
            Log.Debug("Log Created");
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Fatal("Unhandled exception {0}", e.ExceptionObject);
        }
    }
}
