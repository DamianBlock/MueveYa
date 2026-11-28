using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppFletesMueve.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPinSeguridadViaje : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IntentosPinSeguridad",
                table: "SolicitudesFlete",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PinSeguridadBloqueadoUtc",
                table: "SolicitudesFlete",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PinSeguridadHash",
                table: "SolicitudesFlete",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PinSeguridadValidado",
                table: "SolicitudesFlete",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IntentosPinSeguridad", table: "SolicitudesFlete");
            migrationBuilder.DropColumn(name: "PinSeguridadBloqueadoUtc", table: "SolicitudesFlete");
            migrationBuilder.DropColumn(name: "PinSeguridadHash", table: "SolicitudesFlete");
            migrationBuilder.DropColumn(name: "PinSeguridadValidado", table: "SolicitudesFlete");
        }
    }
}
