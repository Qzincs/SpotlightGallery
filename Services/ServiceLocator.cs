using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Windows.Storage;

namespace SpotlightGallery.Services
{
    public static class ServiceLocator
    {
        public static IWallpaperService WallpaperService { get; } = new WallpaperService();

        public static void Initialize()
        {
            // Initialize Serilog for logging
            string logFilePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "app.log");
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
                .CreateLogger();
        }
    }

}
