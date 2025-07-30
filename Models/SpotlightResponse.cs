using System.Text.Json.Serialization;

namespace SpotlightGallery.Models
{
    public class SpotlightResponse
    {
        [JsonPropertyName("batchrsp")]
        public BatchResponse BatchResponse { get; set; }
    }

    public class BatchResponse
    {
        [JsonPropertyName("items")]
        public ItemWrapper[] Items { get; set; }
    }

    public class ItemWrapper
    {
        [JsonPropertyName("item")]
        public string ItemJson { get; set; }
    }

    public interface IAdContent
    {
        string GetTitle();
        string GetDescription();
        string GetCopyright();
        string GetImageUrl();
    }
}