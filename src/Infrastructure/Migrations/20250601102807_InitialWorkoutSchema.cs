using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialWorkoutSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Users",
                newName: "UserId");

            migrationBuilder.CreateTable(
                name: "Exercises",
                columns: table => new
                {
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DifficultyLevel = table.Column<string>(type: "text", nullable: false),
                    BodyPartId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.ExerciseId);
                });

            migrationBuilder.CreateTable(
                name: "Workouts",
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
                    table.PrimaryKey("PK_Workouts", x => x.WorkoutId);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutExercise",
                columns: table => new
                {
                    WorkoutExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SetNumber = table.Column<int>(type: "integer", nullable: false),
                    Repetitions = table.Column<int>(type: "integer", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutExercise", x => x.WorkoutExerciseId);
                    table.ForeignKey(
                        name: "FK_WorkoutExercise_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "ExerciseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkoutExercise_Workouts_WorkoutId",
                        column: x => x.WorkoutId,
                        principalTable: "Workouts",
                        principalColumn: "WorkoutId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercise_ExerciseId",
                table: "WorkoutExercise",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercise_WorkoutId",
                table: "WorkoutExercise",
                column: "WorkoutId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkoutExercise");

            migrationBuilder.DropTable(
                name: "Exercises");

            migrationBuilder.DropTable(
                name: "Workouts");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Users",
                newName: "Id");
        }
    }
}
