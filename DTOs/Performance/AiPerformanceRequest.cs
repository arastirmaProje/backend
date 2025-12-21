using System.Text.Json.Serialization;

namespace Personelim.DTOs.Performance
{
    public class AiPerformanceRequest
    {
        [JsonPropertyName("calisan_id")]
        public Guid CalisanId { get; set; }

        [JsonPropertyName("ad_soyad")]
        public string AdSoyad { get; set; } = string.Empty;

        [JsonPropertyName("tamamlanan_gorev_sayisi")]
        public int TamamlananGorevSayisi { get; set; }

        [JsonPropertyName("tamamlanamayan_gorev_sayisi")]
        public int TamamlanamayanGorevSayisi { get; set; }

        [JsonPropertyName("hedeflenen_mesai_saati")]
        public double HedeflenenMesaiSaati { get; set; }

        [JsonPropertyName("gerceklesen_mesai_saati")]
        public double GerceklesenMesaiSaati { get; set; }

        [JsonPropertyName("kullanilan_izin_gunu")]
        public int KullanilanIzinGunu { get; set; }

        [JsonPropertyName("gorevler")]
        public List<AiTaskDto> Gorevler { get; set; } = new();
    }

    public class AiTaskDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("gorev_adi")]
        public string GorevAdi { get; set; } = string.Empty;

        [JsonPropertyName("zorluk_seviyesi")]
        public string ZorlukSeviyesi { get; set; } = "Belirtilmedi";

        [JsonPropertyName("durum")]
        public string Durum { get; set; } = "Beklemede";

        [JsonPropertyName("baslangic_tarihi")]
        public DateTime BaslangicTarihi { get; set; }

        [JsonPropertyName("bitistarihi")]
        public DateTime BitisTarihi { get; set; }

        [JsonPropertyName("aciklama")]
        public string? Aciklama { get; set; }

        [JsonPropertyName("geri_donut")]
        public string? GeriDonut { get; set; }
    }
}