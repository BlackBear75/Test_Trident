using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramBot.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhpScripts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    AppName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppBundle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Secret = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecretKeyParam = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScriptContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SftpHost = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SftpPassword = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SftpLogin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<int>(type: "int", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhpScripts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreationDate", "Deleted", "DeletionDate", "Role", "Username" },
                values: new object[] { 914220215L, new DateTime(2023, 2, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), false, null, 2, "Bogdan_Porivay" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhpScripts");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
