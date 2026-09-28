using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YStreamUtils.Core.Data.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class Added_Something_I_Forgot_postgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Topic",
                table: "UserScripts",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "PluginSettings",
                columns: table => new
                {
                    PluginName = table.Column<string>(type: "text", nullable: false),
                    SettingsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PluginSettings", x => x.PluginName);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PluginSettings");

            migrationBuilder.AlterColumn<int>(
                name: "Topic",
                table: "UserScripts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
