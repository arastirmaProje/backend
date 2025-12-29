using Microsoft.EntityFrameworkCore;
using Personelim.Data;
using Personelim.DTOs.Performance;
using Personelim.Helpers;
using Personelim.Models;
using Personelim.Models.Enums;
using System.Net.Http.Json;
using System.Text.Json;

namespace Personelim.Services.Performance
{
    public class PerformanceService : IPerformanceService
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _http;

        public PerformanceService(AppDbContext context, IHttpClientFactory factory)
        {
            _context = context;
            _http = factory.CreateClient("AiPerformance");
        }

        public async Task<ServiceResponse<AiPerformanceResponse>> QueryAsync(Guid currentUserId, PerformanceQueryRequest request)
        {
            try
            {
                if (request.BusinessId == Guid.Empty || request.EmployeeUserId == Guid.Empty)
                    return ServiceResponse<AiPerformanceResponse>.ErrorResult("İşletme yada kullanıcı bulunamadı");
                if (request.EndDate.Date < request.StartDate.Date)
                    return ServiceResponse<AiPerformanceResponse>.ErrorResult("Bitiş tarihi başlangıçtan küçük olamaz.");

                var isEmployee = await _context.BusinessMembers.AnyAsync(bm =>
                    bm.UserId == request.EmployeeUserId &&
                    bm.BusinessId == request.BusinessId &&
                    bm.IsActive);
                if (!isEmployee)
                    return ServiceResponse<AiPerformanceResponse>.ErrorResult("Çalışan bu işletmede aktif değil.");

                var employee = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.EmployeeUserId && u.IsActive);
                if (employee == null)
                    return ServiceResponse<AiPerformanceResponse>.ErrorResult("Çalışan bulunamadı.");

                var start = request.StartDate.Date;
                var end = request.EndDate.Date.AddDays(1).AddTicks(-1);

                var tasks = await _context.TaskItems
                    .Where(t =>
                        t.BusinessId == request.BusinessId &&
                        t.AssignedToUserId == request.EmployeeUserId &&
                        t.StartDate <= end &&
                        t.EndDate >= start
                    )
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                // Sadece zorluk derecesi olan ve Tamamlandı/Tamamlanmadı durumundaki görevleri alıyoruz
                var validTasks = tasks.Where(IsValidTaskForAi).ToList();

                int completed = validTasks.Count(t =>
                    string.Equals(Normalize(t.Status), "Tamamlandı", StringComparison.OrdinalIgnoreCase));
                int notCompleted = validTasks.Count(t =>
                    string.Equals(Normalize(t.Status), "Tamamlanmadı", StringComparison.OrdinalIgnoreCase));

                var realizedHoursDec = await _context.Shifts
                    .Where(s =>
                        s.BusinessId == request.BusinessId &&
                        s.UserId == request.EmployeeUserId &&
                        s.StartTime >= start &&
                        s.EndTime <= end
                    )
                    .SumAsync(s => (decimal?)s.TotalHours) ?? 0m;
                double realizedHours = (double)realizedHoursDec;

                int dayCount = (request.EndDate.Date - request.StartDate.Date).Days + 1;
                double targetHours = dayCount * 8;

                int usedLeaveDays = await _context.MemberLeaves
                    .Include(l => l.BusinessMember)
                    .Where(l =>
                        l.BusinessMember.BusinessId == request.BusinessId &&
                        l.BusinessMember.UserId == request.EmployeeUserId &&
                        l.Status == LeaveStatus.Approved &&
                        l.StartDate <= end &&
                        l.EndDate >= start
                    )
                    .SumAsync(l => (int?)l.DayCount) ?? 0;

                var aiPayload = new AiPerformanceRequest
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
                    return ServiceResponse<AiPerformanceResponse>.ErrorResult("AI servisi hata döndü.", body);
                }
                var aiResult = await resp.Content.ReadFromJsonAsync<AiPerformanceResponse>();
                if (aiResult == null)
                    return ServiceResponse<AiPerformanceResponse>.ErrorResult("AI cevabı okunamadı.");

