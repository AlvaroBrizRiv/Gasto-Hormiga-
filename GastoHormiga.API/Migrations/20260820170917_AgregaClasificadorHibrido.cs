using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastoHormiga.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregaClasificadorHibrido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Categoria",
                table: "GastosHormiga",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Alimentos & Antojos",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "General");

            migrationBuilder.AddColumn<bool>(
                name: "EsHormigaManual",
                table: "GastosHormiga",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AlertasPatron",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CantidadTransacciones = table.Column<int>(type: "int", nullable: false),
                    MontoAcumulado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PeriodoInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Pendiente"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    FechaResolucion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertasPatron", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertasPatron_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReglasMercante",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    PalabraClave = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EsPresupuestado = table.Column<bool>(type: "bit", nullable: false),
                    CategoriaOverride = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReglasMercante", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReglasMercante_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertasPatron_UsuarioId_Estado",
                table: "AlertasPatron",
                columns: new[] { "UsuarioId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_ReglasMercante_UsuarioId_PalabraClave",
                table: "ReglasMercante",
                columns: new[] { "UsuarioId", "PalabraClave" },
                unique: true);

            // ── MIGRACIÓN DE DATOS: Categorías antiguas → Nuevas 5 categorías ────────
            // Mapeo: General → Alimentos & Antojos, Otros → Alimentos & Antojos,
            //        Entretenimiento → Suscripciones & Digital, Suscripciones → Suscripciones & Digital,
            //        Alimentacion → Alimentos & Antojos, Transporte → Transporte & Movilidad,
            //        Salud/Arriendo/PagoCuotaAuto/PagoPrestamos/TarjetaCredito → Financiera
            migrationBuilder.Sql(@"
                UPDATE GastosHormiga SET Categoria = 'Alimentos & Antojos'     WHERE Categoria IN ('General', 'Otros', 'Alimentacion');
                UPDATE GastosHormiga SET Categoria = 'Suscripciones & Digital' WHERE Categoria IN ('Entretenimiento', 'Suscripciones');
                UPDATE GastosHormiga SET Categoria = 'Transporte & Movilidad'  WHERE Categoria = 'Transporte';
                UPDATE GastosHormiga SET Categoria = 'Financiera'              WHERE Categoria IN ('Salud', 'Arriendo/Dividendo', 'PagoCuotaAuto', 'PagoPrestamos', 'TarjetaCredito');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertasPatron");

            migrationBuilder.DropTable(
                name: "ReglasMercante");

            migrationBuilder.DropColumn(
                name: "EsHormigaManual",
                table: "GastosHormiga");

            migrationBuilder.AlterColumn<string>(
                name: "Categoria",
                table: "GastosHormiga",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "General",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "Alimentos & Antojos");
        }
    }
}
