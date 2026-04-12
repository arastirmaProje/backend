using System.Collections.Generic;

namespace Personelim.DTOs.Business
{
    public class CreateBusinessRequestDto
    {
        public string BusinessName { get; set; }
        public string PhoneNumber { get; set; }
        public int ProvinceId { get; set; }
        public int DistrictId { get; set; }
        public string Address { get; set; }
        public string? Description { get; set; }
        public List<OfficeLocationDto> Offices { get; set; }
    }

    public class OfficeLocationDto
    {
        public string OfficeName { get; set; } 
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}