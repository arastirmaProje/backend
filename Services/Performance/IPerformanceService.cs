using Personelim.DTOs.Performance;
using Personelim.Helpers;

namespace Personelim.Services.Performance
{
    public interface IPerformanceService
    {
        Task<ServiceResponse<AiPerformanceBulkScoreResponse>> QueryBulkScoresAsync(Guid currentUserId, PerformanceBulkQueryRequest request);
        Task<ServiceResponse<AiPerformanceResponse>> QueryAsync(Guid currentUserId, PerformanceQueryRequest request);

        Task<ServiceResponse<List<PerformanceReportListItem>>> GetReportsByEmployeeAsync(Guid currentUserId, Guid businessId, Guid employeeUserId);

        Task<ServiceResponse<AiPerformanceResponse>> GetReportByIdAsync(Guid currentUserId, Guid reportId);
    }
}