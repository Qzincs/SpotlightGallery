using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SpotlightGallery.Models.Spotlight
{
    public class SpotlightDesktopItemContent
    {
        [JsonPropertyName("ad")]
        public SpotlightDesktopAdContent Ad { get; set; }
    }

    public class SpotlightDesktopAdContent : IAdContent
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("iconHoverText")]
        public string IconHoverText { get; set; }
        [JsonPropertyName("copyright")]
        public string Copyright { get; set; }
        [JsonPropertyName("landscapeImage")]
        public DesktopLandscapeImageContent ImageContent { get; set; }

        public string GetCopyright() => Copyright;

        public string GetDescription() => IconHoverText;

        public string GetImageUrl() => ImageContent?.ImageUrl ?? string.Empty;

        public string GetTitle() => Title;
    }

    public class DesktopLandscapeImageContent
    {
        [JsonPropertyName("asset")]
        public string ImageUrl { get; set; }
    }
}
