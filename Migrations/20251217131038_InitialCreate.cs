using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Progetto_Web_2_IoT_Auth.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    password = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    role = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.id);
                });

            // Inserimento utente di default
            migrationBuilder.InsertData(
                table: "User",
                columns: new[] { "id", "name", "password", "role" },
                values: new object[] { 1, "admin", "$2a$08$pT2DIgkMi/PC/7lmlVAyquaiZM1cNo1xJEjSp0yOiCtWh4NYKA8xG", "Administrator" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
