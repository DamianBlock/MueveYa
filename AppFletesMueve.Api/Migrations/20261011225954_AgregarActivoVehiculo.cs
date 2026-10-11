using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppFletesMueve.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarActivoVehiculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Vehiculos",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Vehiculos");
        }
    }
}
