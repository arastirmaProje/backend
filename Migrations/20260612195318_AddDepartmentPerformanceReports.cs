using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Personelim.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentPerformanceReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DepartmentPerformanceReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DepartmanAdi = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DepartmanSkoru = table.Column<double>(type: "double precision", nullable: false),
                    ToplamCalisan = table.Column<int>(type: "integer", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    DetailedReport = table.Column<string>(type: "text", nullable: true),
                    AiRequestJson = table.Column<string>(type: "text", nullable: false),
                    AiResponseJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentPerformanceReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentPerformanceReports_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DepartmentPerformanceReports_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentPerformanceReports_BusinessId_DepartmentId_Period~",
                table: "DepartmentPerformanceReports",
                columns: new[] { "BusinessId", "DepartmentId", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentPerformanceReports_DepartmentId",
                table: "DepartmentPerformanceReports",
                column: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartmentPerformanceReports");
        }
    }
}
