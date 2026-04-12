public class UploadBusinessDocumentRequestDto
{
    public string? DocumentType { get; set; }
    public IFormFile File { get; set; } = default!;
}