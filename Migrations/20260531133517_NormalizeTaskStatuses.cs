using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Personelim.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeTaskStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"TaskItems\" SET \"Status\" = 'Tamamlandı' WHERE \"Status\" = 'DONE';");
            migrationBuilder.Sql("UPDATE \"TaskItems\" SET \"Status\" = 'Kapatıldı' WHERE \"Status\" = 'CLOSED';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data migration: down is intentionally no-op since original DONE/CLOSED rows cannot be distinguished from rows that were Tamamlandı/Kapatıldı already.
        }
    }
}
