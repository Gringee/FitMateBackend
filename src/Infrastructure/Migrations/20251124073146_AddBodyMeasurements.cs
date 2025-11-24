using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBodyMeasurements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "body_measurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeasuredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    HeightCm = table.Column<int>(type: "integer", nullable: false),
                    BMI = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    BodyFatPercentage = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    ChestCm = table.Column<int>(type: "integer", nullable: true),
                    WaistCm = table.Column<int>(type: "integer", nullable: true),
                    HipsCm = table.Column<int>(type: "integer", nullable: true),
                    BicepsCm = table.Column<int>(type: "integer", nullable: true),
                    ThighsCm = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_body_measurements", x => x.Id);
                    table.CheckConstraint("CK_body_measurements_bmi_valid", "\"BMI\" >= 10 AND \"BMI\" <= 100");
                    table.CheckConstraint("CK_body_measurements_height_valid", "\"HeightCm\" >= 50 AND \"HeightCm\" <= 300");
                    table.CheckConstraint("CK_body_measurements_weight_positive", "\"WeightKg\" > 0 AND \"WeightKg\" < 500");
                    table.ForeignKey(
                        name: "FK_body_measurements_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_body_measurements_MeasuredAtUtc",
                table: "body_measurements",
                column: "MeasuredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_body_measurements_UserId",
                table: "body_measurements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_body_measurements_UserId_MeasuredAtUtc",
                table: "body_measurements",
                columns: new[] { "UserId", "MeasuredAtUtc" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "body_measurements");
        }
    }
}
