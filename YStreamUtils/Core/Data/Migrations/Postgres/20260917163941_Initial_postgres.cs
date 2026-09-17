using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YStreamUtils.Core.Data.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class Initial_postgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OAuthConfigs",
                columns: table => new
                {
                    Platform = table.Column<int>(type: "integer", nullable: false),
                    ClientId = table.Column<string>(type: "text", nullable: false),
                    ClientSecret = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthConfigs", x => x.Platform);
                });

            migrationBuilder.CreateTable(
                name: "OAuthTokens",
                columns: table => new
                {
                    Platform = table.Column<int>(type: "integer", maxLength: 50, nullable: false),
                    IsBot = table.Column<bool>(type: "boolean", nullable: false),
                    TokenJson = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthTokens", x => new { x.Platform, x.IsBot });
                });

            migrationBuilder.CreateTable(
                name: "UserScripts",
                columns: table => new
                {
                    ScriptId = table.Column<string>(type: "text", nullable: false),
                    Topic = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
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
