using System.Text.Json;
using System.Text.Json.Serialization;

namespace Personelim.DTOs.Performance
{
    public class DepartmanPerformanceQueryRequestDto
    {
        public Guid BusinessId { get; set; }
        public Guid DepartmentId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class DepartmentChartsRequestDto
    {
        [JsonPropertyName("businessId")] public Guid BusinessId { get; set; }
        [JsonPropertyName("startDate")] public DateTime StartDate { get; set; }
        [JsonPropertyName("endDate")] public DateTime EndDate { get; set; }
    }

    public class AiDepartmanCalisaniDto
    {
        [JsonPropertyName("calisan_id")] public Guid CalisanId { get; set; }
        [JsonPropertyName("ad_soyad")] public string AdSoyad { get; set; } = string.Empty;
        [JsonPropertyName("tamamlanan_gorev_sayisi")] public int TamamlananGorevSayisi { get; set; }
        [JsonPropertyName("tamamlanamayan_gorev_sayisi")] public int TamamlanamayanGorevSayisi { get; set; }
        [JsonPropertyName("hedeflenen_mesai_saati")] public double HedeflenenMesaiSaati { get; set; }
        [JsonPropertyName("gerceklesen_mesai_saati")] public double GerceklesenMesaiSaati { get; set; }
        [JsonPropertyName("kullanilan_izin_gunu")] public int KullanilanIzinGunu { get; set; }
        [JsonPropertyName("onceki_performans_skoru")] public double? OncekiPerformansSkoru { get; set; }
        [JsonPropertyName("gorevler")] public List<AiTaskDto> Gorevler { get; set; } = new();
    }

    public class AiDepartmanIstegiDto
    {
        [JsonPropertyName("departman_id")] public Guid DepartmanId { get; set; }
        [JsonPropertyName("departman_adi")] public string DepartmanAdi { get; set; } = string.Empty;
        [JsonPropertyName("calisanlar")] public List<AiDepartmanCalisaniDto> Calisanlar { get; set; } = new();
    }

    public class AiCalisanSkorOzetiDto
    {
        [JsonPropertyName("calisan_id")] public Guid CalisanId { get; set; }
        [JsonPropertyName("ad_soyad")] public string AdSoyad { get; set; } = string.Empty;
        [JsonPropertyName("performans_skoru")] public double PerformansSkoru { get; set; }
    }

    public class AiDepartmanRaporuDto
    {
        [JsonPropertyName("departman_id")] public Guid DepartmanId { get; set; }
        [JsonPropertyName("departman_adi")] public string DepartmanAdi { get; set; } = string.Empty;
        [JsonPropertyName("departman_skoru")] public double DepartmanSkoru { get; set; }
        [JsonPropertyName("toplam_calisan")] public int ToplamCalisan { get; set; }
        [JsonPropertyName("calisan_skorlari")] public List<AiCalisanSkorOzetiDto> CalisanSkorlari { get; set; } = new();
        [JsonPropertyName("rapor_ozeti")] public string RaporOzeti { get; set; } = string.Empty;
        [JsonPropertyName("detayli_rapor")] public string DetayliRapor { get; set; } = string.Empty;
        [JsonPropertyName("grafik_verisi")] public JsonElement? GrafikVerisi { get; set; }
    }
}
