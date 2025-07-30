using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SpotlightGallery.Models
{
    [JsonSerializable(typeof(Wallpaper))]
    [JsonSerializable(typeof(SpotlightResponse))]
    [JsonSerializable(typeof(SpotlightDesktopItemContent))]
    [JsonSerializable(typeof(SpotlightDesktopAdContent))]
    [JsonSerializable(typeof(BingDailyResponse))]
    public partial class WallpaperJsonContext : JsonSerializerContext { }
}