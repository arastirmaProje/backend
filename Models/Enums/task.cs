namespace Personelim.Models.Enums
{
    public enum TaskStatus
    {
        Pending = 0,        // Beklemede
        InProgress = 1,     // İşlemde / Devam Ediyor
        Completed = 2,      // Tamamlandı
        Cancelled = 3,      // İptal Edildi
        Overdue = 4         // Süresi Geçti
    }

    public enum TaskDifficulty
    {
        Easy = 0,           // Kolay
        Medium = 1,         // Orta
        Hard = 2,           // Zor
        Expert = 3          // Uzmanlık Gerektiren
    }
}