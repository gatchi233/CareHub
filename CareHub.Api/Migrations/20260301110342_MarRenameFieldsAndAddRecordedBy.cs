using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class MarRenameFieldsAndAddRecordedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DoseAmount",
                table: "MarEntries",
                newName: "DoseQuantity");

            migrationBuilder.AddColumn<string>(
                name: "RecordedBy",
                table: "MarEntries",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecordedBy",
                table: "MarEntries");

            migrationBuilder.RenameColumn(
                name: "DoseQuantity",
                table: "MarEntries",
                newName: "DoseAmount");
        }
    }
}
