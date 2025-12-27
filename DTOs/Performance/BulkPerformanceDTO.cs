using System.Text.Json.Serialization;

namespace Personelim.DTOs.Performance
{
    public class PerformanceBulkQueryRequest
    {
        [JsonPropertyName("businessId")]
        public Guid BusinessId { get; set; }

        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime EndDate { get; set; }
    }

    public class AiPerformanceBulkScoreResponse
    {
        [JsonPropertyName("toplam_calisan")]
        public int ToplamCalisan { get; set; }

        [JsonPropertyName("skorlar")]
        public List<AiPerformanceBulkScoreItem> Skorlar { get; set; } = new();
    }

    public class AiPerformanceBulkScoreItem
    {
        [JsonPropertyName("calisan_id")]
        public Guid CalisanId { get; set; }

        [JsonPropertyName("ad_soyad")]
        public string AdSoyad { get; set; } = "";

        [JsonPropertyName("performans_skoru")]
        public int PerformansSkoru { get; set; }
    }
}