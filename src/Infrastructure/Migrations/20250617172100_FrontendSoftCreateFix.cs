using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FrontendSoftCreateFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_exercises_Name",
                table: "exercises");

            migrationBuilder.CreateIndex(
                name: "ix_exercises_name_lower",
                table: "exercises",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_exercises_name_lower",
                table: "exercises");

            migrationBuilder.CreateIndex(
                name: "IX_exercises_Name",
                table: "exercises",
                column: "Name");
        }
    }
}
