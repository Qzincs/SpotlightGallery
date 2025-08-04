using System;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using SpotlightGallery.Services;
using Serilog;
using Serilog.Context;

namespace SpotlightGallery.BackgroundTasks
{
    // Background task implementation.
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("74b929df-d324-4dc0-903d-0604c0ba37c0")]
    [ComSourceInterfaces(typeof(IBackgroundTask))]
    public class WallpaperUpdateTask : IBackgroundTask, IDisposable
    {
        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Stop the server when the background task is disposed.
                    SpotlightGallery.Program.SignalExit();
                }
                disposed = true;
            }
        }

        ~WallpaperUpdateTask()
        {
            Dispose(false);
        }

        /// <summary>
        /// This method is the main entry point for the background task. The system will believe this background task
        /// is complete when this method returns.
        /// </summary>
        [MTAThread]
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            ServiceLocator.Initialize();
            using (LogContext.PushProperty("Module", nameof(WallpaperUpdateTask)))
            {
                Log.Debug("WallpaperUpdateTask started.");

                // Wire the cancellation handler.
                taskInstance.Canceled += this.OnCanceled;

                DateTimeOffset now = DateTimeOffset.Now;
                DateTimeOffset nextUpdateTime = SettingsHelper.GetSetting("NextUpdateTime", DateTimeOffset.Now);

                // Check if the current time is past the next update time
                if (now > nextUpdateTime)
                {
                    Log.Information("Current time is past the next update time {NextUpdateTime}. Proceeding with wallpaper update.", nextUpdateTime);
                    try
                    {
                        var wallpaperService = ServiceLocator.WallpaperService;
                        // Load the settings for the wallpaper source and resolution
                        int sourceIndex = SettingsHelper.GetSetting("Source", 0);
                        int resolutionIndex = SettingsHelper.GetSetting("Resolution", 0);
                        wallpaperService.AutoSaveDirectory = SettingsHelper.GetSetting("AutoSaveDirectory",
                            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "SpotlightGallery"));
                        wallpaperService.ChangeSource((WallpaperSource)sourceIndex, resolutionIndex);

                        var wallpaper = wallpaperService.DownloadWallpaperAsync().GetAwaiter().GetResult();
                        bool success = wallpaperService.SetWallpaperAsync(wallpaper.path).GetAwaiter().GetResult();

                        if (success)
                        {
                            Log.Information("Wallpaper updated successfully: {WallpaperPath}", wallpaper.path);
                            // Toast notifications only accept images from three URI schemes: http(s)://, ms-appx:///, and ms-appdata:///.
                            // For http/https images, there is a file size limit (3 MB on normal connections, 1 MB on metered connections; previously 200 KB).
                            // Therefore, we use a URL and reduce the image resolution to ensure the image size meets the requirement.
                            string url = wallpaper.url.Replace("_UHD", "_800x600");
                            Helpers.ToastHelper.ShowToast("Wallpaper Updated", $"Today's wallpaper is {wallpaper.title}.", url);

                            // Update the next update time based on the update mode
                            int updateModeIndex = SettingsHelper.GetSetting("UpdateMode", 0);
                            if (updateModeIndex == 0) // daily
                            {
                                nextUpdateTime = now.Date.AddDays(1);
                            }
                            else // at specific time
                            {
                                TimeSpan updateTime = SettingsHelper.GetSetting("UpdateTime", TimeSpan.FromHours(12));
                                nextUpdateTime = now.Date.Add(updateTime);
                                if (nextUpdateTime <= now)
                                {
                                    nextUpdateTime = nextUpdateTime.AddDays(1);
                                }
                            }
                            SettingsHelper.SaveSetting("NextUpdateTime", nextUpdateTime);
                        }
                        else
                        {
                            Log.Error("Failed to set wallpaper: {WallpaperPath}", wallpaper.path);
                            Helpers.ToastHelper.ShowToast("Wallpaper Update Failed", "Please check your network connection.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "WallpaperUpdateTask Exception: {Message}", ex.Message);
                        Helpers.ToastHelper.ShowToast("Wallpaper Update Failed", "Please check your network connection.");
                    }
                }
            }
        }



        /// <summary>
        /// This method is signaled when the system requests the background task be canceled. This method will signal
        /// to the Run method to clean up and return.
        /// </summary>
        [MTAThread]
        public void OnCanceled(IBackgroundTaskInstance taskInstance, BackgroundTaskCancellationReason cancellationReason)
        {
            // Unregister the task when the task is destroyed.
            SpotlightGallery.Program.SignalExit();
        }
    }
}
