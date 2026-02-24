using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedReminder.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Residents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    AdmissionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RoomNumber = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Residents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Medications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MedName = table.Column<string>(type: "text", nullable: false),
                    Dosage = table.Column<string>(type: "text", nullable: false),
                    Usage = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    QuantityUnit = table.Column<string>(type: "text", nullable: false),
                    StockQuantity = table.Column<int>(type: "integer", nullable: false),
                    ReorderLevel = table.Column<int>(type: "integer", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResidentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResidentName = table.Column<string>(type: "text", nullable: true),
                    IsDone = table.Column<bool>(type: "boolean", nullable: false),
                    TimesPerDay = table.Column<int>(type: "integer", nullable: false),
                    ReminderMon = table.Column<bool>(type: "boolean", nullable: false),
                    ReminderTue = table.Column<bool>(type: "boolean", nullable: false),
                    ReminderWed = table.Column<bool>(type: "boolean", nullable: false),
                    ReminderThu = table.Column<bool>(type: "boolean", nullable: false),
                    ReminderFri = table.Column<bool>(type: "boolean", nullable: false),
                    ReminderSat = table.Column<bool>(type: "boolean", nullable: false),
                    ReminderSun = table.Column<bool>(type: "boolean", nullable: false),
                    MonTime1 = table.Column<TimeSpan>(type: "interval", nullable: false),
                    MonTime2 = table.Column<TimeSpan>(type: "interval", nullable: false),
                    MonTime3 = table.Column<TimeSpan>(type: "interval", nullable: false),
                    TueTime1 = table.Column<TimeSpan>(type: "interval", nullable: false),
                    TueTime2 = table.Column<TimeSpan>(type: "interval", nullable: false),
                    TueTime3 = table.Column<TimeSpan>(type: "interval", nullable: false),
                    WedTime1 = table.Column<TimeSpan>(type: "interval", nullable: false),
                    WedTime2 = table.Column<TimeSpan>(type: "interval", nullable: false),
                    WedTime3 = table.Column<TimeSpan>(type: "interval", nullable: false),
                    ThuTime1 = table.Column<TimeSpan>(type: "interval", nullable: false),
                    ThuTime2 = table.Column<TimeSpan>(type: "interval", nullable: false),
                    ThuTime3 = table.Column<TimeSpan>(type: "interval", nullable: false),
                    FriTime1 = table.Column<TimeSpan>(type: "interval", nullable: false),
                    FriTime2 = table.Column<TimeSpan>(type: "interval", nullable: false),
                    FriTime3 = table.Column<TimeSpan>(type: "interval", nullable: false),
                    SatTime1 = table.Column<TimeSpan>(type: "interval", nullable: false),
                    SatTime2 = table.Column<TimeSpan>(type: "interval", nullable: false),
                    SatTime3 = table.Column<TimeSpan>(type: "interval", nullable: false),
                    SunTime1 = table.Column<TimeSpan>(type: "interval", nullable: false),
                    SunTime2 = table.Column<TimeSpan>(type: "interval", nullable: false),
                    SunTime3 = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Medications_Residents_ResidentId",
                        column: x => x.ResidentId,
                        principalTable: "Residents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Observations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    RecordedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Observations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Observations_Residents_ResidentId",
                        column: x => x.ResidentId,
                        principalTable: "Residents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Medications_ResidentId",
                table: "Medications",
                column: "ResidentId");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_ResidentId",
                table: "Observations",
                column: "ResidentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Medications");

            migrationBuilder.DropTable(
                name: "Observations");

            migrationBuilder.DropTable(
                name: "Residents");
        }
    }
}
