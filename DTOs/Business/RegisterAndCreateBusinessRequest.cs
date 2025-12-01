namespace Personelim.DTOs.Business
{
    public class LoginAndCreateBusinessRequest
    {
        // Kullanıcı bilgileri
        public string Email { get; set; }
        public string Password { get; set; }

        // İşletme bilgileri
        public string BusinessName { get; set; }
        public string PhoneNumber { get; set; }
        public int ProvinceId { get; set; }
        public int DistrictId { get; set; }
        public string Address { get; set; }
        public string? Description { get; set; }
        


        // Opsiyonel ofis bilgileri
        public string? OfficeName { get; set; }
        public double BusinessLatitude { get; set; }
        public double BusinessLongitude { get; set; }
        public double? OfficeLatitude { get; set; }
        public double? OfficeLongitude { get; set; }
    }
}