using SpotlightGallery.Models;
using System.IO;
using System.Text.RegularExpressions;

namespace SpotlightGallery.Helpers
{
    public static class FilenameHelper
    {
        /// <summary>
        /// 根据模板生成文件名
        /// </summary>
        public static string GetFormattedFilename(Wallpaper wallpaper, string template)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                return SanitizeFilename(wallpaper.title);
            }

            string filename = template;
            filename = filename.Replace("{title}", wallpaper.title ?? "");
            filename = filename.Replace("{description}", wallpaper.description ?? "");
            filename = filename.Replace("{copyright}", wallpaper.copyright ?? "");

            string author = GetAuthorFromCopyright(wallpaper.copyright);
            filename = filename.Replace("{author}", author);

            return SanitizeFilename(filename);
        }

        /// <summary>
        /// 从版权信息中提取作者名称
        /// </summary>
        private static string GetAuthorFromCopyright(string? copyright)
        {
            if (string.IsNullOrEmpty(copyright))
                return string.Empty;

            // Remove © symbol and trim
            string author = copyright.Replace("©", "").Trim();

            // Split by / and take the first part
            int slashIndex = author.IndexOf('/');
            if (slashIndex > 0)
            {
                author = author.Substring(0, slashIndex).Trim();
            }

            return author;
        }

        /// <summary>
        /// 将文件名中的非法字符移除
        /// </summary>
        public static string SanitizeFilename(string filename)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(filename, "");
        }
    }
}
