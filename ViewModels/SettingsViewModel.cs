using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpotlightGallery.Helpers;
using SpotlightGallery.Services;

namespace SpotlightGallery.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly IWallpaperService wallpaperService = ServiceLocator.WallpaperService;

        private int appThemeIndex;
        /// <summary>
        /// 0 - 浅色, 1 - 深色, 2 - 系统默认
        /// </summary>
        public int AppThemeIndex
        {
            get => appThemeIndex;
            set
            {
                if (SetProperty(ref appThemeIndex, value))
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
                if (SetProperty(ref sourceIndex, value))
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
                if (SetProperty(ref resolutionIndex, value))
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
                ResolutionOptions.Add("1920x1080");
                // 可扩展更多分辨率
            }
        }

        public SettingsViewModel()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            AppThemeIndex = SettingsHelper.GetSetting("AppTheme", 2);
            SourceIndex = SettingsHelper.GetSetting("Source", 0);
            ResolutionIndex = SettingsHelper.GetSetting("Resolution", 0);
            UpdateResolutionOptions();
        }
    }
}
