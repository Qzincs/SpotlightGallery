using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using SpotlightGallery.Helpers;
using Microsoft.UI.Xaml;

namespace SpotlightGallery.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
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



        public SettingsViewModel()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            AppThemeIndex = SettingsHelper.GetSetting("AppTheme", 2);
        }
    }
}
