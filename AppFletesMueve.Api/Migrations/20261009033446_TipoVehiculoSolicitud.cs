using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppFletesMueve.Api.Migrations
{
    /// <inheritdoc />
    public partial class TipoVehiculoSolicitud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TipoVehiculo",
                table: "SolicitudesFlete",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Utilitario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoVehiculo",
                table: "SolicitudesFlete");
        }
    }
}