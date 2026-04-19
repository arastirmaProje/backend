namespace Personelim.Models;

public class Schedule
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Business? Business { get; set; }
    public User? CreatedBy { get; set; }

    public Schedule()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }
}
