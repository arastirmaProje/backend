using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Personelim.Data;
using Personelim.DTOs.Performance;
using Personelim.Helpers;
using Personelim.Models;
using Personelim.Models.Enums;
using Personelim.Resources;
using System.Net.Http.Json;
using System.Text.Json;

namespace Personelim.Services.Performance
{
    public class PerformanceService : IPerformanceService
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _http;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public PerformanceService(AppDbContext context, IHttpClientFactory factory, IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _localizer = localizer;
            _http = factory.CreateClient("AiPerformance");
        }

        public async Task<ServiceResponse<AiPerformanceResponseDto>> QueryAsync(Guid currentUserId, PerformanceQueryRequestDto requestDto)
        {
            try
            {
                if (requestDto.BusinessId == Guid.Empty || requestDto.EmployeeUserId == Guid.Empty)
                    return ServiceResponse<AiPerformanceResponseDto>.ErrorResult(_localizer["BusinessOrUserNotFound"]);
                
                if (requestDto.EndDate.Date < requestDto.StartDate.Date)
                    return ServiceResponse<AiPerformanceResponseDto>.ErrorResult(_localizer["StartDateAfterEndDate"]);

                var isEmployee = await _context.BusinessMembers.AnyAsync(bm =>
                    bm.UserId == requestDto.EmployeeUserId &&
                    bm.BusinessId == requestDto.BusinessId &&
                    bm.IsActive);
                
                if (!isEmployee)
                    return ServiceResponse<AiPerformanceResponseDto>.ErrorResult(_localizer["EmployeeNotActiveInBusiness"]);

                var employee = await _context.Users.FirstOrDefaultAsync(u => u.Id == requestDto.EmployeeUserId && u.IsActive);
                if (employee == null)
                    return ServiceResponse<AiPerformanceResponseDto>.ErrorResult(_localizer["EmployeeNotFound"]);

                var start = requestDto.StartDate.Date;
                var end = requestDto.EndDate.Date.AddDays(1).AddTicks(-1);
                
                var tasks = await _context.TaskItems
                    .Where(t =>
                        t.BusinessId == requestDto.BusinessId &&
                        t.AssignedToUserId == requestDto.EmployeeUserId &&
                        t.StartDate <= end &&
                        t.EndDate >= start
                    )
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
                
                var validTasks = tasks.Where(IsValidTaskForAi).ToList();
                int completed = validTasks.Count(t => string.Equals(Normalize(t.Status), "Tamamlandı", StringComparison.OrdinalIgnoreCase));
                int notCompleted = validTasks.Count(t => string.Equals(Normalize(t.Status), "Tamamlanmadı", StringComparison.OrdinalIgnoreCase));

                var realizedHoursDec = await _context.Shifts
                    .Where(s => s.BusinessId == requestDto.BusinessId && s.UserId == requestDto.EmployeeUserId && s.StartTime >= start && s.EndTime <= end)
                    .SumAsync(s => (decimal?)s.TotalHours) ?? 0m;
                
                double realizedHours = (double)realizedHoursDec;
                int dayCount = (requestDto.EndDate.Date - requestDto.StartDate.Date).Days + 1;
                double targetHours = dayCount * 8;
                
                int usedLeaveDays = await _context.MemberLeaves
                    .Include(l => l.BusinessMember)
                    .Where(l => l.BusinessMember.BusinessId == requestDto.BusinessId && l.BusinessMember.UserId == requestDto.EmployeeUserId && l.StartDate <= end && l.EndDate >= start)
                    .SumAsync(l => (int?)l.DayCount) ?? 0;

                var aiPayload = new AiPerformanceRequestDto
                {
                    CalisanId = employee.Id,
                    AdSoyad = $"{employee.FirstName} {employee.LastName}".Trim(),
                    TamamlananGorevSayisi = completed,
                    TamamlanamayanGorevSayisi = notCompleted,
                    HedeflenenMesaiSaati = targetHours,
                    GerceklesenMesaiSaati = realizedHours,
                    KullanilanIzinGunu = usedLeaveDays,
                    Gorevler = validTasks.Select(t => new AiTaskDto
                    {
                        Id = t.Id,
                        GorevAdi = t.Title ?? "",
                        ZorlukSeviyesi = t.Difficulty!,
                        Durum = t.Status,
                        BaslangicTarihi = t.StartDate,
                        BitisTarihi = t.EndDate,
                        Aciklama = t.Description,
                        GeriDonut = t.Thoughts
                    }).ToList()
                };

                var resp = await _http.PostAsJsonAsync("", aiPayload);
                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    return ServiceResponse<AiPerformanceResponseDto>.ErrorResult(_localizer["AiServiceError"], body);
                }

                var aiResult = await resp.Content.ReadFromJsonAsync<AiPerformanceResponseDto>();
                if (aiResult == null)
                    return ServiceResponse<AiPerformanceResponseDto>.ErrorResult(_localizer["AiResponseReadError"]);

                var report = new PerformanceReport
                {
                    Id = Guid.NewGuid(),
                    BusinessId = requestDto.BusinessId,
                    EmployeeUserId = requestDto.EmployeeUserId,
                    RequestedByUserId = currentUserId,
                    PeriodStart = requestDto.StartDate.Date,
                    PeriodEnd = requestDto.EndDate.Date,
                    CompletedTaskCount = completed,
                    NotCompletedTaskCount = notCompleted,
                    TargetWorkHours = targetHours,
                    RealizedWorkHours = realizedHours,
                    UsedLeaveDays = usedLeaveDays,
                    PerformanceScore = aiResult.PerformansSkoru,
                    Summary = aiResult.RaporOzeti,
                    DetailedReport = aiResult.DetayliRapor,
                    AiRequestJson = JsonSerializer.Serialize(aiPayload),
                    AiResponseJson = JsonSerializer.Serialize(aiResult),
                    CreatedAt = DateTime.UtcNow
                };

                _context.PerformanceReports.Add(report);
                await _context.SaveChangesAsync();
                return ServiceResponse<AiPerformanceResponseDto>.SuccessResult(aiResult);
            }
            catch (Exception ex)
            {
                return ServiceResponse<AiPerformanceResponseDto>.ErrorResult(_localizer["PerformanceReportError"], ex.Message);
            }
        }

        public async Task<ServiceResponse<List<PerformanceReportListItemDto>>> GetReportsByEmployeeAsync(Guid currentUserId, Guid businessId, Guid employeeUserId)
        {
            try
            {
                var list = await _context.PerformanceReports
                    .Where(r => r.BusinessId == businessId && r.EmployeeUserId == employeeUserId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new PerformanceReportListItemDto
                    {
                        Id = r.Id, BusinessId = r.BusinessId, EmployeeUserId = r.EmployeeUserId,
                        PeriodStart = r.PeriodStart, PeriodEnd = r.PeriodEnd,
                        PerformanceScore = r.PerformanceScore, CreatedAt = r.CreatedAt
                    }).ToListAsync();
                return ServiceResponse<List<PerformanceReportListItemDto>>.SuccessResult(list);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<PerformanceReportListItemDto>>.ErrorResult(_localizer["ReportsFetchError"], ex.Message);
            }
        }

        public async Task<ServiceResponse<AiPerformanceResponseDto>> GetReportByIdAsync(Guid currentUserId, Guid reportId)
        {
            try
            {
                var report = await _context.PerformanceReports.FirstOrDefaultAsync(r => r.Id == reportId);
                if (report == null)
                    return ServiceResponse<AiPerformanceResponseDto>.ErrorResult(_localizer["ReportNotFound"]);
                
                var ai = JsonSerializer.Deserialize<AiPerformanceResponseDto>(report.AiResponseJson);
                if (ai == null)
                    return ServiceResponse<AiPerformanceResponseDto>.ErrorResult(_localizer["SavedReportReadError"]);
                
                return ServiceResponse<AiPerformanceResponseDto>.SuccessResult(ai);
            }
            catch (Exception ex)
            {
                return ServiceResponse<AiPerformanceResponseDto>.ErrorResult(_localizer["GeneralError"], ex.Message);
            }
        }

        public async Task<ServiceResponse<AiPerformanceBulkScoreResponse>> QueryBulkScoresAsync(Guid currentUserId, PerformanceBulkQueryRequest request)
        {
            try
            {
                if (request.BusinessId == Guid.Empty)
                    return ServiceResponse<AiPerformanceBulkScoreResponse>.ErrorResult(_localizer["BusinessIdRequired"]);
                
                if (request.EndDate.Date < request.StartDate.Date)
                    return ServiceResponse<AiPerformanceBulkScoreResponse>.ErrorResult(_localizer["StartDateAfterEndDate"]);
                
                var start = request.StartDate.Date;
                var end = request.EndDate.Date.AddDays(1).AddTicks(-1);
                var activeMemberUserIds = await _context.BusinessMembers
                    .Where(bm => bm.BusinessId == request.BusinessId && bm.IsActive)
                    .Select(bm => bm.UserId).Distinct().ToListAsync();

                var response = new AiPerformanceBulkScoreResponse { ToplamCalisan = activeMemberUserIds.Count };
                if (activeMemberUserIds.Count == 0) return ServiceResponse<AiPerformanceBulkScoreResponse>.SuccessResult(response);

                var users = await _context.Users.Where(u => activeMemberUserIds.Contains(u.Id) && u.IsActive)
                    .Select(u => new { u.Id, u.FirstName, u.LastName }).ToListAsync();
                var fullNameByUserId = users.ToDictionary(x => x.Id, x => $"{x.FirstName} {x.LastName}".Trim());
                
                var allTasks = await _context.TaskItems.Where(t => t.BusinessId == request.BusinessId && activeMemberUserIds.Contains(t.AssignedToUserId) && t.StartDate <= end && t.EndDate >= start)
                    .OrderByDescending(t => t.CreatedAt).ToListAsync();
                var tasksByUser = allTasks.GroupBy(t => t.AssignedToUserId).ToDictionary(g => g.Key, g => g.ToList());
                
                var shiftSums = await _context.Shifts.Where(s => s.BusinessId == request.BusinessId && activeMemberUserIds.Contains(s.UserId) && s.StartTime >= start && s.EndTime <= end)
                    .GroupBy(s => s.UserId).Select(g => new { UserId = g.Key, Total = g.Sum(x => (decimal?)x.TotalHours) ?? 0m }).ToListAsync();
                var realizedHoursByUser = shiftSums.ToDictionary(x => x.UserId, x => (double)x.Total);
                
                var leaveSums = await _context.MemberLeaves.Include(l => l.BusinessMember).Where(l => l.BusinessMember.BusinessId == request.BusinessId && activeMemberUserIds.Contains(l.BusinessMember.UserId) && l.Status == LeaveStatus.Approved && l.StartDate <= end && l.EndDate >= start)
                    .GroupBy(l => l.BusinessMember.UserId).Select(g => new { UserId = g.Key, Days = g.Sum(x => (int?)x.DayCount) ?? 0 }).ToListAsync();
                var usedLeaveDaysByUser = leaveSums.ToDictionary(x => x.UserId, x => x.Days);

                int dayCount = (request.EndDate.Date - request.StartDate.Date).Days + 1;
                double targetHours = dayCount * 8;

                foreach (var employeeUserId in activeMemberUserIds)
                {
                    var fullName = fullNameByUserId.TryGetValue(employeeUserId, out var fn) ? fn : "";
                    tasksByUser.TryGetValue(employeeUserId, out var empTasks);
                    empTasks ??= new List<TaskItem>();
                    
                    var validTasks = empTasks.Where(IsValidTaskForAi).ToList();
                    int completed = validTasks.Count(t => string.Equals(Normalize(t.Status), "Tamamlandı", StringComparison.OrdinalIgnoreCase));
                    int notCompleted = validTasks.Count(t => string.Equals(Normalize(t.Status), "Tamamlanmadı", StringComparison.OrdinalIgnoreCase));
                    
                    realizedHoursByUser.TryGetValue(employeeUserId, out var realizedHours);
                    usedLeaveDaysByUser.TryGetValue(employeeUserId, out var usedLeaveDays);

                    var aiPayload = new AiPerformanceRequestDto { CalisanId = employeeUserId, AdSoyad = fullName, TamamlananGorevSayisi = completed, TamamlanamayanGorevSayisi = notCompleted, HedeflenenMesaiSaati = targetHours, GerceklesenMesaiSaati = realizedHours, KullanilanIzinGunu = usedLeaveDays, Gorevler = validTasks.Select(t => new AiTaskDto { Id = t.Id, GorevAdi = t.Title ?? "", ZorlukSeviyesi = t.Difficulty!, Durum = t.Status!, BaslangicTarihi = t.StartDate, BitisTarihi = t.EndDate, Aciklama = t.Description, GeriDonut = t.Thoughts }).ToList() };

                    int scoreToReturn = 0;
                    try {
                        var respAi = await _http.PostAsJsonAsync("", aiPayload);
                        if (respAi.IsSuccessStatusCode) {
                            var aiResult = await respAi.Content.ReadFromJsonAsync<AiPerformanceResponseDto>();
                            if (aiResult != null) {
                                scoreToReturn = (int)Math.Round(aiResult.PerformansSkoru);
                                _context.PerformanceReports.Add(new PerformanceReport { Id = Guid.NewGuid(), BusinessId = request.BusinessId, EmployeeUserId = employeeUserId, RequestedByUserId = currentUserId, PeriodStart = request.StartDate.Date, PeriodEnd = request.EndDate.Date, CompletedTaskCount = completed, NotCompletedTaskCount = notCompleted, TargetWorkHours = targetHours, RealizedWorkHours = realizedHours, UsedLeaveDays = usedLeaveDays, PerformanceScore = aiResult.PerformansSkoru, Summary = aiResult.RaporOzeti, DetailedReport = aiResult.DetayliRapor, AiRequestJson = JsonSerializer.Serialize(aiPayload), AiResponseJson = JsonSerializer.Serialize(aiResult), CreatedAt = DateTime.UtcNow });
                            }
                        }
                    } catch {  }

                    response.Skorlar.Add(new AiPerformanceBulkScoreItem { CalisanId = employeeUserId, AdSoyad = fullName, PerformansSkoru = scoreToReturn });
                }
                await _context.SaveChangesAsync();
                return ServiceResponse<AiPerformanceBulkScoreResponse>.SuccessResult(response);
            }
            catch (Exception ex)
            {
                return ServiceResponse<AiPerformanceBulkScoreResponse>.ErrorResult(_localizer["BulkPerformanceError"], ex.Message);
            }
        }

        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase) { "Tamamlandı", "Tamamlanmadı" };
        private static string Normalize(string? s) => (s ?? "").Trim();
        private static bool IsValidTaskForAi(TaskItem t) {
            var status = Normalize(t.Status);
            var difficulty = Normalize(t.Difficulty);
            return !string.IsNullOrWhiteSpace(difficulty) && AllowedStatuses.Contains(status);
        }
    }
}