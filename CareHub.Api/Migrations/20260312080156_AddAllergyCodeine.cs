using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAllergyCodeine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RecordedAt",
                table: "Observations",
                newName: "RecordedAtUtc");

            migrationBuilder.AddColumn<bool>(
                name: "AllergyCodeine",
                table: "Residents",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllergyCodeine",
                table: "Residents");

            migrationBuilder.RenameColumn(
                name: "RecordedAtUtc",
                table: "Observations",
                newName: "RecordedAt");
        }
    }
}
