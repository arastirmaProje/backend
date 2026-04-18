using Personelim.Models.Enums;

namespace Personelim.Helpers
{
    public record JobTitle(int Id, string Name, UserRole Role, string Category);

    public static class JobTitles
    {
        public static readonly IReadOnlyList<JobTitle> All = new[]
        {
            // ── Genel Yönetim ───────────────────────────────────────────────────────
            new JobTitle(1,  "İşletme Sahibi",                UserRole.Owner,    "Genel Yönetim"),
            new JobTitle(2,  "Genel Müdür",                   UserRole.CEO,      "Genel Yönetim"),
            new JobTitle(3,  "COO",                           UserRole.CEO,      "Genel Yönetim"),
            new JobTitle(4,  "CFO",                           UserRole.CEO,      "Genel Yönetim"),
            new JobTitle(5,  "CMO",                           UserRole.CEO,      "Genel Yönetim"),
            new JobTitle(6,  "CHRO",                          UserRole.CEO,      "Genel Yönetim"),

            // ── Yazılım & Teknoloji ─────────────────────────────────────────────────
            new JobTitle(7,  "CTO",                           UserRole.CEO,      "Yazılım & Teknoloji"),
            new JobTitle(8,  "CPO",                           UserRole.CEO,      "Yazılım & Teknoloji"),
            new JobTitle(9,  "Engineering Manager",           UserRole.Manager,  "Yazılım & Teknoloji"),
            new JobTitle(10, "Product Manager",               UserRole.Manager,  "Yazılım & Teknoloji"),
            new JobTitle(11, "Project Manager",               UserRole.Manager,  "Yazılım & Teknoloji"),
            new JobTitle(12, "Agile Coach",                   UserRole.Manager,  "Yazılım & Teknoloji"),
            new JobTitle(13, "Scrum Master",                  UserRole.TeamLead, "Yazılım & Teknoloji"),
            new JobTitle(14, "Product Owner",                 UserRole.TeamLead, "Yazılım & Teknoloji"),
            new JobTitle(15, "Lead Developer",                UserRole.TeamLead, "Yazılım & Teknoloji"),
            new JobTitle(16, "System Analyst",                UserRole.TeamLead, "Yazılım & Teknoloji"),
            new JobTitle(17, "Business Analyst",              UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(18, "Junior Developer",              UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(19, "Mid-Level Developer",           UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(20, "Senior Developer",              UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(21, "Frontend Developer",            UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(22, "Backend Developer",             UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(23, "Full-Stack Developer",          UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(24, "Mobile Developer",              UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(25, "iOS Developer",                 UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(26, "Android Developer",             UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(27, "Embedded Systems Developer",    UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(28, "QA Engineer",                   UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(29, "SDET",                          UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(30, "Test Automation Engineer",      UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(31, "DevOps Engineer",               UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(32, "Site Reliability Engineer",     UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(33, "Cloud Engineer",                UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(34, "System Administrator",          UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(35, "Network Engineer",              UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(36, "Data Analyst",                  UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(37, "Data Engineer",                 UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(38, "Data Scientist",                UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(39, "Machine Learning Engineer",     UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(40, "AI Engineer",                   UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(41, "Security Engineer",             UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(42, "Penetration Tester",            UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(43, "UI/UX Designer",                UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(44, "UX Researcher",                 UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(45, "Product Designer",              UserRole.Employee, "Yazılım & Teknoloji"),
            new JobTitle(46, "Technical Writer",              UserRole.Employee, "Yazılım & Teknoloji"),

            // ── İnsan Kaynakları ────────────────────────────────────────────────────
            new JobTitle(47, "İnsan Kaynakları Müdürü",       UserRole.Manager,  "İnsan Kaynakları"),
            new JobTitle(48, "İK Uzman Lideri",               UserRole.TeamLead, "İnsan Kaynakları"),
            new JobTitle(49, "İnsan Kaynakları Uzmanı",       UserRole.Employee, "İnsan Kaynakları"),
            new JobTitle(50, "İşe Alım Uzmanı",               UserRole.Employee, "İnsan Kaynakları"),
            new JobTitle(51, "Eğitim ve Gelişim Uzmanı",      UserRole.Employee, "İnsan Kaynakları"),
            new JobTitle(52, "Bordro Uzmanı",                 UserRole.Employee, "İnsan Kaynakları"),
            new JobTitle(53, "İK Asistanı",                   UserRole.Employee, "İnsan Kaynakları"),

            // ── Muhasebe & Finans ───────────────────────────────────────────────────
            new JobTitle(54, "Finans Müdürü",                 UserRole.Manager,  "Muhasebe & Finans"),
            new JobTitle(55, "Muhasebe Müdürü",               UserRole.Manager,  "Muhasebe & Finans"),
            new JobTitle(56, "Kıdemli Muhasebeci",            UserRole.TeamLead, "Muhasebe & Finans"),
            new JobTitle(57, "Muhasebeci",                    UserRole.Employee, "Muhasebe & Finans"),
            new JobTitle(58, "Mali Müşavir",                  UserRole.Employee, "Muhasebe & Finans"),
            new JobTitle(59, "Finansal Analist",              UserRole.Employee, "Muhasebe & Finans"),
            new JobTitle(60, "Bütçe Uzmanı",                  UserRole.Employee, "Muhasebe & Finans"),
            new JobTitle(61, "Vergi Uzmanı",                  UserRole.Employee, "Muhasebe & Finans"),
            new JobTitle(62, "Muhasebe Asistanı",             UserRole.Employee, "Muhasebe & Finans"),

            // ── Satış & Pazarlama ───────────────────────────────────────────────────
            new JobTitle(63, "Satış Müdürü",                  UserRole.Manager,  "Satış & Pazarlama"),
            new JobTitle(64, "Pazarlama Müdürü",              UserRole.Manager,  "Satış & Pazarlama"),
            new JobTitle(65, "Satış Takım Lideri",            UserRole.TeamLead, "Satış & Pazarlama"),
            new JobTitle(66, "Dijital Pazarlama Lideri",      UserRole.TeamLead, "Satış & Pazarlama"),
            new JobTitle(67, "Satış Temsilcisi",              UserRole.Employee, "Satış & Pazarlama"),
            new JobTitle(68, "Kıdemli Satış Temsilcisi",      UserRole.Employee, "Satış & Pazarlama"),
            new JobTitle(69, "Pazarlama Uzmanı",              UserRole.Employee, "Satış & Pazarlama"),
            new JobTitle(70, "Dijital Pazarlama Uzmanı",      UserRole.Employee, "Satış & Pazarlama"),
            new JobTitle(71, "Sosyal Medya Uzmanı",           UserRole.Employee, "Satış & Pazarlama"),
            new JobTitle(72, "İçerik Yazarı",                 UserRole.Employee, "Satış & Pazarlama"),
            new JobTitle(73, "SEO Uzmanı",                    UserRole.Employee, "Satış & Pazarlama"),
            new JobTitle(74, "Marka Uzmanı",                  UserRole.Employee, "Satış & Pazarlama"),
            new JobTitle(75, "Grafik Tasarımcı",              UserRole.Employee, "Satış & Pazarlama"),

            // ── Müşteri Hizmetleri ──────────────────────────────────────────────────
            new JobTitle(76, "Müşteri Hizmetleri Müdürü",     UserRole.Manager,  "Müşteri Hizmetleri"),
            new JobTitle(77, "Müşteri Hizmetleri Lideri",     UserRole.TeamLead, "Müşteri Hizmetleri"),
            new JobTitle(78, "Müşteri Temsilcisi",            UserRole.Employee, "Müşteri Hizmetleri"),
            new JobTitle(79, "Teknik Destek Uzmanı",          UserRole.Employee, "Müşteri Hizmetleri"),
            new JobTitle(80, "Çağrı Merkezi Yetkilisi",       UserRole.Employee, "Müşteri Hizmetleri"),
            new JobTitle(81, "Satış Sonrası Hizmet Uzmanı",   UserRole.Employee, "Müşteri Hizmetleri"),

            // ── Operasyon & Lojistik ────────────────────────────────────────────────
            new JobTitle(82, "Operasyon Müdürü",              UserRole.Manager,  "Operasyon & Lojistik"),
            new JobTitle(83, "Lojistik Müdürü",               UserRole.Manager,  "Operasyon & Lojistik"),
            new JobTitle(84, "Depo Sorumlusu",                UserRole.TeamLead, "Operasyon & Lojistik"),
            new JobTitle(85, "Tedarik Zinciri Lideri",        UserRole.TeamLead, "Operasyon & Lojistik"),
            new JobTitle(86, "Lojistik Uzmanı",               UserRole.Employee, "Operasyon & Lojistik"),
            new JobTitle(87, "Tedarik Zinciri Uzmanı",        UserRole.Employee, "Operasyon & Lojistik"),
            new JobTitle(88, "Satın Alma Uzmanı",             UserRole.Employee, "Operasyon & Lojistik"),
            new JobTitle(89, "Depo Görevlisi",                UserRole.Employee, "Operasyon & Lojistik"),
            new JobTitle(90, "Kurye",                         UserRole.Employee, "Operasyon & Lojistik"),
            new JobTitle(91, "Şoför",                         UserRole.Employee, "Operasyon & Lojistik"),
            new JobTitle(92, "Forklift Operatörü",            UserRole.Employee, "Operasyon & Lojistik"),

            // ── Üretim ──────────────────────────────────────────────────────────────
            new JobTitle(93,  "Üretim Müdürü",                UserRole.Manager,  "Üretim"),
            new JobTitle(94,  "Kalite Müdürü",                UserRole.Manager,  "Üretim"),
            new JobTitle(95,  "Üretim Şefi",                  UserRole.TeamLead, "Üretim"),
            new JobTitle(96,  "Kalite Kontrol Şefi",          UserRole.TeamLead, "Üretim"),
            new JobTitle(97,  "Üretim Planlama Uzmanı",       UserRole.Employee, "Üretim"),
            new JobTitle(98,  "Kalite Kontrol Görevlisi",     UserRole.Employee, "Üretim"),
            new JobTitle(99,  "Üretim Operatörü",             UserRole.Employee, "Üretim"),
            new JobTitle(100, "Makine Operatörü",             UserRole.Employee, "Üretim"),
            new JobTitle(101, "Bakım Teknisyeni",             UserRole.Employee, "Üretim"),
            new JobTitle(102, "Elektrik Teknisyeni",          UserRole.Employee, "Üretim"),

            // ── İdari İşler ─────────────────────────────────────────────────────────
            new JobTitle(103, "İdari İşler Müdürü",           UserRole.Manager,  "İdari İşler"),
            new JobTitle(104, "Ofis Yöneticisi",              UserRole.TeamLead, "İdari İşler"),
            new JobTitle(105, "İdari Asistan",                UserRole.Employee, "İdari İşler"),
            new JobTitle(106, "Sekreter",                     UserRole.Employee, "İdari İşler"),
            new JobTitle(107, "Resepsiyonist",                UserRole.Employee, "İdari İşler"),
            new JobTitle(108, "Ofis Asistanı",                UserRole.Employee, "İdari İşler"),

            // ── Hukuk ───────────────────────────────────────────────────────────────
            new JobTitle(109, "Hukuk Müşaviri",               UserRole.Manager,  "Hukuk"),
            new JobTitle(110, "Kıdemli Avukat",               UserRole.TeamLead, "Hukuk"),
            new JobTitle(111, "Avukat",                       UserRole.Employee, "Hukuk"),
            new JobTitle(112, "Hukuk Asistanı",               UserRole.Employee, "Hukuk"),

            // ── Diğer ───────────────────────────────────────────────────────────────
            new JobTitle(113, "Stajyer",                      UserRole.Employee, "Diğer"),
            new JobTitle(114, "Diğer",                        UserRole.Employee, "Diğer"),
        };

        // ── Kategori haritası ────────────────────────────────────────────────────────
        public static readonly IReadOnlyDictionary<int, string> CategoryMap =
            new Dictionary<int, string>
            {
                { 1,  "Genel Yönetim"       },
                { 2,  "Yazılım & Teknoloji"  },
                { 3,  "İnsan Kaynakları"     },
                { 4,  "Muhasebe & Finans"    },
                { 5,  "Satış & Pazarlama"    },
                { 6,  "Müşteri Hizmetleri"   },
                { 7,  "Operasyon & Lojistik" },
                { 8,  "Üretim"               },
                { 9,  "İdari İşler"          },
                { 10, "Hukuk"                },
                { 11, "Diğer"                },
            };

        public static readonly IReadOnlyList<string> Categories =
            All.Select(j => j.Category).Distinct().ToList();

        // ── Unvan arama ──────────────────────────────────────────────────────────────
        public static JobTitle? GetById(int id) =>
            All.FirstOrDefault(j => j.Id == id);

        public static string? GetTitleName(int id) =>
            GetById(id)?.Name;

        public static int GetTitleId(string name) =>
            All.FirstOrDefault(j => j.Name == name)?.Id ?? 0;

        // ── Kategori arama ───────────────────────────────────────────────────────────
        public static string? GetCategoryName(int categoryId) =>
            CategoryMap.TryGetValue(categoryId, out var name) ? name : null;

        public static int GetCategoryId(string categoryName)
        {
            foreach (var kv in CategoryMap)
                if (kv.Value == categoryName) return kv.Key;
            return 0;
        }

        // ── Doğrulama ────────────────────────────────────────────────────────────────
        public static bool IsValidId(int id) =>
            All.Any(j => j.Id == id);

        public static bool IsValid(string? position) =>
            !string.IsNullOrWhiteSpace(position) && All.Any(j => j.Name == position);

        // ── Rol türetme ──────────────────────────────────────────────────────────────
        public static UserRole GetRole(string? position) =>
            All.FirstOrDefault(j => j.Name == position)?.Role ?? UserRole.Employee;

        public static UserRole GetRoleById(int id) =>
            GetById(id)?.Role ?? UserRole.Employee;

        // ── Yetki kontrolleri ────────────────────────────────────────────────────────
        public static IReadOnlyList<string> PositionsWithMinRole(UserRole minRole) =>
            All.Where(j => j.Role >= minRole).Select(j => j.Name).ToList();

        public static IReadOnlyList<string> EffectiveManagementPositions(bool isSubscribed) =>
            isSubscribed ? PositionsWithMinRole(UserRole.Manager) : PositionsWithMinRole(UserRole.Owner);

        // ── Filtreleme ───────────────────────────────────────────────────────────────
        public static IReadOnlyList<JobTitle> GetByCategory(string category) =>
            All.Where(j => j.Category == category).ToList();
    }
}
