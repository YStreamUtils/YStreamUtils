using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YStreamUtils.Core.Data.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Initial_sqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OAuthConfigs",
                columns: table => new
                {
                    Platform = table.Column<int>(type: "INTEGER", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    ClientSecret = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthConfigs", x => x.Platform);
                });

            migrationBuilder.CreateTable(
                name: "OAuthTokens",
                columns: table => new
                {
                    Platform = table.Column<int>(type: "INTEGER", maxLength: 50, nullable: false),
                    IsBot = table.Column<bool>(type: "INTEGER", nullable: false),
                    TokenJson = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthTokens", x => new { x.Platform, x.IsBot });
                });

            migrationBuilder.CreateTable(
                name: "UserScripts",
                columns: table => new
                {
                    ScriptId = table.Column<string>(type: "TEXT", nullable: false),
                    Topic = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserScripts", x => x.ScriptId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OAuthConfigs");

            migrationBuilder.DropTable(
                name: "OAuthTokens");

            migrationBuilder.DropTable(
                name: "UserScripts");
        }
    }
}
