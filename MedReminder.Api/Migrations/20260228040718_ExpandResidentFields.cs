using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedReminder.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExpandResidentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Residents",
                newName: "ResidentLName");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "Residents",
                newName: "ResidentFName");

            migrationBuilder.AlterColumn<string>(
                name: "DateOfBirth",
                table: "Residents",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AdmissionDate",
                table: "Residents",
                type: "text",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Residents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllergyAspirin",
                table: "Residents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllergyEggs",
                table: "Residents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllergyFish",
                table: "Residents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllergyLatex",
                table: "Residents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllergyMilk",
                table: "Residents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AllergyOtherItems",
                table: "Residents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllergyPeanuts",
                table: "Residents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllergyPenicillin",
                table: "Residents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllergyShellfish",
                table: "Residents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllergySoy",
                table: "Residents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllergySulfa",
                table: "Residents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllergyTreeNuts",
                table: "Residents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllergyWheat",
                table: "Residents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "BedLabel",
                table: "Residents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Residents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DoctorContact",
                table: "Residents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DoctorName",
                table: "Residents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactName1",
                table: "Residents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactName2",
                table: "Residents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhone1",
                table: "Residents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhone2",
                table: "Residents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyRelationship1",
                table: "Residents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmergencyRelationship2",
                table: "Residents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Residents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Residents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "Residents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "Residents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoomType",
                table: "Residents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SIN",
                table: "Residents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResidentName",
                table: "Observations",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "AllergyAspirin",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "AllergyEggs",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "AllergyFish",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "AllergyLatex",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "AllergyMilk",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "AllergyOtherItems",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "AllergyPeanuts",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "AllergyPenicillin",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "AllergyShellfish",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "AllergySoy",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "AllergySulfa",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "AllergyTreeNuts",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "AllergyWheat",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "BedLabel",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "DoctorContact",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "DoctorName",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "EmergencyContactName1",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "EmergencyContactName2",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPhone1",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPhone2",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "EmergencyRelationship1",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "EmergencyRelationship2",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "RoomType",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "SIN",
                table: "Residents");

            migrationBuilder.DropColumn(
                name: "ResidentName",
                table: "Observations");

            migrationBuilder.RenameColumn(
                name: "ResidentLName",
                table: "Residents",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "ResidentFName",
                table: "Residents",
                newName: "FirstName");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "DateOfBirth",
                table: "Residents",
                type: "date",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "AdmissionDate",
                table: "Residents",
                type: "date",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
