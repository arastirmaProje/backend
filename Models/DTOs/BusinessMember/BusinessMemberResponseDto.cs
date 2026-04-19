namespace Personelim.DTOs.BusinessMember
{
    public class BusinessMemberResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public int PositionId { get; set; }
        public string PositionName { get; set; }
        public string Role { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public decimal? Salary { get; set; }
        public string? TCIdentityNumber { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsActive { get; set; }

        public List<MemberDocumentResponse> Documents { get; set; } = new();

        public class MemberDocumentResponse
        {
            public Guid Id { get; set; }
            public string DocumentType { get; set; }
            public string FileName { get; set; }
            public string FileUrl { get; set; }
            public DateTime UploadedAt { get; set; }
        }
    }
}
