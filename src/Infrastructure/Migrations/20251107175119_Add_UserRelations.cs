using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_UserRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_scheduled_workouts_Date",
                table: "scheduled_workouts");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "workout_sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "scheduled_workouts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "plans",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "PlanAccess",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Permission = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanAccess", x => new { x.UserId, x.PlanId });
                    table.ForeignKey(
                        name: "FK_PlanAccess_plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlanAccess_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_UserId",
                table: "workout_sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_workouts_UserId_Date",
                table: "scheduled_workouts",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_plans_CreatedByUserId_PlanName",
                table: "plans",
                columns: new[] { "CreatedByUserId", "PlanName" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanAccess_PlanId",
                table: "PlanAccess",
                column: "PlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_plans_users_CreatedByUserId",
                table: "plans",
                column: "CreatedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_scheduled_workouts_users_UserId",
                table: "scheduled_workouts",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_workout_sessions_users_UserId",
                table: "workout_sessions",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_plans_users_CreatedByUserId",
                table: "plans");

            migrationBuilder.DropForeignKey(
                name: "FK_scheduled_workouts_users_UserId",
                table: "scheduled_workouts");

            migrationBuilder.DropForeignKey(
                name: "FK_workout_sessions_users_UserId",
                table: "workout_sessions");

            migrationBuilder.DropTable(
                name: "PlanAccess");

            migrationBuilder.DropIndex(
                name: "IX_workout_sessions_UserId",
                table: "workout_sessions");

            migrationBuilder.DropIndex(
                name: "IX_scheduled_workouts_UserId_Date",
                table: "scheduled_workouts");

            migrationBuilder.DropIndex(
                name: "IX_plans_CreatedByUserId_PlanName",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "scheduled_workouts");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "plans");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "workout_sessions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_workouts_Date",
                table: "scheduled_workouts",
                column: "Date");
        }
    }
}
