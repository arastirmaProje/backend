public class UploadBusinessDocumentRequest
{
    public string? DocumentType { get; set; }
    public IFormFile File { get; set; } = default!;
}