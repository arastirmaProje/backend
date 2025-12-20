using Personelim.Models;

public class BusinessDocument
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Business Business { get; set; }

    public string? DocumentType { get; set; }   
    public string FileName { get; set; }        
    public string FilePath { get; set; }        
    public string FileExtension { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}