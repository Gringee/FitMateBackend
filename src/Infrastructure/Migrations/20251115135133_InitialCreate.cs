using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "citext", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserName = table.Column<string>(type: "citext", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.CheckConstraint("CK_users_email_format", "\"Email\" ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}$'");
                });

            migrationBuilder.CreateTable(
                name: "friendships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserAId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserBId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_friendships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_friendships_users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_friendships_users_UserAId",
                        column: x => x.UserAId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_friendships_users_UserBId",
                        column: x => x.UserBId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plans_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationSec = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SessionNotes = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workout_sessions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
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
                    table.ForeignKey(
                        name: "FK_scheduled_workouts_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shared_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedWithUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    RespondedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shared_plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shared_plans_plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_shared_plans_users_SharedByUserId",
                        column: x => x.SharedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_shared_plans_users_SharedWithUserId",
                        column: x => x.SharedWithUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "IX_friendships_RequestedByUserId",
                table: "friendships",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_friendships_UserAId_UserBId",
                table: "friendships",
                columns: new[] { "UserAId", "UserBId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_friendships_UserBId",
                table: "friendships",
                column: "UserBId");

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
                name: "IX_plans_CreatedByUserId_PlanName",
                table: "plans",
                columns: new[] { "CreatedByUserId", "PlanName" });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_Token",
                table: "refresh_tokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId_ExpiresAtUtc",
                table: "refresh_tokens",
                columns: new[] { "UserId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_roles_Name",
                table: "roles",
                column: "Name",
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
                name: "IX_scheduled_workouts_PlanId",
                table: "scheduled_workouts",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_scheduled_workouts_UserId_Date",
                table: "scheduled_workouts",
                columns: new[] { "UserId", "Date" });

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
                name: "IX_shared_plans_PlanId",
                table: "shared_plans",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_shared_plans_SharedByUserId",
                table: "shared_plans",
                column: "SharedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_shared_plans_SharedWithUserId_PlanId",
                table: "shared_plans",
                columns: new[] { "SharedWithUserId", "PlanId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_RoleId",
                table: "user_roles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_UserName",
                table: "users",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_ScheduledId",
                table: "workout_sessions",
                column: "ScheduledId");

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_Status_StartedAtUtc",
                table: "workout_sessions",
                columns: new[] { "Status", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_UserId_StartedAtUtc",
                table: "workout_sessions",
                columns: new[] { "UserId", "StartedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "friendships");

            migrationBuilder.DropTable(
                name: "plan_sets");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "scheduled_sets");

            migrationBuilder.DropTable(
                name: "session_sets");

            migrationBuilder.DropTable(
                name: "shared_plans");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "plan_exercises");

            migrationBuilder.DropTable(
                name: "scheduled_exercises");

            migrationBuilder.DropTable(
                name: "session_exercises");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "scheduled_workouts");

            migrationBuilder.DropTable(
                name: "workout_sessions");

            migrationBuilder.DropTable(
                name: "plans");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
