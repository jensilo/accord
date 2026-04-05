using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accord.Web.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyToTemplateSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TemplateSets_Name_Version",
                table: "TemplateSets");

            migrationBuilder.AddColumn<string>(
                name: "Family",
                table: "TemplateSets",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSets_Family_Name_Version",
                table: "TemplateSets",
                columns: new[] { "Family", "Name", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TemplateSets_Family_Name_Version",
                table: "TemplateSets");

            migrationBuilder.DropColumn(
                name: "Family",
                table: "TemplateSets");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateSets_Name_Version",
                table: "TemplateSets",
                columns: new[] { "Name", "Version" },
                unique: true);
        }
    }
}
