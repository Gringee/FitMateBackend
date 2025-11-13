using System.Text;
using Application.Abstractions;
using Infrastructure.Auth;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;

var builder = WebApplication.CreateBuilder(args);

// ---------------- EF Core ----------------
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(conn));

// ---------------- JWT settings (bind + DI) ----------------
var jwt = new JwtSettings();
builder.Configuration.GetSection(JwtSettings.SectionName).Bind(jwt); // "JwtSettings"
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
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();
builder.Services.AddHttpContextAccessor();

// ---------------- Controllers (bez globalnego [Authorize]) ----------------
builder.Services.AddControllers();

// ---------------- Swagger ----------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "FitMate API", Version = "v1" });

    // NAZWA MUSI BYĆ "oauth2", bo taką nazwę generuje SecurityRequirementsOperationFilter
    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Wklej SAM token (bez 'Bearer ')"
    });
    
    c.OperationFilter<SecurityRequirementsOperationFilter>();
});


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
    .AddJwtBearer(opt =>
    {
        opt.RequireHttpsMetadata = false; // w prod: true + HTTPS
        opt.TokenValidationParameters = new TokenValidationParameters
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
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.ConfigObject.AdditionalItems["persistAuthorization"] = true;
});

app.UseCors("AllowMyReactApp");

app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (UnauthorizedAccessException)
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
    }
});

app.UseAuthentication();
app.UseAuthorization();

// ---------------- Health (PUBLIC minimal APIs) ----------------
app.MapGet("/", () => Results.Ok(new { ok = true, name = "TrainingPlanner API" }))
   .AllowAnonymous();

app.MapGet("/health/db", async (AppDbContext db, CancellationToken ct) =>
{
    var can = await db.Database.CanConnectAsync(ct);
    return can ? Results.Ok(new { db = "ok" }) : Results.Problem("DB not reachable");
}).AllowAnonymous();

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