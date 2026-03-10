using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Progetto_Web_2_IoT_Auth.Migrations
{
    /// <inheritdoc />
    public partial class InitialNewSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Mail = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "zone",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_zone", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "access",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ZoneId = table.Column<int>(type: "INTEGER", nullable: false),
                    AccessLevel = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_access", x => x.Id);
                    table.ForeignKey(
                        name: "FK_access_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_access_zone_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "zone",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "device",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ZoneId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    St = table.Column<int>(type: "INTEGER", nullable: false),
                    Power = table.Column<bool>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device", x => x.Id);
                    table.ForeignKey(
                        name: "FK_device_zone_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "zone",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "automation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeviceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Power = table.Column<bool>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeCondition = table.Column<string>(type: "TEXT", nullable: false),
                    WeatherCondition = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_automation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_automation_device_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "device",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "Mail", "PasswordHash", "Role", "Username" },
                values: new object[] { 1, "admin@example.local", "$2a$11$5bXqGaqh3uehFVuTEdfWLOfFUxE7KFIRYv/XOqmEgdon7oNxpVQxS", "admin", "admin" });

            migrationBuilder.InsertData(
                table: "zone",
                columns: new[] { "Id", "Name", "Type" },
                values: new object[] { 1, "default", "default" });

            migrationBuilder.CreateIndex(
                name: "IX_access_UserId_ZoneId",
                table: "access",
                columns: new[] { "UserId", "ZoneId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_access_ZoneId",
                table: "access",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_automation_DeviceId",
                table: "automation",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_device_ZoneId",
                table: "device",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Mail",
                table: "users",
                column: "Mail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Username",
                table: "users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access");

            migrationBuilder.DropTable(
                name: "automation");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "device");

            migrationBuilder.DropTable(
                name: "zone");
        }
    }
}
