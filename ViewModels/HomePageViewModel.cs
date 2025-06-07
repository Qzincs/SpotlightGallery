using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using SpotlightGallery.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpotlightGallery.ViewModels
{
    public class HomePageViewModel : INotifyPropertyChanged
    {
        private readonly IWallpaperService wallpaperService;
        private Wallpaper wallpaper;

        private string wallpaperTitle;
        private string wallpaperDescription;
        private string wallpaperCopyright;
        private BitmapImage wallpaperImage;

        private string infoBarMessage;
        private InfoBarSeverity infoBarSeverity;
        private bool isInfoBarVisible = false;
        private DispatcherTimer infoBarTimer;


        private bool isLoading = false;


        public Wallpaper Wallpaper
        {
            get => wallpaper;
            set
            {
                if (SetProperty(ref wallpaper, value))
                {
                    WallpaperTitle = wallpaper.title;
                    WallpaperDescription = wallpaper.description;
                    WallpaperCopyright = wallpaper.copyright;
                    WallpaperImage = new BitmapImage(new Uri(wallpaper.url));

                    if (!string.IsNullOrEmpty(value?.path))
                    {
                        LoadImageFromPath(value.path);
                    }
                }
            }
        }

        public string WallpaperTitle
        {
            get => wallpaperTitle;
            set => SetProperty(ref wallpaperTitle, value);
        }

        public string WallpaperDescription
        {
            get => wallpaperDescription;
            set => SetProperty(ref wallpaperDescription, value);
        }

        public string WallpaperCopyright
        {
            get => wallpaperCopyright;
            set => SetProperty(ref wallpaperCopyright, value);
        }

        public BitmapImage WallpaperImage
        {
            get => wallpaperImage;
            set => SetProperty(ref wallpaperImage, value);
        }

        public string InfoBarMessage
        {
            get => infoBarMessage;
            set => SetProperty(ref infoBarMessage, value);
        }

        public InfoBarSeverity InfoBarSeverity
        {
            get => infoBarSeverity;
            set => SetProperty(ref infoBarSeverity, value);
        }

        public bool IsInfoBarVisible
        {
            get => isInfoBarVisible;
            set => SetProperty(ref isInfoBarVisible, value);
        }

        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }

        public ICommand NextWallpaperCommand { get; }
        public ICommand ApplyWallpaperCommand { get; }

        public HomePageViewModel(IWallpaperService wallpaperService)
        {
            this.wallpaperService = wallpaperService ?? throw new ArgumentNullException(nameof(wallpaperService));
            NextWallpaperCommand = new RelayCommand(async () => await LoadNextWallpaperAsync(), () => !IsLoading);
            ApplyWallpaperCommand = new RelayCommand(ApplyWallpaper, () => !IsLoading && wallpaper != null && !string.IsNullOrEmpty(wallpaper.path));
        }

        /// <summary>
        /// 加载下一张壁纸
        /// </summary>
        /// <returns></returns>
        public async Task LoadNextWallpaperAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                (NextWallpaperCommand as RelayCommand)?.NotifyCanExecuteChanged();
                (ApplyWallpaperCommand as RelayCommand)?.NotifyCanExecuteChanged();

                HideInfoBar();

                var wallpaper = await wallpaperService.DownloadWallpaperAsync();

                if (wallpaper != null && !string.IsNullOrEmpty(wallpaper.path))
                {
                    Wallpaper = wallpaper;
                }
                else
                {
                    ShowInfoBar("获取壁纸失败", InfoBarSeverity.Error);
                    System.Diagnostics.Debug.WriteLine("下载壁纸失败或路径无效");
                }
            }
            catch (Exception ex)
            {
                ShowInfoBar($"加载壁纸失败: {ex.Message}", InfoBarSeverity.Error);
                System.Diagnostics.Debug.WriteLine($"加载壁纸失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;

                // 刷新命令状态
                (NextWallpaperCommand as RelayCommand)?.NotifyCanExecuteChanged();
                (ApplyWallpaperCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// 设置壁纸
        /// </summary>
        /// <param name="wallpaper"></param>
        public void ApplyWallpaper()
        {
            if (wallpaperService.SetWallpaper(wallpaper.path))
            {
                ShowInfoBar("壁纸设置成功", InfoBarSeverity.Success);
                System.Diagnostics.Debug.WriteLine("壁纸设置成功");
            }
            else
            {
                ShowInfoBar("壁纸设置失败", InfoBarSeverity.Error);
                System.Diagnostics.Debug.WriteLine("壁纸设置失败");
            }
        }

        private void LoadImageFromPath(string path)
        {
            try
            {
                var bitmap = new BitmapImage(new Uri(path));
                WallpaperImage = bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载图片失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示消息通知
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="severity">严重程度</param>
        /// <param name="autoHideSeconds">自动隐藏的秒数，小于等于0则不自动隐藏</param>
        public void ShowInfoBar(string message, InfoBarSeverity severity = InfoBarSeverity.Informational, double autoHideSeconds = 3.0)
        {
            InfoBarMessage = message;
            InfoBarSeverity = severity;
            IsInfoBarVisible = true;

            // 自动隐藏逻辑
            if (InfoBarSeverity != InfoBarSeverity.Error && autoHideSeconds > 0)
            {
                // 根据消息长度调整显示时间
                // 每10个字符增加0.5秒，最长显示10秒
                double adjustedTime = Math.Min(autoHideSeconds + (message.Length / 10) * 0.5, 10.0);

                if (infoBarTimer == null)
                {
                    infoBarTimer = new DispatcherTimer();
                    infoBarTimer.Tick += (s, e) =>
                    {
                        HideInfoBar();
                        infoBarTimer.Stop();
                    };
                }

                infoBarTimer.Interval = TimeSpan.FromSeconds(adjustedTime);
                infoBarTimer.Start();
            }
        }

        /// <summary>
        /// 隐藏消息通知
        /// </summary>
        public void HideInfoBar()
        {
            IsInfoBarVisible = false;
            if (infoBarTimer != null && infoBarTimer.IsEnabled)
            {
                infoBarTimer.Stop();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
