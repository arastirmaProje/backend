using Personelim.Models.Enums;

namespace Personelim.DTOs.Leave
{
    public class UpdateLeaveStatusRequestDto
    {
        public LeaveStatus Status { get; set; } 
        public string? RejectionReason { get; set; } 
    }
}