using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_WorkoutSessionEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workout_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationSec = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SessionNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "session_exercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RestSecPlanned = table.Column<int>(type: "integer", nullable: false),
                    RestSecActual = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_session_exercises_workout_sessions_WorkoutSessionId",
                        column: x => x.WorkoutSessionId,
                        principalTable: "workout_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "session_sets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SetNumber = table.Column<int>(type: "integer", nullable: false),
                    RepsPlanned = table.Column<int>(type: "integer", nullable: false),
                    WeightPlanned = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    RepsDone = table.Column<int>(type: "integer", nullable: true),
                    WeightDone = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    Rpe = table.Column<decimal>(type: "numeric", nullable: true),
                    IsFailure = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_sets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_session_sets_session_exercises_SessionExerciseId",
                        column: x => x.SessionExerciseId,
                        principalTable: "session_exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_session_exercises_WorkoutSessionId_Order",
                table: "session_exercises",
                columns: new[] { "WorkoutSessionId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_session_sets_SessionExerciseId_SetNumber",
                table: "session_sets",
                columns: new[] { "SessionExerciseId", "SetNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_ScheduledId",
                table: "workout_sessions",
                column: "ScheduledId");

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_Status_StartedAtUtc",
                table: "workout_sessions",
                columns: new[] { "Status", "StartedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "session_sets");

            migrationBuilder.DropTable(
                name: "session_exercises");

            migrationBuilder.DropTable(
                name: "workout_sessions");
        }
    }
}
