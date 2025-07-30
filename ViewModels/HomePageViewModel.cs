using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using SpotlightGallery.Models;
using SpotlightGallery.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;

namespace SpotlightGallery.ViewModels
{
    public class HomePageViewModel : ViewModelBase
    {
        private readonly IWallpaperService wallpaperService = ServiceLocator.WallpaperService;
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
        public ICommand SaveWallpaperCommand { get; }

        public HomePageViewModel()
        {
            NextWallpaperCommand = new RelayCommand(async () => await LoadNextWallpaperAsync(), () => !IsLoading);
            ApplyWallpaperCommand = new RelayCommand(ApplyWallpaperAsync, () => !IsLoading && wallpaper != null && !string.IsNullOrEmpty(wallpaper.path));
            SaveWallpaperCommand = new RelayCommand(SaveWallpaperAsync, () => !IsLoading && wallpaper != null && !string.IsNullOrEmpty(wallpaper.path));

            Wallpaper = wallpaperService.GetCurrentWallpaper();
        }

        /// <summary>
        /// 加载下一张壁纸
        /// </summary>
        public async Task LoadNextWallpaperAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                UpdateCommandsState();

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
                UpdateCommandsState();
            }
        }

        private void UpdateCommandsState()
        {
            (NextWallpaperCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (ApplyWallpaperCommand as RelayCommand)?.NotifyCanExecuteChanged();
            (SaveWallpaperCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 设置壁纸
        /// </summary>
        public async void ApplyWallpaperAsync()
        {
            bool result = await wallpaperService.SetWallpaperAsync(wallpaper.path);

            if (result)
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

        public async void SaveWallpaperAsync()
        {
            StorageFile sourceFile = await StorageFile.GetFileFromPathAsync(wallpaper.path);


            // 创建文件保存对话框
            var savePicker = new FileSavePicker();

            var window = App.StartupWindow;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            savePicker.FileTypeChoices.Add("JPEG 图片", new List<string>() { ".jpg" });
            savePicker.SuggestedFileName = $"{WallpaperTitle}";

            StorageFile destinationFile = await savePicker.PickSaveFileAsync();

            if (destinationFile != null)
            {
                CachedFileManager.DeferUpdates(destinationFile);

                // 复制文件
                await sourceFile.CopyAndReplaceAsync(destinationFile);

                // 完成更新
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(destinationFile);

                if (status == FileUpdateStatus.Complete)
                {
                    ShowInfoBar("壁纸已成功保存", InfoBarSeverity.Success);
                }
                else
                {
                    ShowInfoBar("保存壁纸时出错", InfoBarSeverity.Error);
                }
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
    }
}
