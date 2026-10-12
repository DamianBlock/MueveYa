using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppFletesMueve.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEtapasViaje : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FinalizadoUtc",
                table: "SolicitudesFlete",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InicioViajeUtc",
                table: "SolicitudesFlete",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LlegadaDestinoUtc",
                table: "SolicitudesFlete",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LlegadaOrigenUtc",
                table: "SolicitudesFlete",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalizadoUtc",
                table: "SolicitudesFlete");

            migrationBuilder.DropColumn(
                name: "InicioViajeUtc",
                table: "SolicitudesFlete");

            migrationBuilder.DropColumn(
                name: "LlegadaDestinoUtc",
                table: "SolicitudesFlete");

            migrationBuilder.DropColumn(
                name: "LlegadaOrigenUtc",
                table: "SolicitudesFlete");
        }
    }
}
