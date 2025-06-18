using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace SpotlightGallery.Models
{
    public class Wallpaper
    {
        [JsonPropertyName("title")]
        public string title { get; set; }

        [JsonPropertyName("description")]
        public string description { get; set; }

        [JsonPropertyName("copyright")]
        public string copyright { get; set; }

        [JsonPropertyName("url")]
        public string url { get; set; }

        [JsonPropertyName("path")]
        public string? path { get; set; }

        public Wallpaper()
        {
            title = string.Empty;
            description = string.Empty;
            copyright = string.Empty;
            url = string.Empty;
        }

        public Wallpaper(string title, string description, string copyright, string url)
        {
            this.title = title;
            this.description = description;
            this.copyright = copyright;
            this.url = url;
        }
    }
}
