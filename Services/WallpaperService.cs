using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System.UserProfile;

namespace SpotlightGallery.Services
{
    public class Wallpaper
    {
        public string title;
        public string description;
        public string copyright;
        public string url;
        public string? path;

        public Wallpaper(string title, string description, string copyright, string url)
        {
            this.title = title;
            this.description = description;
            this.copyright = copyright;
            this.url = url;
        }
    }

    public interface IWallpaperService
    {
        /// <summary>
        /// 下载一张壁纸
        /// </summary>
        /// <returns>壁纸的路径</returns>
        Task<Wallpaper> DownloadWallpaperAsync();

        /// <summary>
        /// 设置系统壁纸
        /// </summary>
        /// <param name="wallpaperPath">壁纸文件路径</param>
        /// <returns>是否设置成功</returns>
        Task<bool> SetWallpaperAsync(string wallpaperPath);

        /// <summary>
        /// 获取当前系统壁纸
        /// </summary>
        /// <returns>当前壁纸</returns>
        Wallpaper GetCurrentWallpaper();
    }

    class WallpaperService : IWallpaperService
    {
        private readonly string dataDirectory = ApplicationData.Current.LocalFolder.Path;

        private List<Wallpaper> wallpapers = new List<Wallpaper>();

        /// <summary>
        /// 从API获取壁纸信息
        /// </summary>
        /// <returns></returns>
        private async Task GetSomeWallpapers()
        {
            using (var httpClient = new HttpClient())
            {
                // 单次获取壁纸数量
                int count = 4;
                // TODO 语言地区
                string locale = "";

                string apiUrl = $"https://fd.api.iris.microsoft.com/v4/api/selection?&placement=88000820&bcnt={count}&country=CN&locale=zh-CN&fmt=json";

                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();
                    // 壁纸信息
                    dynamic responseData = JsonConvert.DeserializeObject(jsonString);
                    dynamic items = responseData.batchrsp.items;
                    foreach (var item in items)
                    {
                        string imageJsonString = item.item;
                        imageJsonString = imageJsonString.Replace("\\\"", "\"");
                        dynamic imageJson = JsonConvert.DeserializeObject(imageJsonString);
                        imageJson = imageJson.ad;

                        string description = ((string)imageJson.iconHoverText).Split("\r\n")[0];
                        Wallpaper wallpaper = new Wallpaper(
                            (string)imageJson.title,
                            (string)description,
                            (string)imageJson.copyright,
                            (string)imageJson.landscapeImage.asset
                        );
                        wallpapers.Add(wallpaper);
                    }
                }
            }
        }

        public async Task<Wallpaper> DownloadWallpaperAsync()
        {
            Wallpaper wallpaper;

            if (wallpapers.Count == 0)
            {
                await GetSomeWallpapers();
            }

            if (wallpapers.Count == 0)
            {
                throw new Exception("没有可用的壁纸");
            }

            wallpaper = wallpapers[0];
            wallpapers.RemoveAt(0);

            string wallpaperPath = Path.Combine(dataDirectory, $"{wallpaper.title}.jpg");

            if (File.Exists(wallpaperPath))
            {
                return RetrieveWallpaperMetadata(wallpaperPath);
            }

            using (var httpClient = new HttpClient())
            {
                // 下载壁纸图片
                HttpResponseMessage imageResponse = await httpClient.GetAsync(wallpaper.url);
                byte[] imageData = await imageResponse.Content.ReadAsByteArrayAsync();

                if (!File.Exists(dataDirectory))
                {
                    Directory.CreateDirectory(dataDirectory);
                }

                // 将壁纸保存到本地
                File.WriteAllBytes(wallpaperPath, imageData);
            }

            wallpaper.path = wallpaperPath;

            SaveWallpaperMetadata(wallpaper);

            return wallpaper;
        }

        // 获取当前壁纸的路径
        public Wallpaper GetCurrentWallpaper()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", false);

                if (key == null)
                {
                    System.Diagnostics.Debug.WriteLine("无法打开桌面设置注册表项");
                    return new Wallpaper("", "", "", "");
                }

                string wallpaperPath = key.GetValue("wallpaper") as string;

                if (string.IsNullOrEmpty(wallpaperPath) || !File.Exists(wallpaperPath))
                {
                    System.Diagnostics.Debug.WriteLine("当前壁纸路径无效或不存在");
                    return new Wallpaper("", "", "", "");
                }

                return RetrieveWallpaperMetadata(wallpaperPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取当前壁纸时出错: {ex.Message}");
                return new Wallpaper("", "", "", "");
            }
        }

        public async Task<bool> SetWallpaperAsync(string wallpaperPath)
        {
            if (!File.Exists(wallpaperPath))
            {
                System.Diagnostics.Debug.WriteLine($"壁纸文件不存在: {wallpaperPath}");
                return false;
            }

            if (!UserProfilePersonalizationSettings.IsSupported())
            {
                System.Diagnostics.Debug.WriteLine("当前系统不支持设置壁纸。");
                return false;
            }


            var file = await StorageFile.GetFileFromPathAsync(wallpaperPath);
            UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
            bool result = await profileSettings.TrySetWallpaperImageAsync(file);
            return result;
        }

        private void SaveWallpaperMetadata(Wallpaper wallpaper)
        {
            string metadataPath = Path.Combine(dataDirectory, "metadata.json");

            if (!File.Exists(metadataPath))
            {
                File.WriteAllText(metadataPath, "[]");
            }

            string json = File.ReadAllText(metadataPath);
            List<Wallpaper> metadata = JsonConvert.DeserializeObject<List<Wallpaper>>(json) ?? new List<Wallpaper>();
            metadata.Add(wallpaper);
            string newJson = JsonConvert.SerializeObject(metadata, Formatting.Indented);
            File.WriteAllText(metadataPath, newJson);
        }


        /// <summary>
        /// 尝试从本地获取壁纸的元数据
        /// </summary>
        /// <param name="wallpaperPath">壁纸文件路径</param>
        /// <returns>壁纸元数据</returns>
        private Wallpaper RetrieveWallpaperMetadata(string wallpaperPath)
        {
            string metadataPath = Path.Combine(dataDirectory, "metadata.json");

            if (!File.Exists(metadataPath))
            {
                return new Wallpaper("", "", "", "")
                {
                    path = wallpaperPath
                };
            }

            string title = Path.GetFileNameWithoutExtension(wallpaperPath);

            string json = File.ReadAllText(metadataPath);
            var wallpapers = JsonConvert.DeserializeObject<List<Wallpaper>>(json) ?? new List<Wallpaper>();

            return wallpapers.Find(w => w.title == title) ?? new Wallpaper("", "", "", "")
            {
                path = wallpaperPath
            };
        }
    }
}
