using System;
using AppFletesMueve.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppFletesMueve.Api.Migrations
{
    [DbContext(typeof(MueveDbContext))]
    [Migration("20261127000000_AgregarPromocionPrimerViaje")]
    public partial class AgregarPromocionPrimerViaje : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Las cuentas previas no tienen fecha de alta confiable: permanecen en null.
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRegistroUtc",
                table: "Usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DescuentoPromocional",
                table: "SolicitudesFlete",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioCliente",
                table: "SolicitudesFlete",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // El historial existente conserva el mismo importe para cliente y conductor.
            migrationBuilder.Sql("UPDATE \"SolicitudesFlete\" SET \"PrecioCliente\" = \"Precio\";");

            migrationBuilder.AddColumn<string>(
                name: "PromocionAplicada",
                table: "SolicitudesFlete",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "FechaRegistroUtc", table: "Usuarios");
            migrationBuilder.DropColumn(name: "DescuentoPromocional", table: "SolicitudesFlete");
            migrationBuilder.DropColumn(name: "PrecioCliente", table: "SolicitudesFlete");
            migrationBuilder.DropColumn(name: "PromocionAplicada", table: "SolicitudesFlete");
        }
    }
}
