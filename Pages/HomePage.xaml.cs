using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Serilog;
using Serilog.Context;
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
            using (LogContext.PushProperty("Module", nameof(HomePage)))
            {
                try
                {
                    InitializeComponent();

                    ViewModel = new HomePageViewModel();

                    if (ViewModel != null && this != null)
                    {
                        this.DataContext = ViewModel;
                    }
                    else
                    {
                        Log.Error("Failed to set DataContext - ViewModel or HomePage is null.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "HomePage Initialization Exception: {Message}", ex.Message);
                }
            }
        }
    }
}
