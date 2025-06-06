using System;
using System.Runtime.InteropServices;

namespace SpotlightGallery.Helpers
{
    /// <summary>
    /// 窗口过程钩子，用于拦截和处理窗口消息
    /// </summary>
    public sealed class WindowProcedureHook
    {
        private readonly IntPtr _prevProc;
        private readonly WNDPROC _wndProc;
        private readonly Func<IntPtr, int, IntPtr, IntPtr, IntPtr?> _callback;

        public WindowProcedureHook(Microsoft.UI.Xaml.Window window, Func<IntPtr, int, IntPtr, IntPtr, IntPtr?> callback)
        {
            ArgumentNullException.ThrowIfNull(window);
            ArgumentNullException.ThrowIfNull(callback);

            _wndProc = WndProc;
            var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
            _callback = callback;

            const int GWLP_WNDPROC = -4;
            _prevProc = GetWindowLong(handle, GWLP_WNDPROC);
            SetWindowLong(handle, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProc));
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam) => 
            _callback(hwnd, msg, wParam, lParam) ?? CallWindowProc(_prevProc, hwnd, msg, wParam, lParam);
        
        private delegate IntPtr WNDPROC(IntPtr handle, int msg, IntPtr wParam, IntPtr lParam);

        private static IntPtr GetWindowLong(IntPtr handle, int index) =>
            IntPtr.Size == 8 ? GetWindowLongPtrW(handle, index) : (IntPtr)GetWindowLongW(handle, index);

        private static IntPtr SetWindowLong(IntPtr handle, int index, IntPtr newLong) =>
            IntPtr.Size == 8 ? SetWindowLongPtrW(handle, index, newLong) : (IntPtr)SetWindowLongW(handle, index, newLong.ToInt32());

        [DllImport("user32")]
        private static extern IntPtr CallWindowProc(IntPtr prevWndProc, IntPtr handle, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32")]
        private static extern IntPtr GetWindowLongPtrW(IntPtr hWnd, int nIndex);

        [DllImport("user32")]
        private static extern int GetWindowLongW(IntPtr hWnd, int nIndex);

        [DllImport("user32")]
        private static extern int SetWindowLongW(IntPtr hWnd, int nIndexn, int dwNewLong);

        [DllImport("user32")]
        private static extern IntPtr SetWindowLongPtrW(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    }
}