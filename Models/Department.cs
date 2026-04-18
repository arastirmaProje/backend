namespace Personelim.Models
{
    public class Department
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        public Business? Business { get; set; }
        public ICollection<BusinessMember> Members { get; set; } = new List<BusinessMember>();

        public Department()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }
    }
}
