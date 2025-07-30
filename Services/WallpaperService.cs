using Microsoft.Win32;
using SpotlightGallery.Models;
using SpotlightGallery.Models.Spotlight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System.UserProfile;

namespace SpotlightGallery.Services
{
    public enum WallpaperSource
    {
        /// <summary>
        /// Windows聚焦
        /// </summary>
        Spotlight,
        /// <summary>
        /// Bing每日一图，多分辨率
        /// </summary>
        BingDaily
    }

    public enum SpotlightResolution
    {
        Desktop_3840x2160,
        Lockscreen_1920x1080
    }

    public enum BingResolution
    {
        R1920x1080,
    }

    public interface IWallpaperService
    {
        WallpaperSource CurrentSource { get; }
        int CurrentResolutionIndex { get; }

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

        /// <summary>
        /// 更换壁纸来源
        /// </summary>
        /// <param name="source">壁纸来源</param>
        /// <param name="resolutionIndex">分辨率索引</param>
        void ChangeSource(WallpaperSource source, int resolutionIndex);
    }

    class WallpaperService : IWallpaperService
    {
        private readonly string dataDirectory = ApplicationData.Current.LocalFolder.Path;

        private WallpaperSource currentSource = WallpaperSource.Spotlight;
        private SpotlightResolution spotlightResolution = SpotlightResolution.Desktop_3840x2160;
        private BingResolution bingResolution = BingResolution.R1920x1080;

        public WallpaperSource CurrentSource => currentSource;
        public int CurrentResolutionIndex
        {
            get
            {
                return currentSource switch
                {
                    WallpaperSource.Spotlight => (int)spotlightResolution,
                    WallpaperSource.BingDaily => (int)bingResolution,
                    _ => 0
                };
            }
        }

        public void ChangeSource(WallpaperSource source, int resolutionIndex)
        {
            currentSource = source;
            switch (source)
            {
                case WallpaperSource.Spotlight:
                    spotlightResolution = (SpotlightResolution)resolutionIndex;
                    break;
                case WallpaperSource.BingDaily:
                    bingResolution = (BingResolution)resolutionIndex;
                    break;
            }
        }

        /// <summary>
        /// 从特定的源获取壁纸信息的Json字符串
        /// </summary>
        /// <param name="source">壁纸来源</param>
        /// <returns>壁纸信息Json字符串</returns>
        public async Task<string> FetchJsonFromApi(WallpaperSource source)
        {
            string apiUrl = string.Empty;
            switch (currentSource)
            {
                case WallpaperSource.Spotlight:
                    if (spotlightResolution == SpotlightResolution.Desktop_3840x2160)
                        apiUrl = "https://fd.api.iris.microsoft.com/v4/api/selection?&placement=88000820&bcnt=1&country=CN&locale=zh-CN&fmt=json";
                    else
                        apiUrl = "https://arc.msn.com/v3/Delivery/Placement?pid=338387&fmt=json&cdm=1&pl=zh-CN&lc=zh-CN&ctry=CN";
                    break;
                case WallpaperSource.BingDaily:
                    apiUrl = "https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&mkt=zh-CN";
                    break;
            }

            using (var httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 从特定的源获取壁纸信息
        /// </summary>
        /// <param name="source">壁纸来源</param>
        /// <returns>壁纸对象</returns>
        public async Task<Wallpaper> GetWallpaperFromApi(WallpaperSource source)
        {
            string json = await FetchJsonFromApi(source);

            if (source == WallpaperSource.Spotlight)
            {
                if (spotlightResolution == SpotlightResolution.Desktop_3840x2160)
                return ParseSpotlight(json, true);
                else
                    return ParseSpotlight(json, false);
            }
            else
            {
                // 解析Bing每日一图的json数据
                throw new NotImplementedException("Bing每日一图的解析待实现");
            }
        }

        /// <summary>
        /// 解析Spotlight壁纸的json数据，Spotlight有桌面和锁屏两个源，使用isDesktop参数区分
        /// </summary>
        /// <param name="json">壁纸json</param>
        /// <param name="isDesktop">是否为Spotlight桌面源</param>
        /// <returns>壁纸对象</returns>
        private Wallpaper ParseSpotlight(string json, bool isDesktop)
        {
            var spotlightResponse = JsonSerializer.Deserialize<SpotlightResponse>(json);

            if (spotlightResponse?.BatchResponse?.Items != null)
            {
                foreach (var item in spotlightResponse.BatchResponse.Items)
                {
                    if (!string.IsNullOrEmpty(item.ItemJson))
                    {
                        if (isDesktop)
                        {
                            var itemContent = JsonSerializer.Deserialize<SpotlightDesktopItemContent>(item.ItemJson);
                            if (itemContent?.Ad != null)
                            {
                                var ad = itemContent.Ad;
                                string description = ad.GetDescription()?.Split("\r\n")[0] ?? "";
                                return new Wallpaper(
                                    ad.GetTitle(),
                                    description,
                                    ad.GetCopyright(),
                                    ad.GetImageUrl()
                                );
                            }
                        }
                        else
                        {
                            var itemContent = JsonSerializer.Deserialize<SpotlightLockscreenItemContent>(item.ItemJson);
                            if (itemContent?.Ad != null)
                            {
                                var ad = itemContent.Ad;
                                string description = ad.GetDescription()?.Split("\r\n")[0] ?? "";
                                return new Wallpaper(
                                    ad.GetTitle(),
                                    description,
                                    ad.GetCopyright(),
                                    ad.GetImageUrl()
                                );
                            }
                        }
                    }
                }
            }
            return new Wallpaper("", "", "", "");
        }

        /// <summary>
        /// 下载一张壁纸保存到本地，返回壁纸对象
        /// </summary>
        public async Task<Wallpaper> DownloadWallpaperAsync()
        {
            Wallpaper wallpaper = await GetWallpaperFromApi(currentSource);

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

        /// <summary>
        /// 获取当前系统壁纸。如果本地存有壁纸元数据，则壁纸对象中包含相关信息；如果没有，则只包含壁纸路径。
        /// </summary>
        /// <returns>当前系统设置的壁纸对象</returns>
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

        /// <summary>
        /// 设置系统壁纸
        /// </summary>
        /// <param name="wallpaperPath">壁纸文件路径</param>
        /// <returns>是否设置成功</returns>
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

        /// <summary>
        /// 保存壁纸元数据到本地文件
        /// </summary>
        /// <param name="wallpaper">要保存的壁纸对象</param>
        private void SaveWallpaperMetadata(Wallpaper wallpaper)
        {
            string metadataPath = Path.Combine(dataDirectory, "metadata.json");

            if (!File.Exists(metadataPath))
            {
                File.WriteAllText(metadataPath, "[]");
            }

            string json = File.ReadAllText(metadataPath);
            var options = new JsonSerializerOptions { WriteIndented = true };
            List<Wallpaper> metadata = JsonSerializer.Deserialize<List<Wallpaper>>(json, options) ?? new List<Wallpaper>();
            metadata.Add(wallpaper);
            string newJson = JsonSerializer.Serialize(metadata, options);
            File.WriteAllText(metadataPath, newJson);
        }

        /// <summary>
        /// 从本地文件中检索壁纸元数据。如果找不到元数据，则返回只包含路径的壁纸对象。
        /// </summary>
        /// <param name="wallpaperPath">壁纸文件路径</param>
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
            var wallpapers = JsonSerializer.Deserialize<List<Wallpaper>>(json) ?? new List<Wallpaper>();

            return wallpapers.Find(w => w.title == title) ?? new Wallpaper("", "", "", "")
            {
                path = wallpaperPath
            };
        }
    }
}
