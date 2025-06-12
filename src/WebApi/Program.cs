using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Infrastructure;                              
using Infrastructure.Repositories;                 
using Domain.Interfaces;                            
using Application.Services;
using Application.Interfaces;
using Application.Intertfaces;

var builder = WebApplication.CreateBuilder(args);

// Pobranie ustawieñ JWT z appsettings.json
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

//Rejestracja us³ug CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

//Rejestracja kontrolerów
builder.Services.AddControllers();

//Rejestracja Swagger / OpenAPI (raz, z wczytaniem XML)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FitMate API",
        Version = "v1",
        Description = "API aplikacji FitMate"
    });

    //Konfiguracja JWT w Swaggerze
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer. WprowadŸ 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    //Wczytanie komentarzy XML (opcjonalnie, ale potrzebne, ¿eby opisy w kontrolerach pokazywa³y siê w Swagger UI)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

//Rejestracja DbContext (PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

//Rejestracja repozytoriów i serwisów
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWorkoutRepository, WorkoutRepository>();
builder.Services.AddScoped<IExerciseRepository, ExerciseRepository>();
builder.Services.AddScoped<IExerciseRepository, ExerciseRepository>();
builder.Services.AddScoped<IWorkoutService, WorkoutService>();
builder.Services.AddScoped<IExerciseService, ExerciseService>();

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthService>();

//Uwierzytelnianie JWT
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization();

//Budowa aplikacji
var app = builder.Build();

//Konfiguracja pipeline HTTP

//W³¹czenie Swaggera w trybie Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FitMate API v1");
        c.RoutePrefix = "swagger"; // Swagger UI dostêpne pod /swagger
    });
}

//Redirect do HTTPS
app.UseHttpsRedirection();

//W³¹czenie CORS
app.UseCors("AllowLocalhost");

//Uwierzytelnianie i autoryzacja
app.UseAuthentication();
app.UseAuthorization();

//Mapowanie kontrolerów (routing)
app.MapControllers();

//Uruchomienie
app.Run();
