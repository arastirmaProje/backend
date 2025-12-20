using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Personelim.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "EndTime",
                table: "Shifts",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateTable(
                name: "BusinessDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "text", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    FileExtension = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessDocuments_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessDocuments_BusinessId",
                table: "BusinessDocuments",
                column: "BusinessId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessDocuments");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndTime",
                table: "Shifts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
