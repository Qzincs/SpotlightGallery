using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using SpotlightGallery.Helpers;
using SpotlightGallery.Pages;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT.Interop;

namespace SpotlightGallery
{
    public partial class App : Application
    {
        private static Window startupWindow;
        private static WindowProcedureHook windowHook;

        // 图片以外的UI区域的宽高
        private const int VerticalUIHeight = 253;
        private const int HorizontalUIWidth = 40;

        // 窗口的初始宽高
        private const int DefaultWindowWidth = 1030;
        private const int DefaultWindowHeight = 800;
        // 窗口的最小宽高
        private const int MinWindowWidth = 834;
        private const int MinWindowHeight = 700;

        public static Window StartupWindow
        {
            get => startupWindow;
        }

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            startupWindow = new Window { SystemBackdrop = new MicaBackdrop() };
            startupWindow.ExtendsContentIntoTitleBar = true;
            startupWindow.Content = new NavigationRootPage();

            // 设置窗口初始大小
            IntPtr hWnd = WindowNative.GetWindowHandle(startupWindow);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            // 调整窗口大小
            appWindow.Resize(new SizeInt32(DefaultWindowWidth, DefaultWindowHeight));

            // 使窗口居中显示
            CenterWindow(appWindow);

            // 创建窗口过程钩子，处理窗口大小调整
            windowHook = new WindowProcedureHook(startupWindow, WndProc);

            startupWindow.Activate();

            int themeIndex = SettingsHelper.GetSetting("AppTheme", 2);
            ThemeManager.ApplyTheme(themeIndex);

            // 监听系统主题变化，自动更新标题栏按钮颜色
            ThemeManager.RegisterSystemThemeListener(startupWindow);
        }

        /// <summary>
        /// 窗口过程处理函数
        /// </summary>
        private IntPtr? WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            const int WM_SIZING = 0x0214;
            if (msg == WM_SIZING)
            {
                // 获取调整大小的矩形
                var rc = Marshal.PtrToStructure<RECT>(lParam);

                // 首先确保窗口不小于最小尺寸
                int currentWidth = rc.right - rc.left;
                int currentHeight = rc.bottom - rc.top;

                bool needsAdjustment = false;

                // 检查宽度是否小于最小宽度
                if (currentWidth < MinWindowWidth)
                {
                    rc.right = rc.left + MinWindowWidth;
                    needsAdjustment = true;
                }

                // 检查高度是否小于最小高度
                if (currentHeight < MinWindowHeight)
                {
                    rc.bottom = rc.top + MinWindowHeight;
                    needsAdjustment = true;
                }

                // 如果进行了最小尺寸调整，则跳过后续的比例调整
                if (!needsAdjustment)
                {
                    // 计算内容区域高度（窗口高度减去底部UI区域）
                    int contentHeight = rc.bottom - rc.top - VerticalUIHeight;
                    
                    // 根据拖动的边缘调整窗口大小
                    const int WMSZ_LEFT = 1;
                    const int WMSZ_RIGHT = 2;
                    const int WMSZ_TOP = 3;
                    const int WMSZ_TOPLEFT = 4;
                    const int WMSZ_TOPRIGHT = 5;
                    const int WMSZ_BOTTOM = 6;
                    const int WMSZ_BOTTOMLEFT = 7;
                    const int WMSZ_BOTTOMRIGHT = 8;

                    // 如果拖动左边缘或右边缘，调整高度
                    if (wParam.ToInt32() == WMSZ_LEFT || wParam.ToInt32() == WMSZ_RIGHT)
                    {
                        // 基于当前宽度，计算应有的高度
                        int width = rc.right - rc.left;
                        int idealHeight = (width - HorizontalUIWidth) * 9 / 16 + VerticalUIHeight;
                        
                        // 确保高度不小于最小高度
                        idealHeight = Math.Max(idealHeight, MinWindowHeight);
                        
                        rc.bottom = rc.top + idealHeight;
                    }
                    // 如果拖动顶部或底部，调整宽度
                    else if (wParam.ToInt32() == WMSZ_TOP || wParam.ToInt32() == WMSZ_BOTTOM)
                    {
                        // 基于当前高度，计算应有的宽度
                        int height = rc.bottom - rc.top;
                        int idealWidth = (height - VerticalUIHeight) * 16 / 9 + HorizontalUIWidth;
                        
                        // 确保宽度不小于最小宽度
                        idealWidth = Math.Max(idealWidth, MinWindowWidth);
                        
                        rc.right = rc.left + idealWidth;
                    }
                    // 拖动角落的情况
                    else
                    {
                        int width = rc.right - rc.left;
                        int idealHeight = (width - HorizontalUIWidth) * 9 / 16 + VerticalUIHeight;
                        
                        // 确保高度不小于最小高度
                        idealHeight = Math.Max(idealHeight, MinWindowHeight);

                        if (wParam.ToInt32() == WMSZ_TOPLEFT || wParam.ToInt32() == WMSZ_TOPRIGHT)
                        {
                            rc.top = rc.bottom - idealHeight;
                        }
                        else
                        {
                            rc.bottom = rc.top + idealHeight;
                        }
                    }
                }

                // 再次检查是否符合最小尺寸要求
                if (rc.right - rc.left < MinWindowWidth)
                {
                    rc.right = rc.left + MinWindowWidth;
                }
                if (rc.bottom - rc.top < MinWindowHeight)
                {
                    rc.bottom = rc.top + MinWindowHeight;
                }

                // 将修改后的矩形写回
                Marshal.StructureToPtr(rc, lParam, false);
                return new IntPtr(1);
            }

            // 设置最小窗口大小
            const int WM_GETMINMAXINFO = 0x0024;
            if (msg == WM_GETMINMAXINFO)
            {
                var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                
                minMaxInfo.ptMinTrackSize.x = MinWindowWidth;
                minMaxInfo.ptMinTrackSize.y = MinWindowHeight; 

                Marshal.StructureToPtr(minMaxInfo, lParam, false);
                return new IntPtr(1);
            }

            return null;
        }

        /// <summary>
        /// 使窗口在屏幕中居中显示
        /// </summary>
        private void CenterWindow(AppWindow appWindow)
        {
            var displayArea = DisplayArea.GetFromWindowId(
                appWindow.Id, DisplayAreaFallback.Primary);
            
            if (displayArea != null)
            {
                int centerX = (displayArea.WorkArea.Width - DefaultWindowWidth) / 2;
                int centerY = (displayArea.WorkArea.Height - DefaultWindowHeight) / 2;
                
                appWindow.Move(new PointInt32(centerX, centerY));
            }
        }

        /// <summary>
        /// 窗口矩形结构
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        /// <summary>
        /// MINMAXINFO 结构，用于 WM_GETMINMAXINFO 消息
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxTrackSize;
            public POINT ptMinTrackSize;
        }

        /// <summary>
        /// POINT 结构
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }
    }
}
