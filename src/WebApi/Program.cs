using System.Text;
using Application.Abstractions;
using Application.Services;
using Infrastructure.Auth;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using WebApi.Middleware;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using WebApi.Swagger;
using WebApi.Converters;

var builder = WebApplication.CreateBuilder(args);

// ---------------- EF Core ----------------
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(conn));
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AppDbContext>());

// ---------------- JWT settings (bind + DI) ----------------
var jwt = new JwtSettings();
builder.Configuration.GetSection(JwtSettings.SectionName).Bind(jwt); // "JwtSettings"
if (string.IsNullOrWhiteSpace(jwt.Secret))
    throw new InvalidOperationException("JwtSettings:Secret is missing/empty");
builder.Services.AddSingleton(jwt);

// ---------------- DI: services ----------------
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<IScheduledService, ScheduledService>();
builder.Services.AddScoped<IWorkoutSessionService, WorkoutSessionService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();
builder.Services.AddScoped<IFriendWorkoutService, FriendWorkoutService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUserValidationHelpers, UserValidationHelpers>();
builder.Services.AddHttpContextAccessor();

// ---------------- Controllers (bez globalnego [Authorize]) ----------------


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
    });
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid request body.",
            Instance = context.HttpContext.Request.Path
        };

        return new BadRequestObjectResult(problemDetails);
    };
});

// ---------------- Swagger ----------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "FitMate API", Version = "v1" });
    
    c.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });
    c.MapType<TimeOnly>(() => new OpenApiSchema { Type = "string", Format = "time", Example = new Microsoft.OpenApi.Any.OpenApiString("18:30") });
    
    c.AddServer(new OpenApiServer
    {
        Url = "http://localhost:8080",
        Description = "Local development server"
    });

    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Wklej SAM token (bez 'Bearer ')"
    });

    c.OperationFilter<SecurityRequirementsOperationFilter>();
    
    c.OperationFilter<DefaultErrorResponsesOperationFilter>();
    
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});


// ---------------- CORS ----------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.AllowAnyOrigin() 
            .AllowAnyMethod()
            .AllowAnyHeader();
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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.ConfigObject.AdditionalItems["persistAuthorization"] = true;
    });
}


if (app.Environment.IsDevelopment())
{
    app.UseCors("DevelopmentPolicy");
}
else
{
    app.UseCors(policy => policy
        .WithOrigins(builder.Configuration["AllowedOrigins"]?.Split(',') ?? Array.Empty<string>())
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

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
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, hasher);
}

app.Run();
public partial class Program { }