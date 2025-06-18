using SpotlightGallery.Models.Spotlight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SpotlightGallery.Models
{
    public class SpotlightLockscreenItemContent
    {
        [JsonPropertyName("ad")]
        public SpotlightLockscreenAdContent Ad { get; set; }
    }

    public class SpotlightLockscreenAdContent : IAdContent
    {
        [JsonPropertyName("title_text")]
        public TextContent TitleText { get; set; }
        [JsonPropertyName("hs1_title_text")]
        public TextContent Hs1TitleText { get; set; }
        [JsonPropertyName("copyright_text")]
        public TextContent CopyrightText { get; set; }
        [JsonPropertyName("image_fullscreen_001_landscape")]
        public LockScreenLandscapeImageContent ImageContent { get; set; }

        public string GetCopyright() => CopyrightText?.Text ?? string.Empty;
        public string GetDescription() => Hs1TitleText?.Text ?? string.Empty;
        public string GetImageUrl() => ImageContent?.ImageUrl ?? string.Empty;
        public string GetTitle() => TitleText?.Text ?? string.Empty;
    }

    public class TextContent
    {
        [JsonPropertyName("tx")]
        public string Text { get; set; }
    }

    public class LockScreenLandscapeImageContent
    {
        [JsonPropertyName("u")]
        public string ImageUrl { get; set; }
    }
}
