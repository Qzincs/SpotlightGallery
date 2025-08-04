using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;

namespace SpotlightGallery.Helpers
{
    /// <summary>
    /// Helper class for showing toast notifications.
    /// </summary>
    public static class ToastHelper
    {
        /// <summary>
        /// Shows a toast notification with the specified title and message.
        /// </summary>
        public static void ShowToast(string title, string message)
        {
            var toastXml = new XmlDocument();
            toastXml.LoadXml("<toast><visual><binding template=\"ToastGeneric\"></binding></visual></toast>");

            var binding = toastXml.SelectSingleNode("/toast/visual/binding");

            var titleElement = toastXml.CreateElement("text");
            titleElement.InnerText = title;
            binding.AppendChild(titleElement);

            var messageElement = toastXml.CreateElement("text");
            messageElement.InnerText = message;
            binding.AppendChild(messageElement);

            var toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        /// <summary>
        /// Shows a toast notification with the specified title, message, and hero image.
        /// </summary>
        public static void ShowToast(string title, string message, string imageUrl)
        {
            var toastXml = new XmlDocument();
            toastXml.LoadXml("<toast><visual><binding template=\"ToastGeneric\"></binding></visual></toast>");

            var binding = toastXml.SelectSingleNode("/toast/visual/binding");

            var titleElement = toastXml.CreateElement("text");
            titleElement.InnerText = title;
            binding.AppendChild(titleElement);

            var messageElement = toastXml.CreateElement("text");
            messageElement.InnerText = message;
            binding.AppendChild(messageElement);

            if (!string.IsNullOrEmpty(imageUrl))
            {
                var imageElement = toastXml.CreateElement("image");
                var placementAttr = toastXml.CreateAttribute("placement");
                placementAttr.Value = "hero";
                imageElement.Attributes.SetNamedItem(placementAttr);
                var srcAttr = toastXml.CreateAttribute("src");
                srcAttr.Value = imageUrl;
                imageElement.Attributes.SetNamedItem(srcAttr);
                binding.AppendChild(imageElement);
            }

            var toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}
