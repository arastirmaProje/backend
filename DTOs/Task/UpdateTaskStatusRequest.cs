using Personelim.Models.Enums;

namespace Personelim.DTOs.Task
{
    public class UpdateTaskStatusRequest
    {
        public Models.Enums.TaskStatus Status { get; set; }
        public string Thoughts { get; set; } // "İş bitti, şu kısım zorladı" vb.
    }
}