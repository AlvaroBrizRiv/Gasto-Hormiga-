using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastoHormiga.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEsHormigaToSuscripcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsHormiga",
                table: "Suscripciones",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsHormiga",
                table: "Suscripciones");
        }
    }
}
