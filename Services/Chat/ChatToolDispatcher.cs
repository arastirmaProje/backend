using System.Text.Json;
using Personelim.DTOs.Leave;
using Personelim.DTOs.Performance;
using Personelim.DTOs.Task;
using Personelim.Helpers;
using Personelim.Services.Leave;
using Personelim.Services.Performance;
using Personelim.Services.Task;

namespace Personelim.Services.Chat
{
    public interface IChatToolDispatcher
    {
        System.Threading.Tasks.Task<object> DispatchAsync(string functionName, JsonElement arguments, ChatToolContext ctx);
    }

    public class ChatToolContext
    {
        public Guid UserId { get; set; }
        public Guid? BusinessId { get; set; }
        public Guid? DepartmanId { get; set; }
    }

    public class ChatToolDispatcher : IChatToolDispatcher
    {
        private readonly IPerformanceService _perf;
        private readonly ITaskService _task;
        private readonly ILeaveService _leave;

        public ChatToolDispatcher(IPerformanceService perf, ITaskService task, ILeaveService leave)
        {
            _perf = perf;
            _task = task;
            _leave = leave;
        }

        public async System.Threading.Tasks.Task<object> DispatchAsync(string functionName, JsonElement args, ChatToolContext ctx)
        {
            try
            {
                return functionName switch
                {
                    "performans_sorgula" => await PerformansSorgulaAsync(args, ctx),
                    "gorev_listele" => await GorevListeleAsync(ctx),
                    "izin_talebi_olustur" => await IzinTalebiOlusturAsync(args, ctx),
                    "performans_gecmisi" => await PerformansGecmisiAsync(ctx),
                    "departman_performans" => await DepartmanPerformansAsync(args, ctx),
                    "calisan_karsilastir" => await CalisanKarsilastirAsync(args, ctx),
                    "gorev_olustur" => await GorevOlusturAsync(args, ctx),
                    "departman_raporu_iste" => await DepartmanPerformansAsync(args, ctx),
                    _ => new { hata = $"Bilinmeyen tool: {functionName}" }
                };
            }
            catch (Exception ex)
            {
                return new { hata = ex.Message };
            }
        }

        private async System.Threading.Tasks.Task<object> PerformansSorgulaAsync(JsonElement args, ChatToolContext ctx)
        {
            if (!ctx.BusinessId.HasValue) return new { hata = "İşletme bağlamı yok." };

            var req = new PerformanceQueryRequestDto
            {
                BusinessId = ctx.BusinessId.Value,
                EmployeeUserId = ctx.UserId,
                StartDate = ParseDate(args, "baslangic_tarihi"),
                EndDate = ParseDate(args, "bitis_tarihi", endOfDay: true)
            };
            var result = await _perf.QueryAsync(ctx.UserId, req);
            return result.Success ? (object)result.Data! : new { hata = result.Message };
        }

        private async System.Threading.Tasks.Task<object> GorevListeleAsync(ChatToolContext ctx)
        {
            var result = await _task.GetMyTasksAsync(ctx.UserId);
            return result.Success ? (object)result.Data! : new { hata = result.Message };
        }

        private async System.Threading.Tasks.Task<object> IzinTalebiOlusturAsync(JsonElement args, ChatToolContext ctx)
        {
            if (!ctx.BusinessId.HasValue) return new { hata = "İşletme bağlamı yok." };

            var req = new CreateLeaveRequestDto
            {
                BusinessId = ctx.BusinessId.Value,
                Title = TryGetString(args, "neden") ?? "İzin talebi",
                Description = TryGetString(args, "neden"),
                StartDate = ParseDate(args, "baslangic"),
                EndDate = ParseDate(args, "bitis", endOfDay: true)
            };
            var result = await _leave.CreateLeaveRequestAsync(ctx.UserId, req);
            return result.Success ? (object)result.Data! : new { hata = result.Message };
        }

        private async System.Threading.Tasks.Task<object> PerformansGecmisiAsync(ChatToolContext ctx)
        {
            if (!ctx.BusinessId.HasValue) return new { hata = "İşletme bağlamı yok." };

            var result = await _perf.GetReportsByEmployeeAsync(ctx.UserId, ctx.BusinessId.Value, ctx.UserId);
            return result.Success ? (object)result.Data! : new { hata = result.Message };
        }

        private async System.Threading.Tasks.Task<object> DepartmanPerformansAsync(JsonElement args, ChatToolContext ctx)
        {
            if (!ctx.BusinessId.HasValue || !ctx.DepartmanId.HasValue)
                return new { hata = "İşletme veya departman bağlamı yok." };

            var req = new DepartmanPerformanceQueryRequestDto
            {
                BusinessId = ctx.BusinessId.Value,
                DepartmentId = ctx.DepartmanId.Value,
                StartDate = ParseDate(args, "baslangic_tarihi"),
                EndDate = ParseDate(args, "bitis_tarihi", endOfDay: true)
            };
            var result = await _perf.QueryDepartmentAsync(ctx.UserId, req);
            return result.Success ? (object)result.Data! : new { hata = result.Message };
        }

        private async System.Threading.Tasks.Task<object> CalisanKarsilastirAsync(JsonElement args, ChatToolContext ctx)
        {
            if (!ctx.BusinessId.HasValue) return new { hata = "İşletme bağlamı yok." };

            var req = new PerformanceBulkQueryRequest
            {
                BusinessId = ctx.BusinessId.Value,
                StartDate = ParseDate(args, "baslangic_tarihi"),
                EndDate = ParseDate(args, "bitis_tarihi", endOfDay: true)
            };
            var result = await _perf.QueryBulkScoresAsync(ctx.UserId, req);
            return result.Success ? (object)result.Data! : new { hata = result.Message };
        }

        private async System.Threading.Tasks.Task<object> GorevOlusturAsync(JsonElement args, ChatToolContext ctx)
        {
            if (!ctx.BusinessId.HasValue) return new { hata = "İşletme bağlamı yok." };

            var atananId = TryGetString(args, "atanan_calisan_id");
            if (string.IsNullOrEmpty(atananId) || !Guid.TryParse(atananId, out var assignedTo))
                return new { hata = "Atanan çalışan ID'si geçersiz." };

            var req = new CreateTaskRequestDto
            {
                BusinessId = ctx.BusinessId.Value,
                AssignedToUserId = assignedTo,
                Title = TryGetString(args, "baslik") ?? "Görev",
                Description = TryGetString(args, "aciklama") ?? "",
                StartDate = ParseDate(args, "baslangic_tarihi"),
                EndDate = ParseDate(args, "bitis_tarihi", endOfDay: true)
            };
            var result = await _task.CreateTaskAsync(ctx.UserId, req);
            return result.Success ? (object)result.Data! : new { hata = result.Message };
        }

        // ── helpers ─────────────────────────────────────────────────────────

        private static string? TryGetString(JsonElement el, string prop)
        {
            if (el.ValueKind != JsonValueKind.Object) return null;
            if (!el.TryGetProperty(prop, out var v)) return null;
            return v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        }

        private static DateTime ParseDate(JsonElement el, string prop, bool endOfDay = false)
        {
            var s = TryGetString(el, prop);
            if (string.IsNullOrEmpty(s)) return DateTime.UtcNow.Date;
            if (DateTime.TryParse(s, out var dt))
                return endOfDay ? dt.Date.AddDays(1).AddTicks(-1) : dt.Date;
            return DateTime.UtcNow.Date;
        }
    }
}
