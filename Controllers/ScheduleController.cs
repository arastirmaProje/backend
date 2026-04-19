using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Schedule;
using Personelim.Models.Enums;
using Personelim.Services.Schedule;
using System.Security.Claims;

namespace Personelim.Controllers;

/// <summary>Toplantı ve etkinlik yönetimi.</summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public class ScheduleController : ControllerBase
{
    private readonly IScheduleService _service;

    public ScheduleController(IScheduleService service)
    {
        _service = service;
    }

    /// <summary>Yeni toplantı veya etkinlik oluşturur. Type: 0 = Toplantı, 1 = Etkinlik</summary>
    [HttpPost("api/schedules")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateScheduleRequestDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.CreateAsync(userId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>İşletmenin planlarını listeler. Type filtresi opsiyonel: 0 = Toplantı, 1 = Etkinlik</summary>
    /// <param name="businessId">İşletme ID</param>
    /// <param name="type">0 = Toplantı, 1 = Etkinlik (opsiyonel)</param>
    [HttpGet("api/schedules/{businessId}")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Get(Guid businessId, [FromQuery] ScheduleType? type)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.GetByBusinessAsync(userId, businessId, type);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Toplantı veya etkinliği siler.</summary>
    /// <param name="id">Schedule ID</param>
    [HttpDelete("api/schedules/{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.DeleteAsync(userId, id);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
