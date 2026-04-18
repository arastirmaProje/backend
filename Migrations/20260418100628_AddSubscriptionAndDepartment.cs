using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Personelim.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionAndDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "BusinessMembers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSubscribed",
                table: "Businesses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMembers_DepartmentId",
                table: "BusinessMembers",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_BusinessId",
                table: "Departments",
                column: "BusinessId");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessMembers_Departments_DepartmentId",
                table: "BusinessMembers",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessMembers_Departments_DepartmentId",
                table: "BusinessMembers");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_BusinessMembers_DepartmentId",
                table: "BusinessMembers");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "BusinessMembers");

            migrationBuilder.DropColumn(
                name: "IsSubscribed",
                table: "Businesses");
        }
    }
}
