using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace SpotlightGallery.Helpers
{
    public class ThemeManager
    {
        private static UISettings uiSettings = new UISettings();

        public static void ApplyTheme(int themeIndex)
        {
            Window window = App.StartupWindow;
            if (window?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = themeIndex switch
                {
                    0 => ElementTheme.Light,
                    1 => ElementTheme.Dark,
                    _ => ElementTheme.Default // System Default
                };
            }
            ApplySystemThemeToCaptionButtons(window);
        }

        // 标题栏的按钮不会自动跟随系统主题变化，因此需要手动设置颜色
        public static Color ApplySystemThemeToCaptionButtons(Window window)
        {
            // 判断当前主题
            ElementTheme theme = ElementTheme.Default;
            if (window?.Content is FrameworkElement rootElement)
            {
                theme = rootElement.RequestedTheme;
            }

            // 默认颜色
            Color color = Colors.Black;

            // 根据主题选择颜色
            switch (theme)
            {
                case ElementTheme.Dark:
                    color = Colors.White;
                    break;
                case ElementTheme.Light:
                    color = Colors.Black;
                    break;
                case ElementTheme.Default:
                default:
                    // 跟随系统主题
                    var bgColor = uiSettings.GetColorValue(UIColorType.Background);
                    // 简单判断：背景较暗用白色，否则用黑色
                    color = (bgColor.R + bgColor.G + bgColor.B) / 3 < 128 ? Colors.White : Colors.Black;
                    break;
            }

            SetCaptionButtonColors(window, color);
            return color;
        }

        public static void SetCaptionButtonColors(Window window, Windows.UI.Color color)
        {
            var res = Application.Current.Resources;
            res["WindowCaptionForeground"] = color;
            window.AppWindow.TitleBar.ButtonForegroundColor = color;
        }

        // 当系统主题变化时，自动更新标题栏按钮颜色
        public static void RegisterSystemThemeListener(Window window)
        {
            if (uiSettings != null)
            {
                uiSettings.ColorValuesChanged += (s, e) =>
                {
                    // 必须在UI线程调用
                    window.DispatcherQueue.TryEnqueue(() =>
                    {
                        if (window?.Content is FrameworkElement rootElement &&
                            rootElement.RequestedTheme == ElementTheme.Default)
                        {
                            ApplySystemThemeToCaptionButtons(window);
                        }
                    });
                };
            }
        }
    }
}
