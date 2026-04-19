using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Slack;
using Personelim.Helpers;
using Personelim.Models;
using Personelim.Services.SlackWebhook;
using System.Security.Claims;

namespace Personelim.Controllers;

/// <summary>İşletme bazlı Slack webhook yönetimi.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class SlackWebhooksController : ControllerBase
{
    private readonly ISlackWebhookService _service;

    public SlackWebhooksController(ISlackWebhookService service)
    {
        _service = service;
    }

    /// <summary>İşletmeye yeni bir Slack webhook ekler.</summary>
    /// <remarks>
    /// <b>eventType</b> örnekleri: <c>task_created</c>, <c>leave_request</c>, <c>member_added</c>
    ///
    /// Aynı eventType için birden fazla webhook eklenebilir (farklı kanallar).
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateSlackWebhookRequestDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.CreateAsync(userId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>İşletmenin tüm Slack webhook'larını listeler.</summary>
    /// <param name="businessId">İşletme ID</param>
    [HttpGet("{businessId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetByBusiness(Guid businessId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.GetByBusinessAsync(userId, businessId);
        return result.Success ? Ok(result) : Forbid();
    }

    /// <summary>Desteklenen tüm Slack event type'larını listeler.</summary>
    /// <remarks>Webhook eklerken <c>eventType</c> alanı için bu listeden seçim yapılmalıdır.</remarks>
    [HttpGet("event-types")]
    [ProducesResponseType(200)]
    public IActionResult GetEventTypes()
    {
        return Ok(SlackEventTypes.All);
    }

    /// <summary>Webhook URL, eventType veya label'ını günceller.</summary>
    /// <param name="id">Webhook ID</param>
    [HttpPut("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSlackWebhookRequestDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.UpdateAsync(userId, id, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Webhook'u aktif/pasif yapar.</summary>
    /// <param name="id">Webhook ID</param>
    [HttpPatch("{id}/toggle")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Toggle(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.ToggleActiveAsync(userId, id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Webhook'u siler.</summary>
    /// <param name="id">Webhook ID</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.DeleteAsync(userId, id);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
