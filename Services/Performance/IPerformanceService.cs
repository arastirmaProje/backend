using Personelim.DTOs.Performance;
using Personelim.Helpers;

namespace Personelim.Services.Performance
{
    public interface IPerformanceService
    {
        Task<ServiceResponse<AiPerformanceBulkScoreResponse>> QueryBulkScoresAsync(Guid currentUserId, PerformanceBulkQueryRequest request);
        Task<ServiceResponse<AiPerformanceResponseDto>> QueryAsync(Guid currentUserId, PerformanceQueryRequestDto requestDto);

        Task<ServiceResponse<List<PerformanceReportListItemDto>>> GetReportsByEmployeeAsync(Guid currentUserId, Guid businessId, Guid employeeUserId);

        Task<ServiceResponse<AiPerformanceResponseDto>> GetReportByIdAsync(Guid currentUserId, Guid reportId);
    }
}