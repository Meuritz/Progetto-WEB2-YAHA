using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Progetto_Web_2_IoT_Auth.Migrations
{
    /// <inheritdoc />
    public partial class main : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Device",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    type = table.Column<string>(type: "TEXT", nullable: false),
                    userGroupId = table.Column<int>(type: "INTEGER", nullable: true),
                    deviceGroupId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Device", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Device_DeviceGroup_deviceGroupId",
                        column: x => x.deviceGroupId,
                        principalTable: "DeviceGroup",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Device_UserGroup_userGroupId",
                        column: x => x.userGroupId,
                        principalTable: "UserGroup",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    userGroupId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_UserGroup_userGroupId",
                        column: x => x.userGroupId,
                        principalTable: "UserGroup",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "DeviceGroup",
                columns: new[] { "Id", "Name" },
                values: new object[] { 1, "dafault" });

            migrationBuilder.InsertData(
                table: "UserGroup",
                columns: new[] { "Id", "Name" },
                values: new object[] { 1, "dafault" });

            migrationBuilder.InsertData(
                table: "User",
                columns: new[] { "Id", "Name", "Password", "Role", "userGroupId" },
                values: new object[] { 1, "admin", "$2a$12$MR8CJEGoLEj5PXdSPG6KruVdnXjLclghWI/uyg6H81itPlcyA.dDO", "admin", 1 });

            migrationBuilder.CreateIndex(
                name: "IX_Device_deviceGroupId",
                table: "Device",
                column: "deviceGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Device_userGroupId",
                table: "Device",
                column: "userGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_User_userGroupId",
                table: "User",
                column: "userGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Device");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "DeviceGroup");

            migrationBuilder.DropTable(
                name: "UserGroup");
        }
    }
}
