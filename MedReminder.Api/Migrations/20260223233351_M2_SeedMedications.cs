using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedReminder.Api.Migrations
{
    /// <inheritdoc />
    public partial class M2_SeedMedications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use fixed IDs so the migration is deterministic.
            // (Guid.NewGuid() inside migrations is not recommended.)
            var med1 = new Guid("11111111-1111-1111-1111-111111111111");
            var med2 = new Guid("22222222-2222-2222-2222-222222222222");
            var med3 = new Guid("33333333-3333-3333-3333-333333333333");

            // Use a fixed UTC timestamp so it’s stable across machines.
            var expiry1 = new DateTimeOffset(2027, 01, 01, 0, 0, 0, TimeSpan.Zero);
            var expiry2 = new DateTimeOffset(2026, 10, 01, 0, 0, 0, TimeSpan.Zero);
            var expiry3 = new DateTimeOffset(2026, 08, 15, 0, 0, 0, TimeSpan.Zero);

            migrationBuilder.InsertData(
                table: "Medications",
                columns: new[]
                {
                    "Id",
                    "MedName",
                    "Dosage",
                    "Usage",
                    "Quantity",
                    "QuantityUnit",
                    "StockQuantity",
                    "ReorderLevel",
                    "ExpiryDate",
                    "TimesPerDay",

                    // If your table has these columns, keep them.
                    // If it doesn't, delete the column names + values below.
                    "ResidentId",
                    "ResidentName",
                    "IsDone",

                    "ReminderMon","ReminderTue","ReminderWed","ReminderThu","ReminderFri","ReminderSat","ReminderSun",

                    "MonTime1","MonTime2","MonTime3",
                    "TueTime1","TueTime2","TueTime3",
                    "WedTime1","WedTime2","WedTime3",
                    "ThuTime1","ThuTime2","ThuTime3",
                    "FriTime1","FriTime2","FriTime3",
                    "SatTime1","SatTime2","SatTime3",
                    "SunTime1","SunTime2","SunTime3"
                },
                values: new object[,]
                {
                    {
                        med1,
                        "Aspirin",
                        "81 mg",
                        "Pain relief / blood thinner",
                        1,
                        "tablet",
                        120,
                        20,
                        expiry1,
                        1,

                        null,
                        "John Demo",
                        false,

                        true,true,true,true,true,false,false,

                        new TimeSpan(8,0,0), new TimeSpan(14,0,0), new TimeSpan(20,0,0),
                        new TimeSpan(8,0,0), new TimeSpan(14,0,0), new TimeSpan(20,0,0),
                        new TimeSpan(8,0,0), new TimeSpan(14,0,0), new TimeSpan(20,0,0),
                        new TimeSpan(8,0,0), new TimeSpan(14,0,0), new TimeSpan(20,0,0),
                        new TimeSpan(8,0,0), new TimeSpan(14,0,0), new TimeSpan(20,0,0),
                        new TimeSpan(8,0,0), new TimeSpan(14,0,0), new TimeSpan(20,0,0),
                        new TimeSpan(8,0,0), new TimeSpan(14,0,0), new TimeSpan(20,0,0)
                    },
                    {
                        med2,
                        "Metformin",
                        "500 mg",
                        "Type 2 Diabetes",
                        1,
                        "tablet",
                        60,
                        15,
                        expiry2,
                        2,

                        null,
                        "John Demo",
                        false,

                        true,true,true,true,true,true,true,

                        new TimeSpan(9,0,0), new TimeSpan(18,0,0), new TimeSpan(0,0,0),
                        new TimeSpan(9,0,0), new TimeSpan(18,0,0), new TimeSpan(0,0,0),
                        new TimeSpan(9,0,0), new TimeSpan(18,0,0), new TimeSpan(0,0,0),
                        new TimeSpan(9,0,0), new TimeSpan(18,0,0), new TimeSpan(0,0,0),
                        new TimeSpan(9,0,0), new TimeSpan(18,0,0), new TimeSpan(0,0,0),
                        new TimeSpan(9,0,0), new TimeSpan(18,0,0), new TimeSpan(0,0,0),
                        new TimeSpan(9,0,0), new TimeSpan(18,0,0), new TimeSpan(0,0,0)
                    },
                    {
                        med3,
                        "Lisinopril",
                        "10 mg",
                        "Blood pressure",
                        1,
                        "tablet",
                        90,
                        25,
                        expiry3,
                        1,

                        null,
                        "Mary Test",
                        false,

                        true,true,true,true,true,false,false,

                        new TimeSpan(8,30,0), new TimeSpan(0,0,0), new TimeSpan(0,0,0),
                        new TimeSpan(8,30,0), new TimeSpan(0,0,0), new TimeSpan(0,0,0),
                        new TimeSpan(8,30,0), new TimeSpan(0,0,0), new TimeSpan(0,0,0),
                        new TimeSpan(8,30,0), new TimeSpan(0,0,0), new TimeSpan(0,0,0),
                        new TimeSpan(8,30,0), new TimeSpan(0,0,0), new TimeSpan(0,0,0),
                        new TimeSpan(8,30,0), new TimeSpan(0,0,0), new TimeSpan(0,0,0),
                        new TimeSpan(8,30,0), new TimeSpan(0,0,0), new TimeSpan(0,0,0)
                    }
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Medications",
                keyColumn: "Id",
                keyValues: new object[]
                {
                    new Guid("11111111-1111-1111-1111-111111111111"),
                    new Guid("22222222-2222-2222-2222-222222222222"),
                    new Guid("33333333-3333-3333-3333-333333333333")
                });
        }
    }
}
