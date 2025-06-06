using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SpotlightGallery.Pages
{


    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NavigationRootPage : Page
    {
        public NavigationRootPage()
        {
            InitializeComponent();

            Loaded += delegate (object sender, RoutedEventArgs e)
            {
                NavView.SelectedItem = NavView.MenuItems[0];


                Window window = App.StartupWindow;
                window.ExtendsContentIntoTitleBar = true;
                window.SetTitleBar(this.AppTitleBar);

                AppWindow appWindow = window.AppWindow;
                appWindow.SetIcon("Assets/Tiles/GalleryIcon.ico");
                appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            };
        }

        private void OnNavigationViewSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                if (ContentFrame.CurrentSourcePageType != typeof(SettingsPage))
                {
                    ContentFrame.Navigate(typeof(SettingsPage));
                }
            }
            else
            {
                if (args.SelectedItemContainer != null)
                {
                    var navItemTag = args.SelectedItemContainer.Tag.ToString();

                    switch (navItemTag)
                    {
                        case "home":
                            ContentFrame.Navigate(typeof(HomePage));
                            break;
                        case "history":
                            // ContentFrame.Navigate(typeof(HistoryPage));
                            break;
                        case "favourite":
                            // ContentFrame.Navigate(typeof(FavouritePage));
                            break;
                    }
                }

            }
        }

        private void OnPaneDisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {
            if (sender.PaneDisplayMode == NavigationViewPaneDisplayMode.Top)
            {
                VisualStateManager.GoToState(this, "Top", true);
            }
            else
            {
                if (args.DisplayMode == NavigationViewDisplayMode.Minimal)
                {
                    VisualStateManager.GoToState(this, "Compact", true);
                }
                else
                {
                    VisualStateManager.GoToState(this, "Default", true);
                }
            }
        }
    }
}
