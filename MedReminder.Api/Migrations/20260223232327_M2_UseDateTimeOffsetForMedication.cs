using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedReminder.Api.Migrations
{
    public partial class M2_UseDateTimeOffsetForMedication : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty:
            // This migration is for Medication DateTimeOffset changes only.
            // (Remove accidental Observation rename.)
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty
        }
    }
}