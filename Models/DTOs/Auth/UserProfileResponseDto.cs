namespace Personelim.DTOs.Auth
{
    public class UserProfileResponseDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int BusinessCount { get; set; }
        public int OwnedBusinessCount { get; set; }
        public List<UserMembershipDto> Memberships { get; set; } = new();
    }

    public class UserMembershipDto
    {
        public Guid BusinessMemberId { get; set; }
        public Guid BusinessId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int PositionId { get; set; }
        public string PositionName { get; set; } = string.Empty;
    }
}
