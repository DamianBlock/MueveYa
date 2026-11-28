using System;
using AppFletesMueve.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppFletesMueve.Api.Migrations
{
    [DbContext(typeof(MueveDbContext))]
    [Migration("20261128000000_AgregarRecuperacionPassword")]
    public partial class AgregarRecuperacionPassword : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoRecuperacionHash",
                table: "Usuarios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CodigoRecuperacionVenceUtc",
                table: "Usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntentosCodigoRecuperacion",
                table: "Usuarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CodigoRecuperacionHash", table: "Usuarios");
            migrationBuilder.DropColumn(name: "CodigoRecuperacionVenceUtc", table: "Usuarios");
            migrationBuilder.DropColumn(name: "IntentosCodigoRecuperacion", table: "Usuarios");
        }
    }
}
