using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Meeting;
using Personelim.Helpers;
using Personelim.Services.Meeting;
using System.Security.Claims;

namespace Personelim.Controllers;

/// <summary>Toplantı ve etkinlik yönetimi.</summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public class MeetingsController : ControllerBase
{
    private readonly IMeetingService _service;

    public MeetingsController(IMeetingService service)
    {
        _service = service;
    }

    /// <summary>Yeni toplantı oluşturur ve Slack'e bildirim gönderir.</summary>
    [HttpPost("api/meetings")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateMeeting([FromBody] CreateMeetingRequestDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.CreateAsync(userId, dto, MeetingTypes.Meeting);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>İşletmenin toplantılarını listeler.</summary>
    /// <param name="businessId">İşletme ID</param>
    [HttpGet("api/meetings/{businessId}")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetMeetings(Guid businessId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.GetByBusinessAsync(userId, businessId, MeetingTypes.Meeting);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Yeni etkinlik oluşturur ve Slack'e bildirim gönderir.</summary>
    [HttpPost("api/events")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateEvent([FromBody] CreateMeetingRequestDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.CreateAsync(userId, dto, MeetingTypes.Event);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>İşletmenin etkinliklerini listeler.</summary>
    /// <param name="businessId">İşletme ID</param>
    [HttpGet("api/events/{businessId}")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetEvents(Guid businessId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.GetByBusinessAsync(userId, businessId, MeetingTypes.Event);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Toplantı veya etkinliği siler.</summary>
    /// <param name="id">ID</param>
    [HttpDelete("api/meetings/{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.DeleteAsync(userId, id);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
