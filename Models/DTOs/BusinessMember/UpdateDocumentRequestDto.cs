using Microsoft.AspNetCore.Http;

namespace Personelim.DTOs.BusinessMember
{
    public class UpdateDocumentRequestDto
    {
        public string DocumentType { get; set; } 
        
        public IFormFile? File { get; set; } 
    }
}