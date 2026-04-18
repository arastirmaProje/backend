namespace Personelim.Models.Enums
{
    /// <summary>
    /// Değer ne kadar yüksekse yetki o kadar fazladır.
    /// İzin kontrolleri: role >= UserRole.Manager gibi yapılır.
    /// Unvan (CEO, Geliştirici vb.) için BusinessMember.Position kullanılır.
    /// </summary>
    public enum UserRole
    {
        Employee  = 10,
        TeamLead  = 50,
        Manager   = 60,
        CEO       = 80,
        Owner     = 100
    }
}