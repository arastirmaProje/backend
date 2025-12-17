using Microsoft.EntityFrameworkCore;
using Personelim.Data;
using Personelim.DTOs.Shift;
using Personelim.Helpers;
using Personelim.Models;

namespace Personelim.Services.Shift
{
    public class ShiftService : IShiftService
    {
        private readonly AppDbContext _context;

        public ShiftService(AppDbContext context)
        {
            _context = context;
        }

        // =======================================================
        // 1. TEK ENDPOINT (TOGGLE): Başlat / Bitir Mantığı
        // =======================================================
        public async Task<ServiceResponse<ShiftResponse>> ToggleShiftAsync(Guid userId, Guid businessId)
        {
            try
            {
                // Kullanıcının henüz BİTMEMİŞ (EndTime == null) bir mesaisi var mı?
                var activeShift = await _context.Shifts
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.BusinessId == businessId && s.EndTime == null);

                // SENARYO A: Aktif mesai VAR -> O zaman BİTİRİYORUZ.
                if (activeShift != null)
                {
                    activeShift.EndTime = DateTime.UtcNow; // Şu anı bas
                    await _context.SaveChangesAsync();

                    // Bitmiş halini hesaplayıp dön
                    // .Value güvenlidir çünkü yukarıda atama yaptık.
                    double hours = (activeShift.EndTime.Value - activeShift.StartTime).TotalHours;

                    return ServiceResponse<ShiftResponse>.SuccessResult(new ShiftResponse
                    {
                        Id = activeShift.Id,
                        UserId = userId,
                        StartTime = activeShift.StartTime,
                        EndTime = activeShift.EndTime, // DTO'da DateTime? olduğu için sorun çıkmaz
                        TotalHours = Math.Round(hours, 2)
                    }, "Mesai sonlandırıldı.");
                }

                // SENARYO B: Aktif mesai YOK -> O zaman BAŞLATIYORUZ.
                else
                {
                    var newShift = new Models.Shift
                    {
                        BusinessId = businessId,
                        UserId = userId,
                        StartTime = DateTime.UtcNow,
                        EndTime = null, // Henüz bitmedi
                        CreatedAt = DateTime.UtcNow
                    };

                    await _context.Shifts.AddAsync(newShift);
                    await _context.SaveChangesAsync();

                    return ServiceResponse<ShiftResponse>.SuccessResult(new ShiftResponse
                    {
                        Id = newShift.Id,
                        UserId = userId,
                        StartTime = newShift.StartTime,
                        EndTime = null, // DTO'da DateTime? olduğu için null kabul eder
                        TotalHours = 0 // Yeni başladı
                    }, "Kronometre başlatıldı.");
                }
            }
            catch (Exception ex)
            {
                return ServiceResponse<ShiftResponse>.ErrorResult("İşlem hatası: " + ex.Message);
            }
        }

        // =======================================================
        // 2. LİSTELEME VE HESAPLAMA (GET)
        // =======================================================
        public async Task<ServiceResponse<List<ShiftResponse>>> GetShiftsByBusinessAsync(Guid currentUserId, Guid businessId)
        {
            try
            {
                // Önce veriyi veritabanından çekelim
                var shifts = await _context.Shifts
                    .Include(s => s.User)
                    .Where(s => s.BusinessId == businessId)
                    .OrderByDescending(s => s.StartTime) 
                    .ToListAsync();

                // Bellek üzerinde hesaplama (Select içi)
                var responseList = shifts.Select(s =>
                {
                    double calculatedHours = 0;

                    if (s.EndTime != null)
                    {
                        // Mesai bitmişse
                        calculatedHours = (s.EndTime.Value - s.StartTime).TotalHours;
                    }
                    else
                    {
                        // Mesai sürüyorsa -> Şu anki zamana göre hesapla
                        calculatedHours = (DateTime.UtcNow - s.StartTime).TotalHours;
                    }

                    return new ShiftResponse
                    {
                        Id = s.Id,
                        UserId = s.UserId,
                        UserName = s.User != null ? $"{s.User.FirstName} {s.User.LastName}" : "Bilinmiyor",
                        StartTime = s.StartTime,
                        EndTime = s.EndTime, // Burası DateTime? olduğu için direkt atanabilir
                        TotalHours = Math.Round(calculatedHours, 2)
                    };
                }).ToList();

                return ServiceResponse<List<ShiftResponse>>.SuccessResult(responseList);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<ShiftResponse>>.ErrorResult("Hata: " + ex.Message);
            }
        }
    }
}