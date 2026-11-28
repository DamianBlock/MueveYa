using System;
using AppFletesMueve.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppFletesMueve.Api.Migrations;

[DbContext(typeof(MueveDbContext))]
[Migration("20261127000100_AgregarSolicitudEliminacionCuenta")]
public partial class AgregarSolicitudEliminacionCuenta : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<DateTime>(
        name: "SolicitudEliminacionUtc", table: "Usuarios", type: "timestamp with time zone", nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
        name: "SolicitudEliminacionUtc", table: "Usuarios");
}