                var report = new PerformanceReport
                {
                    Id = Guid.NewGuid(),
                    BusinessId = request.BusinessId,
                    EmployeeUserId = request.EmployeeUserId,
                    RequestedByUserId = currentUserId,
                    PeriodStart = request.StartDate.Date,
                    PeriodEnd = request.EndDate.Date,
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
                return ServiceResponse<AiPerformanceResponse>.SuccessResult(aiResult);
            }
            catch (Exception ex)
            {
                return ServiceResponse<AiPerformanceResponse>.ErrorResult("Performans raporu oluşturulamadı.", ex.Message);
            }
        }

        public async Task<ServiceResponse<List<PerformanceReportListItem>>> GetReportsByEmployeeAsync(Guid currentUserId, Guid businessId, Guid employeeUserId)
        {
            try
            {
                var list = await _context.PerformanceReports
                    .Where(r => r.BusinessId == businessId && r.EmployeeUserId == employeeUserId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new PerformanceReportListItem
                    {
                        Id = r.Id,
                        BusinessId = r.BusinessId,
                        EmployeeUserId = r.EmployeeUserId,
                        PeriodStart = r.PeriodStart,
                        PeriodEnd = r.PeriodEnd,
                        PerformanceScore = r.PerformanceScore,
                        CreatedAt = r.CreatedAt
                    })
                    .ToListAsync();
                return ServiceResponse<List<PerformanceReportListItem>>.SuccessResult(list);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<PerformanceReportListItem>>.ErrorResult("Raporlar alınamadı.", ex.Message);
            }
        }

        public async Task<ServiceResponse<AiPerformanceResponse>> GetReportByIdAsync(Guid currentUserId, Guid reportId)
        {
            try
            {
                var report = await _context.PerformanceReports.FirstOrDefaultAsync(r => r.Id == reportId);
                if (report == null)
                    return ServiceResponse<AiPerformanceResponse>.ErrorResult("Rapor bulunamadı.");

                var ai = JsonSerializer.Deserialize<AiPerformanceResponse>(report.AiResponseJson);
                if (ai == null)
                    return ServiceResponse<AiPerformanceResponse>.ErrorResult("Kayıtlı rapor okunamadı.");
                return ServiceResponse<AiPerformanceResponse>.SuccessResult(ai);
            }
            catch (Exception ex)
            {
                return ServiceResponse<AiPerformanceResponse>.ErrorResult("Rapor alınamadı.", ex.Message);
            }
        }

        public async Task<ServiceResponse<AiPerformanceBulkScoreResponse>> QueryBulkScoresAsync(
            Guid currentUserId,
            PerformanceBulkQueryRequest request)
        {
            try
            {
                if (request.BusinessId == Guid.Empty)
                    return ServiceResponse<AiPerformanceBulkScoreResponse>.ErrorResult("BusinessId zorunludur.");
                if (request.EndDate.Date < request.StartDate.Date)
                    return ServiceResponse<AiPerformanceBulkScoreResponse>.ErrorResult("Bitiş tarihi başlangıçtan küçük olamaz.");
                
                var start = request.StartDate.Date;
                var end = request.EndDate.Date.AddDays(1).AddTicks(-1);

                var activeMemberUserIds = await _context.BusinessMembers
                    .Where(bm => bm.BusinessId == request.BusinessId && bm.IsActive)
                    .Select(bm => bm.UserId)
                    .Distinct()
                    .ToListAsync();

                var response = new AiPerformanceBulkScoreResponse
                {
                    ToplamCalisan = activeMemberUserIds.Count
                };

                if (activeMemberUserIds.Count == 0)
                    return ServiceResponse<AiPerformanceBulkScoreResponse>.SuccessResult(response);

                var users = await _context.Users
                    .Where(u => activeMemberUserIds.Contains(u.Id) && u.IsActive)
                    .Select(u => new { u.Id, u.FirstName, u.LastName })
                    .ToListAsync();

                var fullNameByUserId = users.ToDictionary(
                    x => x.Id,
                    x => $"{x.FirstName} {x.LastName}".Trim());
                
                var allTasks = await _context.TaskItems
                    .Where(t =>
                        t.BusinessId == request.BusinessId &&
                        activeMemberUserIds.Contains(t.AssignedToUserId) &&
                        t.StartDate <= end &&
                        t.EndDate >= start
                    )
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                var tasksByUser = allTasks
                    .GroupBy(t => t.AssignedToUserId)
                    .ToDictionary(g => g.Key, g => g.ToList());
                
                var shiftSums = await _context.Shifts
                    .Where(s =>
                        s.BusinessId == request.BusinessId &&
                        activeMemberUserIds.Contains(s.UserId) &&
                        s.StartTime >= start &&
                        s.EndTime <= end
                    )
                    .GroupBy(s => s.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        Total = g.Sum(x => (decimal?)x.TotalHours) ?? 0m
                    })
                    .ToListAsync();
                var realizedHoursByUser = shiftSums.ToDictionary(x => x.UserId, x => (double)x.Total);
                
                var leaveSums = await _context.MemberLeaves
                    .Include(l => l.BusinessMember)
                    .Where(l =>
                        l.BusinessMember.BusinessId == request.BusinessId &&
                        activeMemberUserIds.Contains(l.BusinessMember.UserId) &&
                        l.Status == LeaveStatus.Approved &&
                        l.StartDate <= end &&
                        l.EndDate >= start
                    )
                    .GroupBy(l => l.BusinessMember.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        Days = g.Sum(x => (int?)x.DayCount) ?? 0
                    })
                    .ToListAsync();
                var usedLeaveDaysByUser = leaveSums.ToDictionary(x => x.UserId, x => x.Days);

                int dayCount = (request.EndDate.Date - request.StartDate.Date).Days + 1;
                double targetHours = dayCount * 8;

                foreach (var employeeUserId in activeMemberUserIds)
                {
                    var fullName = fullNameByUserId.TryGetValue(employeeUserId, out var fn) ? fn : "";
                    tasksByUser.TryGetValue(employeeUserId, out var empTasks);
                    empTasks ??= new List<TaskItem>();

                    // Sadece zorluk derecesi olan ve durumu uygun (Tamamlandı/Tamamlanmadı) olanları filtreliyoruz
                    var validTasks = empTasks.Where(IsValidTaskForAi).ToList();

                    int completed = validTasks.Count(t => string.Equals(Normalize(t.Status), "Tamamlandı", StringComparison.OrdinalIgnoreCase));
                    int notCompleted = validTasks.Count(t => string.Equals(Normalize(t.Status), "Tamamlanmadı", StringComparison.OrdinalIgnoreCase));
                    
                    realizedHoursByUser.TryGetValue(employeeUserId, out var realizedHours);
                    usedLeaveDaysByUser.TryGetValue(employeeUserId, out var usedLeaveDays);

                    var aiPayload = new AiPerformanceRequest
                    {
                        CalisanId = employeeUserId,
                        AdSoyad = fullName,
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
                            Durum = t.Status!,
                            BaslangicTarihi = t.StartDate,
                            BitisTarihi = t.EndDate,
                            Aciklama = t.Description,
                            GeriDonut = t.Thoughts
                        }).ToList()
                    };

                    int scoreToReturn = 0;
                    try
                    {
                        var respAi = await _http.PostAsJsonAsync("", aiPayload);
                        if (respAi.IsSuccessStatusCode)
                        {
                            var aiResult = await respAi.Content.ReadFromJsonAsync<AiPerformanceResponse>();
                            if (aiResult != null)
                            {
                                scoreToReturn = (int)Math.Round(aiResult.PerformansSkoru);
                                
                                var report = new PerformanceReport
                                {
                                    Id = Guid.NewGuid(),
                                    BusinessId = request.BusinessId,
                                    EmployeeUserId = employeeUserId,
                                    RequestedByUserId = currentUserId,
                                    PeriodStart = request.StartDate.Date,
                                    PeriodEnd = request.EndDate.Date,
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
                            }
                        }
                    }
                    catch
                    {
                        // Loglanabilir, devam ediliyor.
                    }

                    response.Skorlar.Add(new AiPerformanceBulkScoreItem
                    {
                        CalisanId = employeeUserId,
                        AdSoyad = fullName,
                        PerformansSkoru = scoreToReturn
                    });
                }

                await _context.SaveChangesAsync();
                return ServiceResponse<AiPerformanceBulkScoreResponse>.SuccessResult(response);
            }
            catch (Exception ex)
            {
                return ServiceResponse<AiPerformanceBulkScoreResponse>.ErrorResult("Toplu performans skoru oluşturulamadı.", ex.Message);
            }
        }

        private static readonly HashSet<string> AllowedStatuses =
            new(StringComparer.OrdinalIgnoreCase) { "Tamamlandı", "Tamamlanmadı" };

        private static string Normalize(string? s) => (s ?? "").Trim();

        private static bool IsValidTaskForAi(TaskItem t)
        {
            var status = Normalize(t.Status);
            var difficulty = Normalize(t.Difficulty);
            return !string.IsNullOrWhiteSpace(difficulty)
                   && AllowedStatuses.Contains(status);
        }
    }
}