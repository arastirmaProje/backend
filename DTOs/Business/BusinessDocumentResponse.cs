public class BusinessDocumentResponse
{
    public Guid Id { get; set; }
    public string? DocumentType { get; set; }
    public string FileName { get; set; } = default!;
    public string FileUrl { get; set; } = default!;
    public DateTime UploadedAt { get; set; }
}