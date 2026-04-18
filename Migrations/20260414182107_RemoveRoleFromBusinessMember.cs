using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Personelim.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRoleFromBusinessMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NULL olan Position değerlerini "Diğer" yap, sonra NOT NULL'a geç
            migrationBuilder.Sql("UPDATE \"BusinessMembers\" SET \"Position\" = 'Diğer' WHERE \"Position\" IS NULL OR \"Position\" = ''");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "BusinessMembers");

            migrationBuilder.AlterColumn<string>(
                name: "Position",
                table: "BusinessMembers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Diğer",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Position",
                table: "BusinessMembers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "BusinessMembers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
