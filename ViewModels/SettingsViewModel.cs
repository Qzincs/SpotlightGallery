using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Serilog;
using Serilog.Context;
using SpotlightGallery.Helpers;
using SpotlightGallery.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Background;

namespace SpotlightGallery.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly IWallpaperService wallpaperService = ServiceLocator.WallpaperService;
        private static Windows.ApplicationModel.Resources.ResourceLoader ResourceLoader =>
            Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse();

        private bool isInitialized = false;

        public string AppVersion
        {
            get
            {
                var v = Windows.ApplicationModel.Package.Current.Id.Version;
                return $"{v.Major}.{v.Minor}.{v.Build}";
            }
        }

        private int appThemeIndex;
        /// <summary>
        /// 0 - 浅色, 1 - 深色, 2 - 系统默认
        /// </summary>
        public int AppThemeIndex
        {
            get => appThemeIndex;
            set
            {
                if (SetProperty(ref appThemeIndex, value) && isInitialized)
                {
                    SettingsHelper.SaveSetting("AppTheme", value);
                    ThemeManager.ApplyTheme(value);
                }
            }
        }

        public ObservableCollection<string> ResolutionOptions { get; } = new();

        private int sourceIndex;

        /// <summary>
        /// 0 - Spotlight, 1 - Bing
        /// </summary>
        public int SourceIndex
        {
            get => sourceIndex;
            set
            {
                if (SetProperty(ref sourceIndex, value) && isInitialized)
                {
                    SettingsHelper.SaveSetting("Source", value);
                    UpdateResolutionOptions();
                    ResolutionIndex = 0;
                }
            }
        }

        private int resolutionIndex;

        /// <summary>
        /// Spotlight: 0 - 3840x2160(Spotlight Desktop), 1 - 1920x1080(Spotlight Lockscreen)
        /// </summary>
        public int ResolutionIndex
        {
            get => resolutionIndex;
            set
            {
                if (SetProperty(ref resolutionIndex, value) && isInitialized)
                {
                    SettingsHelper.SaveSetting("Resolution", value);
                    wallpaperService.ChangeSource((WallpaperSource)SourceIndex, value);
                }
            }
        }

        private void UpdateResolutionOptions()
        {
            ResolutionOptions.Clear();
            if ((WallpaperSource)SourceIndex == WallpaperSource.Spotlight)
            {
                ResolutionOptions.Add("3840x2160");
                ResolutionOptions.Add("1920x1080");
            }
            else if ((WallpaperSource)SourceIndex == WallpaperSource.BingDaily)
            {
                ResolutionOptions.Add("3840x2160");
                ResolutionOptions.Add("1920x1200");
                ResolutionOptions.Add("1920x1080");
                ResolutionOptions.Add("1366x768");
                ResolutionOptions.Add("1280x768");
                ResolutionOptions.Add("1024x768");
                ResolutionOptions.Add("800x600");
            }
        }

        private int wallpaperLocaleIndex;
        public int WallpaperLocaleIndex
        {
            get => wallpaperLocaleIndex;
            set
            {
                if (SetProperty(ref wallpaperLocaleIndex, value) && isInitialized)
                {
                    SettingsHelper.SaveSetting("WallpaperLocale", value);
                    wallpaperService.CurrentLocale = (WallpaperLocale)value;
                }
            }
        }

        private bool isAutoUpdateEnabled;
        public bool IsAutoUpdateEnabled
        {
            get => isAutoUpdateEnabled;
            set
            {
                if (SetProperty(ref isAutoUpdateEnabled, value) && isInitialized)
                {
                    SettingsHelper.SaveSetting("AutoUpdate", value);

                    if (value)
                    {
                        DateTimeOffset nextUpdateTime = CalculateNextUpdateTime();
                        SettingsHelper.SaveSetting("NextUpdateTime", nextUpdateTime);

                        _ = RegisterWallpaperUpdateTaskAsync();
                    }
                    else
                    {
                        UnregisterWallpaperUpdateTask();
                    }
                }
            }
        }

        private int updateModeIndex;
        /// <summary>
        /// 0 - daily, 1 - at specific time
        /// </summary>
        public int UpdateModeIndex
        {
            get => updateModeIndex;
            set
            {
                if (SetProperty(ref updateModeIndex, value) && isInitialized)
                {
                    SettingsHelper.SaveSetting("UpdateMode", value);
                    SettingsHelper.SaveSetting("NextUpdateTime", CalculateNextUpdateTime());

                    OnPropertyChanged(nameof(IsUpdateTimeEnabled));
                }
            }
        }

        public bool IsUpdateTimeEnabled => UpdateModeIndex == 1;

        private TimeSpan updateTime;
        /// <summary>
        /// time when the wallpaper should be updated if UpdateModeIndex is set to 1 (at specific time)
        /// </summary>
        public TimeSpan UpdateTime
        {
            get => updateTime;
            set
            {
                if (SetProperty(ref updateTime, value) && isInitialized)
                {
                    SettingsHelper.SaveSetting("UpdateTime", value);
                    DateTimeOffset nextUpdateTime = CalculateNextUpdateTime();
                    SettingsHelper.SaveSetting("NextUpdateTime", nextUpdateTime);
                }
            }
        }

        private DateTimeOffset CalculateNextUpdateTime()
        {
            if (UpdateModeIndex == 0) // daily
            {
                return DateTimeOffset.Now.Date.AddDays(1);
            }
            else // at specific time
            {
                var nextUpdateTime = DateTimeOffset.Now.Date.Add(UpdateTime);
                if (nextUpdateTime <= DateTimeOffset.Now)
                {
                    nextUpdateTime = nextUpdateTime.AddDays(1);
                }
                return nextUpdateTime;
            }
        }

        public async Task RegisterWallpaperUpdateTaskAsync()
        {
            using (LogContext.PushProperty("Module", nameof(SettingsViewModel)))
            {
                try
                {
                    foreach (var task in BackgroundTaskRegistration.AllTasks)
                    {
                        if (task.Value.Name == "WallpaperUpdateTask")
                        {
                            task.Value.Unregister(true);
                        }
                    }

                    var status = await BackgroundExecutionManager.RequestAccessAsync();
                    if (status == BackgroundAccessStatus.DeniedByUser || status == BackgroundAccessStatus.DeniedBySystemPolicy)
                        return;

                    var builder = new BackgroundTaskBuilder
                    {
                        Name = "WallpaperUpdateTask",
                    };
                    builder.SetTaskEntryPointClsid(typeof(BackgroundTasks.WallpaperUpdateTask).GUID);

                    // run background task every 4 hours to check next update time
                    builder.SetTrigger(new TimeTrigger(240, false));

                    builder.Register();

                    bool isRegistered = BackgroundTaskRegistration.AllTasks
                        .Any(t => t.Value.Name == "WallpaperUpdateTask");

                    if (isRegistered)
                    {
                        Log.Information("WallpaperUpdateTask registered successfully.");
                    }
                    else
                    {
                        Log.Error("Failed to register WallpaperUpdateTask.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "RegisterWallpaperUpdateTaskAsync Exception: {Message}", ex.Message);
                }
            }
        }

        private void UnregisterWallpaperUpdateTask()
        {
            // 取消注册任务
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == "WallpaperUpdateTask")
                {
                    task.Value.Unregister(true);
                    using (LogContext.PushProperty("Module", nameof(SettingsViewModel)))
                    {
                        Log.Information("WallpaperUpdateTask unregistered successfully.");
                    }
                    break;
                }
            }
        }

        private bool isAutoSaveEnabled;
        public bool IsAutoSaveEnabled
        {
            get => isAutoSaveEnabled;
            set
            {
                if (SetProperty(ref isAutoSaveEnabled, value) && isInitialized)
                {
                    SettingsHelper.SaveSetting("AutoSave", value);
                    ServiceLocator.WallpaperService.IsAutoSaveEnabled = value;
                }
            }
        }

        private string autoSaveDirectory = string.Empty;
        public string AutoSaveDirectory
        {
            get => autoSaveDirectory;
            set
            {
                if (SetProperty(ref autoSaveDirectory, value) && isInitialized)
                {
                    SettingsHelper.SaveSetting("AutoSaveDirectory", value);
                    ServiceLocator.WallpaperService.AutoSaveDirectory = value;
                }
            }
        }

        public ICommand OpenAutoSaveDirectoryCommand => new RelayCommand(OpenAutoSaveDirectory);
        private async void OpenAutoSaveDirectory()
        {
            if (Directory.Exists(AutoSaveDirectory))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = AutoSaveDirectory,
                    UseShellExecute = true
                });
            }
            else
            {
                var mainWindow = App.StartupWindow;
                var loader = ResourceLoader;
                string title = loader.GetString("FolderNotFoundDialog_Title");
                string content = loader.GetString("FolderNotFoundDialog_Content");
                string button = loader.GetString("FolderNotFoundDialog_Button");
                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    Title = title,
                    Content = content,
                    PrimaryButtonText = button,
                    DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
                    RequestedTheme = (mainWindow.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default,
                    XamlRoot = mainWindow.Content.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
                {
                    ChangeAutoSaveDirectory();
                }
            }
        }

        public ICommand ChangeAutoSaveDirectoryCommand => new RelayCommand(ChangeAutoSaveDirectory);
        private async void ChangeAutoSaveDirectory()
        {
            var picker = new Windows.Storage.Pickers.FolderPicker
            {
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary
            };
            picker.FileTypeFilter.Add("*"); // allow all file types

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.StartupWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                AutoSaveDirectory = folder.Path;
            }
        }

        private int displayLanguageIndex;
        public int DisplayLanguageIndex
        {
            get => displayLanguageIndex;
            set
            {
                if (SetProperty(ref displayLanguageIndex, value) && isInitialized)
                {
                    SettingsHelper.SaveSetting("DisplayLanguage", value);
                    ChangeDisplayLanguage((DisplayLanguageOption)value);
                }
            }
        }

        private async void ChangeDisplayLanguage(DisplayLanguageOption option)
        {
            LanguageHelper.ApplyDisplayLanguage();

            var mainWindow = App.StartupWindow;
            var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse();
            string title = loader.GetString("LanguageChangeDialog_Title");
            string content = loader.GetString("LanguageChangeDialog_Content");
            string primaryButton = loader.GetString("LanguageChangeDialog_PrimaryButton");
            string cancelButton = loader.GetString("LanguageChangeDialog_CancelButton");
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButton,
                CloseButtonText = cancelButton,
                DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
                RequestedTheme = (mainWindow.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default,
                XamlRoot = mainWindow.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
            }
        }

        private bool isDebugLogEnabled;
        public bool IsDebugLogEnabled
        {
            get => isDebugLogEnabled;
            set
            {
                if (SetProperty(ref isDebugLogEnabled, value) && isInitialized)
                {
                    SettingsHelper.SaveSetting("DebugLogEnabled", value);
                    ServiceLocator.LogLevelSwitch.MinimumLevel = value
                        ? Serilog.Events.LogEventLevel.Debug
                        : Serilog.Events.LogEventLevel.Information;
                }
            }
        }

        public SettingsViewModel()
        {
            LoadSettings();
            isInitialized = true;
        }

        private void LoadSettings()
        {
            AppThemeIndex = SettingsHelper.GetSetting("AppTheme", 2);
            // load source and resolution settings
            SourceIndex = SettingsHelper.GetSetting("Source", 0);
            ResolutionIndex = SettingsHelper.GetSetting("Resolution", 0);
            UpdateResolutionOptions();
            // load wallpaper locale settings
            WallpaperLocaleIndex = SettingsHelper.GetSetting("WallpaperLocale", (int)WallpaperLocale.zh_CN); // default to zh-CN
            // load auto update settings
            IsAutoUpdateEnabled = SettingsHelper.GetSetting("AutoUpdate", false);
            UpdateModeIndex = SettingsHelper.GetSetting("UpdateMode", 0);
            UpdateTime = SettingsHelper.GetSetting("UpdateTime", TimeSpan.FromHours(12));
            // load auto save settings
            IsAutoSaveEnabled = SettingsHelper.GetSetting("AutoSave", false);
            AutoSaveDirectory = SettingsHelper.GetSetting("AutoSaveDirectory", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "SpotlightGallery"));
            // load display language settings
            DisplayLanguageIndex = SettingsHelper.GetSetting("DisplayLanguage", 0);
            // load debug log settings
            IsDebugLogEnabled = SettingsHelper.GetSetting("DebugLogEnabled", false);
        }
    }
}
