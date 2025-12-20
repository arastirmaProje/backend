namespace Personelim.Models
{
    public class Business
    {
        public Guid Id { get; set; }
        public string Name { get; set; } 
        public string? Description { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string? VerificationCode { get; set; } 
        public bool IsVerified { get; set; } = false;
        
        
        public string LocationName { get; set; } 
        public double Latitude { get; set; }     
        public double Longitude { get; set; }    
        
        public string? ImageUrl { get; set; }
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public Guid OwnerId { get; set; }
        public Guid? ParentBusinessId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation Properties
        public Province? Province { get; set; }  
        public District? District { get; set; }
        public User? Owner { get; set; }
        public Business? ParentBusiness { get; set; }
        public ICollection<Business> SubBusinesses { get; set; }
        public ICollection<BusinessMember> Members { get; set; }
        public ICollection<Invitation> Invitations { get; set; }
        public ICollection<BusinessDocument> Documents { get; set; } = new List<BusinessDocument>();
        public Business()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            IsActive = true;
            Members = new List<BusinessMember>();
            Invitations = new List<Invitation>();
            SubBusinesses = new List<Business>();
        }
    }
}