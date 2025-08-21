using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using SpotlightGallery.BackgroundTasks;

namespace SpotlightGallery
{
    public class Program
    {
        static private uint _RegistrationToken;
        static private ManualResetEvent _exitEvent = new ManualResetEvent(false);

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Contains("--update-wallpaper"))
            {
                // 注册COM服务器用于后台任务
                Guid taskGuid = typeof(BackgroundTasks.WallpaperUpdateTask).GUID;
                ComServer.CoRegisterClassObject(ref taskGuid,
                                                new ComServer.BackgroundTaskFactory(),
                                                ComServer.CLSCTX_LOCAL_SERVER,
                                                ComServer.REGCLS_MULTIPLEUSE,
                                                out _RegistrationToken);
                _exitEvent.WaitOne(); // 等待退出信号
            }
            else
            {
                WinRT.ComWrappersSupport.InitializeComWrappers();
                Application.Start((p) => {
                    var context = new DispatcherQueueSynchronizationContext(
                        DispatcherQueue.GetForCurrentThread());
                    SynchronizationContext.SetSynchronizationContext(context);
                    _ = new App();
                });
            }
        }

        public static void SignalExit()
        {
            _exitEvent.Set();
        }
    }
}