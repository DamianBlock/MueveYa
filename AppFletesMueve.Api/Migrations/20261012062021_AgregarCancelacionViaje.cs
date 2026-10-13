using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppFletesMueve.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCancelacionViaje : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CanceladaEnEtapa",
                table: "SolicitudesFlete",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CanceladaPor",
                table: "SolicitudesFlete",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CanceladaUtc",
                table: "SolicitudesFlete",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoCancelacion",
                table: "SolicitudesFlete",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanceladaEnEtapa",
                table: "SolicitudesFlete");

            migrationBuilder.DropColumn(
                name: "CanceladaPor",
                table: "SolicitudesFlete");

            migrationBuilder.DropColumn(
                name: "CanceladaUtc",
                table: "SolicitudesFlete");

            migrationBuilder.DropColumn(
                name: "MotivoCancelacion",
                table: "SolicitudesFlete");
        }
    }
}
