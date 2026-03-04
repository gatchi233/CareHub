using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class IntegerDoseQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ChangeQty",
                table: "MedicationInventoryLedgers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AlterColumn<int>(
                name: "DoseQuantity",
                table: "MarEntries",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "ChangeQty",
                table: "MedicationInventoryLedgers",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "DoseQuantity",
                table: "MarEntries",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
