using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWorkoutExcerciseIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workout_exercises_WorkoutId_SetNumber",
                table: "workout_exercises");

            migrationBuilder.CreateIndex(
                name: "IX_workout_exercises_WorkoutId_ExerciseId_SetNumber",
                table: "workout_exercises",
                columns: new[] { "WorkoutId", "ExerciseId", "SetNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workout_exercises_WorkoutId_ExerciseId_SetNumber",
                table: "workout_exercises");

            migrationBuilder.CreateIndex(
                name: "IX_workout_exercises_WorkoutId_SetNumber",
                table: "workout_exercises",
                columns: new[] { "WorkoutId", "SetNumber" },
                unique: true);
        }
    }
}
