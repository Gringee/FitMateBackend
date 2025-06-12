using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinalMigrationChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "body_parts",
                columns: table => new
                {
                    BodyPartId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_body_parts", x => x.BodyPartId);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "workouts",
                columns: table => new
                {
                    WorkoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workouts", x => x.WorkoutId);
                });

            migrationBuilder.CreateTable(
                name: "exercises",
                columns: table => new
                {
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DifficultyLevel = table.Column<string>(type: "text", nullable: false),
                    BodyPartId = table.Column<Guid>(type: "uuid", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercises", x => x.ExerciseId);
                    table.ForeignKey(
                        name: "FK_exercises_body_parts_BodyPartId",
                        column: x => x.BodyPartId,
                        principalTable: "body_parts",
                        principalColumn: "BodyPartId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_exercises_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "workout_exercises",
                columns: table => new
                {
                    WorkoutExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SetNumber = table.Column<int>(type: "integer", nullable: false),
                    Repetitions = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_exercises", x => x.WorkoutExerciseId);
                    table.ForeignKey(
                        name: "FK_workout_exercises_exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "exercises",
                        principalColumn: "ExerciseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workout_exercises_workouts_WorkoutId",
                        column: x => x.WorkoutId,
                        principalTable: "workouts",
                        principalColumn: "WorkoutId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_body_parts_Name",
                table: "body_parts",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_Name",
                table: "categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exercises_BodyPartId",
                table: "exercises",
                column: "BodyPartId");

            migrationBuilder.CreateIndex(
                name: "IX_exercises_CategoryId",
                table: "exercises",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_exercises_Name",
                table: "exercises",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_workout_exercises_ExerciseId",
                table: "workout_exercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_workout_exercises_WorkoutId_SetNumber",
                table: "workout_exercises",
                columns: new[] { "WorkoutId", "SetNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workouts_UserId",
                table: "workouts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "workout_exercises");

            migrationBuilder.DropTable(
                name: "exercises");

            migrationBuilder.DropTable(
                name: "workouts");

            migrationBuilder.DropTable(
                name: "body_parts");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
