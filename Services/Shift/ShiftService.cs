using Microsoft.EntityFrameworkCore;
using Personelim.Data;
using Personelim.DTOs.Shift;
using Personelim.Helpers;
using Personelim.Models;
using Personelim.Models.Enums;

namespace Personelim.Services.Shift
{
    public class ShiftService : IShiftService
    {
        private readonly AppDbContext _context;

        public ShiftService(AppDbContext context)
        {
            _context = context;
        }

        // 1. MESAİ EKLEME (POST)
        public async Task<ServiceResponse<ShiftResponse>> CreateShiftAsync(Guid currentUserId, CreateShiftRequest request)
        {
            try
            {
                // A) Yetki ve Üyelik Kontrolü
                // İşlemi yapan kişi (currentUserId), bu işletmenin sahibi mi?
                var isOwner = await _context.BusinessMembers.AnyAsync(bm =>
                    bm.UserId == currentUserId &&
                    bm.BusinessId == request.BusinessId &&
                    bm.Role == UserRole.Owner && bm.IsActive);

                if (!isOwner) return ServiceResponse<ShiftResponse>.ErrorResult("Mesai ekleme yetkiniz yok.");

                // Mesai yazılacak kişi bu işletmede çalışıyor mu?
                var isEmployee = await _context.BusinessMembers.AnyAsync(bm =>
                    bm.UserId == request.UserId &&
                    bm.BusinessId == request.BusinessId && bm.IsActive);

                if (!isEmployee) return ServiceResponse<ShiftResponse>.ErrorResult("Seçilen personel bu işletmede aktif değil.");

                // B) Tarih Kontrolü
                if (request.EndTime <= request.StartTime)
                    return ServiceResponse<ShiftResponse>.ErrorResult("Bitiş saati başlangıçtan ileri olmalıdır.");

                // C) Kayıt
                var newShift = new Models.Shift
                {
                    BusinessId = request.BusinessId,
                    UserId = request.UserId,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Shifts.AddAsync(newShift);
                await _context.SaveChangesAsync();

                // D) Response Dönüşü için kullanıcı adını çekelim
                var user = await _context.Users.FindAsync(request.UserId);
                
                return ServiceResponse<ShiftResponse>.SuccessResult(new ShiftResponse
                {
                    Id = newShift.Id,
                    UserId = newShift.UserId,
                    UserName = user != null ? $"{user.FirstName} {user.LastName}" : "Bilinmiyor",
                    StartTime = newShift.StartTime,
                    EndTime = newShift.EndTime,
                    TotalHours = (newShift.EndTime - newShift.StartTime).TotalHours,
                    CreatedAt = newShift.CreatedAt
                }, "Mesai başarıyla eklendi.");
            }
            catch (Exception ex)
            {
                return ServiceResponse<ShiftResponse>.ErrorResult("Mesai eklenirken hata oluştu: " + ex.Message);
            }
        }

        // 2. MESAİLERİ LİSTELEME (GET)
        public async Task<ServiceResponse<List<ShiftResponse>>> GetShiftsByBusinessAsync(Guid currentUserId, Guid businessId)
        {
            try
            {
                // Yetki kontrolü: Sadece o işletmenin sahibi veya personeli görebilir
                var isMember = await _context.BusinessMembers.AnyAsync(bm =>
                    bm.UserId == currentUserId &&
                    bm.BusinessId == businessId && bm.IsActive);

                if (!isMember) return ServiceResponse<List<ShiftResponse>>.ErrorResult("Bu işletmenin mesailerini görme yetkiniz yok.");

                // Verileri çek
                var shifts = await _context.Shifts
                    .Include(s => s.User)
                    .Where(s => s.BusinessId == businessId)
                    .OrderByDescending(s => s.StartTime) // En yeni mesai en üstte
                    .ToListAsync();

                var responseList = shifts.Select(s => new ShiftResponse
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    UserName = s.User != null ? $"{s.User.FirstName} {s.User.LastName}" : "Bilinmiyor",
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    TotalHours = Math.Round((s.EndTime - s.StartTime).TotalHours, 2), // Virgülden sonra 2 hane
                    CreatedAt = s.CreatedAt
                }).ToList();

                return ServiceResponse<List<ShiftResponse>>.SuccessResult(responseList);
            }
            catch (Exception ex)
            {
                return ServiceResponse<List<ShiftResponse>>.ErrorResult("Mesailer listelenirken hata oluştu: " + ex.Message);
            }
        }
    }
}