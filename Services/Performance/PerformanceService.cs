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
                    return ServiceResponse<AiPerformanceResponse>.ErrorResult("BusinessId ve EmployeeUserId zorunludur.");

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

                int completed = tasks.Count(t =>
                    string.Equals((t.Status ?? "").Trim(), "Tamamlandı", StringComparison.OrdinalIgnoreCase));

                // “Tamamlandı dışındaki her şeyi tamamlanmayan
                int notCompleted = tasks.Count - completed;

                // Mesai toplamı
                var realizedHoursDec = await _context.Shifts
                    .Where(s =>
                        s.BusinessId == request.BusinessId &&
                        s.UserId == request.EmployeeUserId &&
                        s.StartTime >= start &&
                        s.EndTime <= end
                    )
                    .SumAsync(s => (decimal?)s.TotalHours) ?? 0m;

                double realizedHours = (double)realizedHoursDec;

                // Hedef: gün * 8
                int dayCount = (request.EndDate.Date - request.StartDate.Date).Days + 1;
                double targetHours = dayCount * 8;

                // İzin: Approved izinlerin DayCount toplamı (aralıkla çakışan)
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
                    Gorevler = tasks.Select(t => new AiTaskDto
                    {
                        Id = t.Id,
                        GorevAdi = t.Title ?? "",
                        ZorlukSeviyesi = string.IsNullOrWhiteSpace(t.Difficulty) ? "Belirtilmedi" : t.Difficulty!,
                        Durum = string.IsNullOrWhiteSpace(t.Status) ? "Beklemede" : t.Status!,
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
                var isOwner = await _context.BusinessMembers.AnyAsync(bm =>
                    bm.UserId == currentUserId &&
                    bm.BusinessId == businessId &&
                    bm.Role == UserRole.Owner &&
                    bm.IsActive);

                if (!isOwner)
                    return ServiceResponse<List<PerformanceReportListItem>>.ErrorResult("Yetkiniz yok.");

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

                var isOwner = await _context.BusinessMembers.AnyAsync(bm =>
                    bm.UserId == currentUserId &&
                    bm.BusinessId == report.BusinessId &&
                    bm.Role == UserRole.Owner &&
                    bm.IsActive);

                if (!isOwner)
                    return ServiceResponse<AiPerformanceResponse>.ErrorResult("Yetkiniz yok.");
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
    }
}