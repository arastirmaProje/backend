using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Personelim.Migrations
{
    /// <inheritdoc />
    public partial class RenameMeetingsToSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Meetings",
                newName: "Schedules");

            migrationBuilder.RenameIndex(
                name: "IX_Meetings_CreatedByUserId",
                table: "Schedules",
                newName: "IX_Schedules_CreatedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Meetings_BusinessId",
                table: "Schedules",
                newName: "IX_Schedules_BusinessId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Schedules",
                newName: "Meetings");

            migrationBuilder.RenameIndex(
                name: "IX_Schedules_CreatedByUserId",
                table: "Meetings",
                newName: "IX_Meetings_CreatedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Schedules_BusinessId",
                table: "Meetings",
                newName: "IX_Meetings_BusinessId");
        }
    }
}
