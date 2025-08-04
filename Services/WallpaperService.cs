using Microsoft.Win32;
using Serilog;
using Serilog.Context;
using SpotlightGallery.Models;
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
        R3840x2160,
        R1920x1200,
        R1920x1080,
        R1366x768,
        R1280x768,
        R1024x768,
        R800x600
    }
    public static class ResolutionExtensions
    {
        public static string ToResolutionString(this SpotlightResolution resolution)
        {
            return resolution switch
            {
                SpotlightResolution.Desktop_3840x2160 => "UHD",
                SpotlightResolution.Lockscreen_1920x1080 => "1920x1080",
                _ => resolution.ToString()
            };
        }

        public static string ToResolutionString(this BingResolution resolution)
        {
            return resolution switch
            {
                BingResolution.R3840x2160 => "UHD",
                BingResolution.R1920x1200 => "1920x1200",
                BingResolution.R1920x1080 => "1920x1080",
                BingResolution.R1366x768 => "1366x768",
                BingResolution.R1280x768 => "1280x768",
                BingResolution.R1024x768 => "1024x768",
                BingResolution.R800x600 => "800x600",
                _ => resolution.ToString()
            };
        }
    }

    public interface IWallpaperService
    {
        WallpaperSource CurrentSource { get; }
        int CurrentResolutionIndex { get; }
        string AutoSaveDirectory { get; set; }

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

        /// <summary>
        /// 清理旧壁纸文件
        /// </summary>
        void CleanupOldWallpapers();
    }

    class WallpaperService : IWallpaperService
    {
        private readonly string dataDirectory = ApplicationData.Current.LocalFolder.Path;
        private string autoSaveDirectory = string.Empty;
        public string AutoSaveDirectory
        {
            get => autoSaveDirectory;
            set => autoSaveDirectory = value;
        }

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
            using (LogContext.PushProperty("Module", nameof(WallpaperService)))
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
                Log.Debug("Changed wallpaper source to {Source} with resolution {Resolution}",
                    currentSource.ToString(),
                    currentSource == WallpaperSource.Spotlight ? spotlightResolution.ToResolutionString() : bingResolution.ToResolutionString());
            }
        }

        /// <summary>
        /// 从特定的源获取壁纸信息的Json字符串
        /// </summary>
        /// <param name="source">壁纸来源</param>
        /// <returns>壁纸信息Json字符串</returns>
        public async Task<string> FetchJsonFromApi(WallpaperSource source)
        {
            using (LogContext.PushProperty("Module", nameof(WallpaperService)))
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
                        apiUrl = "https://services.bingapis.com/ge-apps/api/v2/bwc/hpimages?mkt=zh-cn&theme=bing&defaultBrowser=ME&dhpSetToBing=True&dseSetToBing=True";
                        break;
                }

                using (var httpClient = new HttpClient())
                {
                    HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                    Log.Error("Failed to fetch wallpaper JSON from {ApiUrl}. Status code: {StatusCode}", apiUrl, response.StatusCode);
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// 从特定的源获取壁纸信息
        /// </summary>
        /// <param name="source">壁纸来源</param>
        /// <returns>壁纸对象</returns>
        public async Task<Wallpaper> GetWallpaperFromApi(WallpaperSource source)
        {
            using (LogContext.PushProperty("Module", nameof(WallpaperService)))
            {
                string json = await FetchJsonFromApi(source);
                if (!string.IsNullOrEmpty(json))
                {
                    Log.Debug("Fetched wallpaper JSON from {Source}: {Json}", source.ToString(), json);
                }
                else
                {
                    Log.Error("Wallpaper JSON is empty or null for source {Source}", source.ToString());
                    return new Wallpaper("", "", "", "");
                }

                if (source == WallpaperSource.Spotlight)
                {
                    if (spotlightResolution == SpotlightResolution.Desktop_3840x2160)
                        return ParseSpotlight(json, true);
                    else
                        return ParseSpotlight(json, false);
                }
                else
                {
                    return ParseBingDaily(json);
                }
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
            using (LogContext.PushProperty("Module", nameof(WallpaperService)))
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
                    Log.Error("No valid item found in Spotlight JSON response.");
                }
                Log.Error("No valid items found in Spotlight JSON response.");

                return new Wallpaper("", "", "", "");
            }
        }

        private Wallpaper ParseBingDaily(string json)
        {
            using (LogContext.PushProperty("Module", nameof(WallpaperService)))
            {
                var bingResponse = JsonSerializer.Deserialize<BingDailyResponse>(json);

                if (bingResponse?.Images.Count > 0 && bingResponse?.Images != null)
                {
                    var image = bingResponse.Images[0];
                    return new Wallpaper(
                        image.Headline,
                        image.Title,
                        image.Copyright,
                        image.Url.Replace("UHD", bingResolution.ToResolutionString())
                    );
                }
                Log.Error("No valid images found in Bing Daily JSON response.");
                return new Wallpaper("", "", "", "");
            }
        }

        /// <summary>
        /// 下载一张壁纸保存到本地，返回壁纸对象
        /// </summary>
        public async Task<Wallpaper> DownloadWallpaperAsync()
        {
            using (LogContext.PushProperty("Module", nameof(WallpaperService)))
            {
                Wallpaper wallpaper = await GetWallpaperFromApi(currentSource);

                string resolutionSuffix = currentSource switch
                {
                    WallpaperSource.Spotlight => spotlightResolution.ToResolutionString(),
                    WallpaperSource.BingDaily => bingResolution.ToResolutionString(),
                    _ => ""
                };

                string wallpaperPath = Path.Combine(dataDirectory, $"{wallpaper.title}_{resolutionSuffix}.jpg");

                if (File.Exists(wallpaperPath))
                {
                    Log.Debug("Wallpaper already exists at {WallpaperPath}, retrieving metadata.", wallpaperPath);
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
                    Log.Information("Wallpaper saved to {WallpaperPath}", wallpaperPath);
                }

                wallpaper.path = wallpaperPath;

                SaveWallpaperMetadata(wallpaper);

                return wallpaper;
            }
        }

        /// <summary>
        /// 获取当前系统壁纸。如果本地存有壁纸元数据，则壁纸对象中包含相关信息；如果没有，则只包含壁纸路径。
        /// </summary>
        /// <returns>当前系统设置的壁纸对象</returns>
        public Wallpaper GetCurrentWallpaper()
        {
            using (LogContext.PushProperty("Module", nameof(WallpaperService)))
            {
                try
                {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", false);

                    if (key == null)
                    {
                        Log.Error("Unable to open desktop settings registry key: {RegistryKeyPath}", @"HKEY_CURRENT_USER\Control Panel\Desktop");
                        return new Wallpaper("", "", "", "");
                    }

                    string wallpaperPath = key.GetValue("wallpaper") as string;

                    if (string.IsNullOrEmpty(wallpaperPath) || !File.Exists(wallpaperPath))
                    {
                        Log.Error("Current wallpaper path is invalid or does not exist: {WallpaperPath}", wallpaperPath);
                        return new Wallpaper("", "", "", "");
                    }

                    return RetrieveWallpaperMetadata(wallpaperPath);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error retrieving current wallpaper: {Message}", ex.Message);
                    return new Wallpaper("", "", "", "");
                }
            }
        }

        /// <summary>
        /// 设置系统壁纸
        /// </summary>
        /// <param name="wallpaperPath">壁纸文件路径</param>
        /// <returns>是否设置成功</returns>
        public async Task<bool> SetWallpaperAsync(string wallpaperPath)
        {
            using (LogContext.PushProperty("Module", nameof(WallpaperService)))
            {
                if (!File.Exists(wallpaperPath))
                {
                    Log.Error("Wallpaper file does not exist: {WallpaperPath}", wallpaperPath);
                    return false;
                }

                if (!UserProfilePersonalizationSettings.IsSupported())
                {
                    Log.Error("Current system does not support setting wallpaper.");
                    return false;
                }

                var file = await StorageFile.GetFileFromPathAsync(wallpaperPath);
                UserProfilePersonalizationSettings profileSettings = UserProfilePersonalizationSettings.Current;
                bool result = await profileSettings.TrySetWallpaperImageAsync(file);

                if (result)
                {
                    SaveWallpaperToAutoSaveDirectory(wallpaperPath);
                }

                return result;
            }
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
            metadata.RemoveAll(w => w.title == wallpaper.title && w.description == wallpaper.description);
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

            string title = Path.GetFileNameWithoutExtension(wallpaperPath).Split('_')[0];

            string json = File.ReadAllText(metadataPath);
            var wallpapers = JsonSerializer.Deserialize<List<Wallpaper>>(json) ?? new List<Wallpaper>();

            return wallpapers.Find(w => w.title == title) ?? new Wallpaper("", "", "", "")
            {
                path = wallpaperPath
            };
        }

        private bool SaveWallpaperToAutoSaveDirectory(string wallpaperPath)
        {
            using (LogContext.PushProperty("Module", nameof(WallpaperService)))
            {
                if (string.IsNullOrEmpty(autoSaveDirectory) || !Directory.Exists(autoSaveDirectory))
                {
                    Log.Error("Auto save directory is not set or does not exist: {AutoSaveDirectory}", autoSaveDirectory);
                    Helpers.ToastHelper.ShowToast("自动保存失败", "自动保存目录未设置或不存在。");
                    return false;
                }

                string fileName = Path.GetFileName(wallpaperPath);
                string filePath = Path.Combine(autoSaveDirectory, fileName);
                
                if (File.Exists(filePath))
                {
                    Log.Information("Wallpaper already exists in auto save directory: {FilePath}", filePath);
                    return true;
                }

                try
                {
                    byte[] fileData = File.ReadAllBytes(wallpaperPath);
                    File.WriteAllBytes(filePath, fileData);

                    Log.Information("Wallpaper saved to auto save directory: {FilePath}", filePath);
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to save wallpaper to auto save directory: {Message}", ex.Message);
                    Helpers.ToastHelper.ShowToast("自动保存失败", "保存壁纸时发生错误：" + ex.Message);
                    return false;
                }
            }
        }

        public void CleanupOldWallpapers()
        {
            try
            {
                var currentWallpaperPath = GetCurrentWallpaper().path;
                var files = Directory.GetFiles(dataDirectory, "*.jpg");
                foreach (var file in files)
                {
                    if (!string.Equals(file, currentWallpaperPath, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(file);
                        Log.Information("Deleted old wallpaper file: {FilePath}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to cleanup old wallpapers: {Message}", ex.Message);
            }
        }

    }
}
