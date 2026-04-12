using Microsoft.EntityFrameworkCore;
using Personelim.Data;
using Personelim.DTOs.Business;
using Personelim.Helpers;

namespace Personelim.Validators
{
    public interface IBusinessValidator
    {
        Task<ServiceResponse<bool>> ValidateCreateBusinessAsync(CreateBusinessRequestDto requestDto);
    }

    public class BusinessValidator : IBusinessValidator
    {
        private readonly AppDbContext _context;
        
        // Kısaltılmış/temel operatör kod listesi (gerekirse genişlet)
        private readonly HashSet<string> _validOperatorCodes = new()
        {
            "500","501","505","506","507",
            "530","531","532","533","534","535","536","537","538","539",
            "540","541","542","543","544","545","546","547","548","549",
            "550","551","552","553","554","555","556","557","558","559"
        };

        public BusinessValidator(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResponse<bool>> ValidateCreateBusinessAsync(CreateBusinessRequestDto requestDto)
        {
            if (requestDto == null)
                return ServiceResponse<bool>.ErrorResult("Geçersiz istek");

            // --- İşletme adı ---
            if (string.IsNullOrWhiteSpace(requestDto.BusinessName))
                return ServiceResponse<bool>.ErrorResult("İşletme adı zorunludur");

            var name = requestDto.BusinessName.Trim();
            if (name.Length < 2) return ServiceResponse<bool>.ErrorResult("İşletme adı en az 2 karakter olmalıdır");
            if (name.Length > 200) return ServiceResponse<bool>.ErrorResult("İşletme adı en fazla 200 karakter olabilir");

            var existingBusinessByName = await _context.Businesses
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Name.ToLower() == name.ToLower() && b.IsActive);

            if (existingBusinessByName != null)
                return ServiceResponse<bool>.ErrorResult("Bu isimde bir işletme zaten kayıtlı");

            // --- Adres ---
            if (string.IsNullOrWhiteSpace(requestDto.Address))
                return ServiceResponse<bool>.ErrorResult("Adres boş bırakılamaz");

            var address = requestDto.Address.Trim();
            if (address.Length < 10) return ServiceResponse<bool>.ErrorResult("Adres en az 10 karakter olmalıdır");
            if (address.Length > 500) return ServiceResponse<bool>.ErrorResult("Adres en fazla 500 karakter olabilir");

            // --- Telefon ---
            if (string.IsNullOrWhiteSpace(requestDto.PhoneNumber))
                return ServiceResponse<bool>.ErrorResult("Telefon numarası boş bırakılamaz");

            var cleanPhoneNumber = new string(requestDto.PhoneNumber.Where(char.IsDigit).ToArray());

            if (cleanPhoneNumber.Length != 10)
                return ServiceResponse<bool>.ErrorResult("Telefon numarası 10 haneli olmalıdır");

            if (!cleanPhoneNumber.StartsWith("5"))
                return ServiceResponse<bool>.ErrorResult("Telefon numarası 5 ile başlamalıdır");

            var operatorCode = cleanPhoneNumber.Substring(0, 3);
            if (!_validOperatorCodes.Contains(operatorCode))
                return ServiceResponse<bool>.ErrorResult("Geçersiz operatör kodu. Lütfen geçerli bir Türkiye telefon numarası giriniz");

            // Benzersizlik kontrolü (DB'de telefon null olabilir, bu yüzden null kontrolü ekliyoruz)
            var existingPhones = await _context.Businesses
                .AsNoTracking()
                .Where(b => b.IsActive && !string.IsNullOrEmpty(b.PhoneNumber))
                .Select(b => b.PhoneNumber)
                .ToListAsync();

            foreach (var phone in existingPhones)
            {
                var dbClean = new string(phone.Where(char.IsDigit).ToArray());
                if (dbClean == cleanPhoneNumber)
                    return ServiceResponse<bool>.ErrorResult("Bu telefon numarası başka bir işletme tarafından kullanılıyor");
            }

            // --- Province / District kontrolü ---
            // Buradaki hata genelde tip uyuşmazlığından kaynaklanır. FindAsync kullanarak tipi EF'e bırakıyoruz.
            var province = await _context.Provinces.FindAsync(requestDto.ProvinceId);
            if (province == null)
                return ServiceResponse<bool>.ErrorResult("Geçersiz şehir seçimi");

            var district = await _context.Districts.FindAsync(requestDto.DistrictId);
            if (district == null)
                return ServiceResponse<bool>.ErrorResult("Geçersiz ilçe seçimi");

            // District'in province aitliğini kontrol et
            // (District entity içinde ProvinceId alanı olduğunu varsayıyoruz)
            if (!Equals(district.ProvinceId, province.Id))
                return ServiceResponse<bool>.ErrorResult("Seçilen ilçe, seçilen şehre ait değil");

            return ServiceResponse<bool>.SuccessResult(true);
        }
    }
}
