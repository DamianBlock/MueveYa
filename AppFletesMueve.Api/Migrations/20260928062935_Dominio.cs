using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppFletesMueve.Api.Migrations
{
    /// <inheritdoc />
    public partial class Dominio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Conductores",
                columns: table => new
                {
                    ConductorId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Licencia = table.Column<string>(type: "text", nullable: false),
                    Disponible = table.Column<bool>(type: "boolean", nullable: false),
                    Latitud = table.Column<double>(type: "double precision", nullable: false),
                    Longitud = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conductores", x => x.ConductorId);
                    table.ForeignKey(
                        name: "FK_Conductores_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TiposCarga",
                columns: table => new
                {
                    TipoCargaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    PesoEstimadoKg = table.Column<double>(type: "double precision", nullable: false),
                    VolumenEstimadoM3 = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposCarga", x => x.TipoCargaId);
                });

            migrationBuilder.CreateTable(
                name: "Vehiculos",
                columns: table => new
                {
                    VehiculoId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConductorId = table.Column<int>(type: "integer", nullable: false),
                    Patente = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Marca = table.Column<string>(type: "text", nullable: false),
                    Modelo = table.Column<string>(type: "text", nullable: false),
                    Anio = table.Column<int>(type: "integer", nullable: false),
                    TipoVehiculo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CapacidadKg = table.Column<double>(type: "double precision", nullable: false),
                    VolumenM3 = table.Column<double>(type: "double precision", nullable: false),
                    Disponible = table.Column<bool>(type: "boolean", nullable: false),
                    Imagen = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehiculos", x => x.VehiculoId);
                    table.ForeignKey(
                        name: "FK_Vehiculos_Conductores_ConductorId",
                        column: x => x.ConductorId,
                        principalTable: "Conductores",
                        principalColumn: "ConductorId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesFlete",
                columns: table => new
                {
                    SolicitudFleteId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClienteId = table.Column<int>(type: "integer", nullable: false),
                    ConductorId = table.Column<int>(type: "integer", nullable: true),
                    VehiculoId = table.Column<int>(type: "integer", nullable: true),
                    TipoServicio = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaProgramada = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DireccionOrigen = table.Column<string>(type: "text", nullable: false),
                    LatitudOrigen = table.Column<double>(type: "double precision", nullable: false),
                    LongitudOrigen = table.Column<double>(type: "double precision", nullable: false),
                    DireccionDestino = table.Column<string>(type: "text", nullable: false),
                    LatitudDestino = table.Column<double>(type: "double precision", nullable: false),
                    LongitudDestino = table.Column<double>(type: "double precision", nullable: false),
                    DistanciaKm = table.Column<double>(type: "double precision", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesFlete", x => x.SolicitudFleteId);
                    table.ForeignKey(
                        name: "FK_SolicitudesFlete_Conductores_ConductorId",
                        column: x => x.ConductorId,
                        principalTable: "Conductores",
                        principalColumn: "ConductorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesFlete_Usuarios_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesFlete_Vehiculos_VehiculoId",
                        column: x => x.VehiculoId,
                        principalTable: "Vehiculos",
                        principalColumn: "VehiculoId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesCarga",
                columns: table => new
                {
                    SolicitudCargaId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SolicitudFleteId = table.Column<int>(type: "integer", nullable: false),
                    TipoCargaId = table.Column<int>(type: "integer", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    PesoKg = table.Column<double>(type: "double precision", nullable: false),
                    VolumenM3 = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesCarga", x => x.SolicitudCargaId);
                    table.ForeignKey(
                        name: "FK_SolicitudesCarga_SolicitudesFlete_SolicitudFleteId",
                        column: x => x.SolicitudFleteId,
                        principalTable: "SolicitudesFlete",
                        principalColumn: "SolicitudFleteId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SolicitudesCarga_TiposCarga_TipoCargaId",
                        column: x => x.TipoCargaId,
                        principalTable: "TiposCarga",
                        principalColumn: "TipoCargaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Conductores_UsuarioId",
                table: "Conductores",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCarga_SolicitudFleteId",
                table: "SolicitudesCarga",
                column: "SolicitudFleteId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCarga_TipoCargaId",
                table: "SolicitudesCarga",
                column: "TipoCargaId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesFlete_ClienteId",
                table: "SolicitudesFlete",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesFlete_ConductorId",
                table: "SolicitudesFlete",
                column: "ConductorId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesFlete_Estado",
                table: "SolicitudesFlete",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesFlete_VehiculoId",
                table: "SolicitudesFlete",
                column: "VehiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_ConductorId",
                table: "Vehiculos",
                column: "ConductorId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_Patente",
                table: "Vehiculos",
                column: "Patente",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesCarga");

            migrationBuilder.DropTable(
                name: "SolicitudesFlete");

            migrationBuilder.DropTable(
                name: "TiposCarga");

            migrationBuilder.DropTable(
                name: "Vehiculos");

            migrationBuilder.DropTable(
                name: "Conductores");
        }
    }
}
