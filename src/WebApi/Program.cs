using Infrastructure.Persistence;
using Infrastructure.Services;             
using Application.Abstractions;            
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core
var conn = builder.Configuration.GetConnectionString("Default")
           ?? throw new InvalidOperationException("Missing ConnectionStrings:Default");
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(conn));

// Controllers
builder.Services.AddControllers();

// DI: services
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<IScheduledService, ScheduledService>();

builder.Services.AddScoped<IWorkoutSessionService, WorkoutSessionService>();

builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMyReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") 
                  .AllowAnyHeader()
                  .AllowAnyMethod(); 
        });
});


var app = builder.Build();

app.UseCors("AllowMyReactApp");

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Ok(new { ok = true, name = "TrainingPlanner API" }));
app.MapGet("/health/db", async (AppDbContext db, CancellationToken ct) =>
{
    var can = await db.Database.CanConnectAsync(ct);
    return can ? Results.Ok(new { db = "ok" }) : Results.Problem("DB not reachable");
});



// map controllers
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.AppDbContext>();
    db.Database.Migrate();
}

app.Run();
