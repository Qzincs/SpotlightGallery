using System;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace SpotlightGallery.BackgroundTasks
{
    // Background task implementation.
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("74b929df-d324-4dc0-903d-0604c0ba37c0")]
    [ComSourceInterfaces(typeof(IBackgroundTask))]
    public class WallpaperUpdateTask : IBackgroundTask, IDisposable
    {
        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Stop the server when the background task is disposed.
                    SpotlightGallery.Program.SignalExit();
                }
                disposed = true;
            }
        }

        ~WallpaperUpdateTask()
        {
            Dispose(false);
        }

        /// <summary>
        /// This method is the main entry point for the background task. The system will believe this background task
        /// is complete when this method returns.
        /// </summary>
        [MTAThread]
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Wire the cancellation handler.
            taskInstance.Canceled += this.OnCanceled;

            // Set the progress to indicate this task has started
            taskInstance.Progress = 10;

            // Create the toast notification content
            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);
            var toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode("C# Background task executed"));

            // Create the toast notification
            var toast = new ToastNotification(toastXml);

            // Show the toast notification
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        /// <summary>
        /// This method is signaled when the system requests the background task be canceled. This method will signal
        /// to the Run method to clean up and return.
        /// </summary>
        [MTAThread]
        public void OnCanceled(IBackgroundTaskInstance taskInstance, BackgroundTaskCancellationReason cancellationReason)
        {
            // Unregister the task when the task is destroyed.
            SpotlightGallery.Program.SignalExit();
        }
    }
}
