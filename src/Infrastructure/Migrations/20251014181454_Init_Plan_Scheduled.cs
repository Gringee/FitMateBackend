using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Init_Plan_Scheduled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "plan_exercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RestSeconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 90)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plan_exercises_plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scheduled_workouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Time = table.Column<TimeOnly>(type: "time", nullable: true),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_workouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scheduled_workouts_plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "plan_sets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SetNumber = table.Column<int>(type: "integer", nullable: false),
                    Reps = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_sets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plan_sets_plan_exercises_PlanExerciseId",
                        column: x => x.PlanExerciseId,
                        principalTable: "plan_exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scheduled_exercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledWorkoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RestSeconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 90)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scheduled_exercises_scheduled_workouts_ScheduledWorkoutId",
                        column: x => x.ScheduledWorkoutId,
                        principalTable: "scheduled_workouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scheduled_sets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SetNumber = table.Column<int>(type: "integer", nullable: false),
                    Reps = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scheduled_sets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scheduled_sets_scheduled_exercises_ScheduledExerciseId",
                        column: x => x.ScheduledExerciseId,
                        principalTable: "scheduled_exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_plan_exercises_PlanId",
                table: "plan_exercises",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_plan_sets_PlanExerciseId_SetNumber",
                table: "plan_sets",
                columns: new[] { "PlanExerciseId", "SetNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_exercises_ScheduledWorkoutId",
                table: "scheduled_exercises",
                column: "ScheduledWorkoutId");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_sets_ScheduledExerciseId_SetNumber",
                table: "scheduled_sets",
                columns: new[] { "ScheduledExerciseId", "SetNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_workouts_Date",
                table: "scheduled_workouts",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_workouts_PlanId",
                table: "scheduled_workouts",
                column: "PlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plan_sets");

            migrationBuilder.DropTable(
                name: "scheduled_sets");

            migrationBuilder.DropTable(
                name: "plan_exercises");

            migrationBuilder.DropTable(
                name: "scheduled_exercises");

            migrationBuilder.DropTable(
                name: "scheduled_workouts");

            migrationBuilder.DropTable(
                name: "plans");
        }
    }
}
