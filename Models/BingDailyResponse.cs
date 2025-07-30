using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SpotlightGallery.Models
{
    public class BingDailyResponse
    {
        [JsonPropertyName("images")]
        public List<BingImage> Images { get; set; }
        [JsonPropertyName("imageCount")]
        public int ImageCount { get; set; }
    }

    public class BingImage
    {
        [JsonPropertyName("urlbase")]
        public string Url { get; set; }
        [JsonPropertyName("copyrighttext")]
        public string Copyright { get; set; }
        [JsonPropertyName("headline")]
        public string Headline { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("fullDateString")]
        public string FullDateString { get; set; }
    }
}
