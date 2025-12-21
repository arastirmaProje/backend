using System.Text.Json.Serialization;

namespace Personelim.DTOs.Performance
{
    public class AiPerformanceResponse
    {
        [JsonPropertyName("calisan_id")]
        public Guid CalisanId { get; set; }

        [JsonPropertyName("performans_skoru")]
        public double PerformansSkoru { get; set; }

        [JsonPropertyName("rapor_ozeti")]
        public string? RaporOzeti { get; set; }

        [JsonPropertyName("detayli_rapor")]
        public string? DetayliRapor { get; set; }

        [JsonPropertyName("onceki_raporlar")]
        public object? OncekiRaporlar { get; set; }
    }
}