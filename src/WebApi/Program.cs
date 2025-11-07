using System.Text;
using Application.Abstractions;
using Infrastructure.Auth;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------------- EF Core ----------------
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(conn));

// ---------------- JWT settings (bind + DI) ----------------
var jwt = new JwtSettings();
builder.Configuration.GetSection(JwtSettings.SectionName).Bind(jwt);
if (string.IsNullOrWhiteSpace(jwt.Secret))
    throw new InvalidOperationException("JwtSettings:Secret is missing/empty");

builder.Services.AddSingleton(jwt);

// ---------------- DI: services ----------------
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<IScheduledService, ScheduledService>();
builder.Services.AddScoped<IWorkoutSessionService, WorkoutSessionService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// ---------------- Controllers + Swagger ----------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---------------- CORS ----------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMyReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ---------------- AuthN / AuthZ ----------------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; //in prod: true 
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ---------------- Build ----------------
var app = builder.Build();

// ---------------- Swagger / Dev page ----------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseSwagger();     
    app.UseSwaggerUI();   
}

app.UseCors("AllowMyReactApp");

app.UseAuthentication();
app.UseAuthorization();

// ---------------- Health ----------------
app.MapGet("/", () => Results.Ok(new { ok = true, name = "TrainingPlanner API" }));
app.MapGet("/health/db", async (AppDbContext db, CancellationToken ct) =>
{
    var can = await db.Database.CanConnectAsync(ct);
    return can ? Results.Ok(new { db = "ok" }) : Results.Problem("DB not reachable");
});

// ---------------- Controllers ----------------
app.MapControllers();

// ---------------- DB seed ----------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

app.Run();
public partial class Program { }