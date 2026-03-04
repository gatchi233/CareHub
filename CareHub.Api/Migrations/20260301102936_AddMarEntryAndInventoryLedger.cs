using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMarEntryAndInventoryLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicationOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DoseAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    DoseUnit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScheduledForUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdministeredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AdministeredByStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsVoided = table.Column<bool>(type: "boolean", nullable: false),
                    VoidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarEntries_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarEntries_Residents_ResidentId",
                        column: x => x.ResidentId,
                        principalTable: "Residents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicationInventoryLedgers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeQty = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationInventoryLedgers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicationInventoryLedgers_MarEntries_MarEntryId",
                        column: x => x.MarEntryId,
                        principalTable: "MarEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicationInventoryLedgers_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarEntries_ClientRequestId",
                table: "MarEntries",
                column: "ClientRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarEntries_MedicationId",
                table: "MarEntries",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_MarEntries_ResidentId",
                table: "MarEntries",
                column: "ResidentId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationInventoryLedgers_MarEntryId",
                table: "MedicationInventoryLedgers",
                column: "MarEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicationInventoryLedgers_MedicationId",
                table: "MedicationInventoryLedgers",
                column: "MedicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicationInventoryLedgers");

            migrationBuilder.DropTable(
                name: "MarEntries");
        }
    }
}
