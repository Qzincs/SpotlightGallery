using CommunityToolkit.Mvvm.Input;
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
                }

                if (!string.IsNullOrEmpty(value?.path))
                {
                    LoadImageFromPath(value.path);
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

        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }

        public ICommand NextImageCommand { get; }

        public HomePageViewModel(IWallpaperService wallpaperService)
        {
            this.wallpaperService = wallpaperService ?? throw new ArgumentNullException(nameof(wallpaperService));
            NextImageCommand = new RelayCommand(async () => await LoadNextWallpaperAsync(), () => !isLoading);
        }

        public async Task LoadNextWallpaperAsync()
        {
            if(IsLoading) return;

            try
            {
                IsLoading = true;

                var wallpaper = await wallpaperService.DownloadWallpaperAsync();

                if(wallpaper != null && !string.IsNullOrEmpty(wallpaper.path))
                {
                    Wallpaper = wallpaper;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("下载壁纸失败或路径无效");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载壁纸失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
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
