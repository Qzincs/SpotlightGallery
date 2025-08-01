using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpotlightGallery.Helpers;
using SpotlightGallery.Services;
using Windows.ApplicationModel.Background;

namespace SpotlightGallery.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly IWallpaperService wallpaperService = ServiceLocator.WallpaperService;

        private bool isInitialized = false;

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
                    wallpaperService.ChangeSource((WallpaperSource)value, 0);
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

        private bool isAutoUpdateEnabled;
        public bool IsAutoUpdateEnabled
        {
            get => isAutoUpdateEnabled;
            set
            {
                if (SetProperty(ref isAutoUpdateEnabled, value) && isInitialized)
                {
                    SettingsHelper.SaveSetting("AutoUpdate", value);
                    SettingsHelper.SaveSetting("NextUpdateTime", DateTimeOffset.Now.AddDays(1));
                    if (value)
                    {
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
                    if (value == 0) // daily
                    {
                        SettingsHelper.SaveSetting("NextUpdateTime", new DateTimeOffset(DateTimeOffset.Now.Date.AddDays(1)));
                    }
                    else // at specific time
                    {
                        SettingsHelper.SaveSetting("NextUpdateTime", new DateTimeOffset(DateTimeOffset.Now.Date.AddDays(1)).Add(UpdateTime));
                    }

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
                    DateTimeOffset nextUpdateTime = DateTimeOffset.Now.Date.Add(updateTime);
                    if (nextUpdateTime <= DateTimeOffset.Now)
                    {
                        nextUpdateTime = nextUpdateTime.AddDays(1);
                    }
                    SettingsHelper.SaveSetting("NextUpdateTime", nextUpdateTime);
                }
            }
        }

        public async Task RegisterWallpaperUpdateTaskAsync()
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
                System.Diagnostics.Debug.WriteLine(isRegistered
                    ? "WallpaperUpdateTask registered successfully."
                    : "Failed to register WallpaperUpdateTask.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RegisterWallpaperUpdateTaskAsync Exception: {ex}");
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
                    break;
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
            wallpaperService.ChangeSource((WallpaperSource)SourceIndex, ResolutionIndex);
            // load auto update settings
            IsAutoUpdateEnabled = SettingsHelper.GetSetting("AutoUpdate", false);
            UpdateModeIndex = SettingsHelper.GetSetting("UpdateMode", 0);
            UpdateTime = SettingsHelper.GetSetting("UpdateTime", TimeSpan.FromHours(12));
        }
    }
}
