using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Windows.Storage;

namespace SpotlightGallery.Services
{
    public static class ServiceLocator
    {
        public static IWallpaperService WallpaperService { get; } = new WallpaperService();

        public static LoggingLevelSwitch LogLevelSwitch { get; } = new LoggingLevelSwitch(LogEventLevel.Information);

        public static void Initialize()
        {
            // Initialize Serilog for logging
            string logFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "app.log");
            bool isDebugLogEnabled = SettingsHelper.GetSetting("DebugLogEnabled", false);
            LogLevelSwitch.MinimumLevel = isDebugLogEnabled ? LogEventLevel.Debug : LogEventLevel.Information;
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(LogLevelSwitch)
                .Enrich.FromLogContext()
                .WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [{Module}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            // Initialize the WallpaperService with settings
            int sourceIndex = SettingsHelper.GetSetting("Source", 0);
            int resolutionIndex = SettingsHelper.GetSetting("Resolution", 0);
            WallpaperService.ChangeSource((WallpaperSource)sourceIndex, resolutionIndex);

            WallpaperService.IsAutoSaveEnabled = SettingsHelper.GetSetting("AutoSave", false);
            string autoSaveDirectory = SettingsHelper.GetSetting("AutoSaveDirectory",
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "SpotlightGallery"));
            WallpaperService.AutoSaveDirectory = autoSaveDirectory;
        }
    }

}
