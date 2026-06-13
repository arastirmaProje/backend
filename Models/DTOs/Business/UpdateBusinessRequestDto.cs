namespace Personelim.DTOs.Business
{
    public class UpdateBusinessRequestDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public IFormFile? Image { get; set; }

        public string? LocationName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public List<UpdateOfficeLocationDto>? Offices { get; set; }
    }

    public class UpdateOfficeLocationDto
    {
        public Guid? Id { get; set; }
        public string OfficeName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
