using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Personelim.DTOs.Performance;
using Personelim.Services.Performance;
using System.Security.Claims;

namespace Personelim.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PerformanceController : ControllerBase
    {
        private readonly IPerformanceService _service;

        public PerformanceController(IPerformanceService service)
        {
            _service = service;
        }

        [HttpPost("query")]
        public async Task<IActionResult> Query([FromBody] PerformanceQueryRequestDto requestDto)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _service.QueryAsync(userId, requestDto);
            return result.Success ? Ok(result) : BadRequest(result);
        }
        
        [HttpPost("query-bulk-scores")]
        public async Task<IActionResult> QueryBulkScores([FromBody] PerformanceBulkQueryRequest request)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _service.QueryBulkScoresAsync(userId, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("business/{businessId}/employee/{employeeUserId}")]
        public async Task<IActionResult> GetReports(Guid businessId, Guid employeeUserId)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _service.GetReportsByEmployeeAsync(userId, businessId, employeeUserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("query-department")]
        public async Task<IActionResult> QueryDepartment([FromBody] DepartmanPerformanceQueryRequestDto request)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _service.QueryDepartmentAsync(userId, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("query-department-charts")]
        public async Task<IActionResult> QueryDepartmentCharts([FromBody] DepartmentChartsRequestDto request)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _service.QueryDepartmentChartsAsync(userId, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("query-employee-charts")]
        public async Task<IActionResult> QueryEmployeeCharts([FromBody] PerformanceQueryRequestDto requestDto)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _service.QueryEmployeeChartsAsync(userId, requestDto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{reportId}")]
        public async Task<IActionResult> GetReport(Guid reportId)
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _service.GetReportByIdAsync(userId, reportId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private Guid GetUserIdFromToken()
        {
            var idClaim = User.Claims.FirstOrDefault(c =>
                c.Type == "uid" || c.Type == "id" || c.Type == ClaimTypes.NameIdentifier);

            return idClaim != null ? Guid.Parse(idClaim.Value) : Guid.Empty;
        }
    }
}