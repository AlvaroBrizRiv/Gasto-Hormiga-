using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastoHormiga.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoFrecuenciaYTarjeta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsImportadoTarjeta",
                table: "GastosHormiga",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OrigenTarjeta",
                table: "GastosHormiga",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoFrecuencia",
                table: "GastosHormiga",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Semanal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsImportadoTarjeta",
                table: "GastosHormiga");

            migrationBuilder.DropColumn(
                name: "OrigenTarjeta",
                table: "GastosHormiga");

            migrationBuilder.DropColumn(
                name: "TipoFrecuencia",
                table: "GastosHormiga");
        }
    }
}
