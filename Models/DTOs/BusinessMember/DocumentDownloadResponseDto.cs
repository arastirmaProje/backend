namespace Personelim.DTOs.BusinessMember
{
    public class DocumentDownloadResponseDto
    {
        public byte[] FileBytes { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
    }
}