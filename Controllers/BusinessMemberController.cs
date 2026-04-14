using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.BusinessMember;
using Personelim.Services.BusinessMember;
using System.Security.Claims;
using Personelim.Helpers;

namespace Personelim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BusinessMemberController : ControllerBase
    {
        private readonly IBusinessMemberService _memberService;

        public BusinessMemberController(IBusinessMemberService memberService)
        {
            _memberService = memberService;
        }

        [HttpGet("business/{businessId}")]
        public async Task<IActionResult> GetMembersByBusiness(Guid businessId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();
            var result = await _memberService.GetMembersByBusinessIdAsync(userId, businessId);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("{memberId}")]
        public async Task<IActionResult> GetMemberById(Guid memberId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();
            var result = await _memberService.GetMemberByIdAsync(userId, memberId);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{memberId}")]
        public async Task<IActionResult> UpdateMember(Guid memberId, [FromBody] UpdateBusinessMemberRequestDto requestDto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();
            var result = await _memberService.UpdateMemberAsync(userId, memberId, requestDto);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{memberId}")]
        public async Task<IActionResult> RemoveMember(Guid memberId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();
            var result = await _memberService.RemoveMemberAsync(userId, memberId);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("{memberId}/documents")]
        public async Task<IActionResult> UploadDocument(Guid memberId, [FromForm] UploadDocumentRequestDto requestDto)
        {
            if (requestDto.File == null || requestDto.File.Length == 0)
            {
                return BadRequest(ServiceResponse<object>.ErrorResult("Lütfen bir dosya seçiniz."));
            }

            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _memberService.UploadDocumentAsync(userId, memberId, requestDto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("documents/{documentId}")]
        public async Task<IActionResult> UpdateDocument(Guid documentId, [FromForm] UpdateDocumentRequestDto requestDto)
        {
            if (string.IsNullOrWhiteSpace(requestDto.DocumentType) && requestDto.File == null)
            {
                return BadRequest(
                    ServiceResponse<object>.ErrorResult(
                        "Güncellenecek bir bilgi (Belge Tipi veya Dosya) göndermelisiniz."));
            }

            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _memberService.UpdateDocumentAsync(userId, documentId, requestDto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("documents/{documentId}")]
        public async Task<IActionResult> DeleteDocument(Guid documentId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _memberService.DeleteDocumentAsync(userId, documentId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("documents/{documentId}/download")]
        public async Task<IActionResult> GetDocument(Guid documentId)
        {
            var currentUserId = GetUserId();
            if (currentUserId == Guid.Empty) return Unauthorized();

            var result = await _memberService.GetDocumentFileAsync(currentUserId, documentId);

            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            var fileData = result.Data;

            return File(fileData.FileBytes, fileData.ContentType, fileData.FileName);
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim?.Value, out var id) ? id : Guid.Empty;
        }
    }
}