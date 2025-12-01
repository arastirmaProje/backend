public class CreateBusinessRequest
{
    // Kullanıcı giriş doğrulaması için
    public string Email { get; set; }
    public string Password { get; set; }

    // Şirket bilgileri
    public string BusinessName { get; set; }
    public string PhoneNumber { get; set; }
    public int ProvinceId { get; set; }
    public int DistrictId { get; set; }
    public string Address { get; set; }
    public string description { get; set; }
    public double BusinessLatitude { get; set; }
    public double BusinessLongitude { get; set; }

    // Ofis bilgileri
    public string OfficeName { get; set; }
    public double OfficeLatitude { get; set; }
    public double OfficeLongitude { get; set; }
}