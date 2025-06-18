using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using SpotlightGallery.Services;
using SpotlightGallery.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SpotlightGallery.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        public HomePageViewModel ViewModel { get; }

        public HomePage()
        {
            try
            {
                InitializeComponent();

                var wallpaperService = new WallpaperService();
                ViewModel = new HomePageViewModel(wallpaperService);

                if (ViewModel != null && this != null)
                {
                    this.DataContext = ViewModel;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"设置 DataContext 失败 - ViewModel: {ViewModel != null}, this: {this != null}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HomePage 初始化异常: {ex}");
            }
        }
    }
}
