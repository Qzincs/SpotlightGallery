using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
    }

    class WallpaperService : IWallpaperService
    {
        // TODO 壁纸保存路径可以由用户配置
        private readonly string wallpaperDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "SpotlightGallery");
        private readonly string metadataDirectory = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "WallpaperMetadata");

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

            string wallpaperPath = Path.Combine(wallpaperDirectory, $"{wallpaper.title}.jpg");

            using (var httpClient = new HttpClient())
            {
                // 下载壁纸图片
                HttpResponseMessage imageResponse = await httpClient.GetAsync(wallpaper.url);
                byte[] imageData = await imageResponse.Content.ReadAsByteArrayAsync();

                if (!File.Exists(wallpaperDirectory))
                {
                    Directory.CreateDirectory(wallpaperDirectory);
                }

                // 将壁纸保存到本地
                File.WriteAllBytes(wallpaperPath, imageData);
            }

            wallpaper.path = wallpaperPath;

            return wallpaper;
        }

    }
}
