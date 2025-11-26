# FitMateBackend - Dokumentacja Warstwy Infrastructure

## Spis Treści
1. [Przegląd](#przegląd)
2. [Architektura](#architektura)
3. [Persistence (Entity Framework Core)](#persistence-entity-framework-core)
4. [Security](#security)
5. [Konfiguracje Encji](#konfiguracje-encji)
6. [Migracje](#migracje)
7. [Data Seeding](#data-seeding)
8. [Dependency Injection](#dependency-injection)

---

## Przegląd

Warstwa Infrastructure zawiera implementacje szczegółów technicznych i integracje z zewnętrznymi frameworkami. Jest to jedyna warstwa, która zależy od konkretnych technologii (PostgreSQL, JWT, BCrypt).

### Odpowiedzialności
- ✅ **Dostęp do danych** - Entity Framework Core + PostgreSQL
- ✅ **Autentykacja** - JWT token generation
- ✅ **Bezpieczeństwo** - BCrypt password hashing
- ✅ **Konfiguracja** - Fluent API dla encji
- ✅ **Migracje** - EF Core migrations
- ✅ **Seeding** - Początkowe dane (role, admin)

###  Statystyki
- **DbContext**: 1 (AppDbContext)
- **Entity Configurations**: 14
- **Security Services**: 2 (TokenService, PasswordHasher)
- **Migrations**: 3
- **Dependency on Application**: Tak (interfejsy)

---

## Architektura

```
Infrastructure/
├── Auth/                      # Autentykacja JWT
│   ├── JwtSettings.cs        # Konfiguracja JWT
│   └── TokenService.cs       # Generowanie tokenów
│
├── Services/                  # Implementacje serwisów
│   └── PasswordHasher.cs     # BCrypt hashing
│
├── Persistence/               # Entity Framework Core
│   ├── AppDbContext.cs       # DbContext główny
│   ├── DbSeeder.cs           # Data seeding
│   └── DesignTimeDbContextFactory.cs  # Dla migrations
│
├── Configurations/            # Fluent API (13 plików)
│   ├── UserConfiguration.cs
│   ├── RoleConfiguration.cs
│   ├── PlanConfiguration.cs
│   ├── ScheduledWorkoutConfiguration.cs
│   ├── WorkoutSessionConfiguration.cs
│   ├── FriendshipConfiguration.cs
    ├── BodyMeasurementConfiguration.cs
    └── ... (6 więcej)
│
├── Migrations/                # EF Core migrations
│   ├── 20241120_Initial.cs
│   ├── 20241121_AddFriendships.cs
│   └── 20241122_AddSharedPlans.cs
│
└── Infrastructure.csproj
```

---

## Persistence (Entity Framework Core)

### AppDbContext

**Opis**: Główny DbContext aplikacji implementujący `IApplicationDbContext`.

```csharp
public class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<PlanExercise> PlanExercises => Set<PlanExercise>();
    public DbSet<PlanSet> PlanSets => Set<PlanSet>();
    public DbSet<ScheduledWorkout> ScheduledWorkouts => Set<ScheduledWorkout>();
    public DbSet<ScheduledExercise> ScheduledExercises => Set<ScheduledExercise>();
    public DbSet<ScheduledSet> ScheduledSets => Set<ScheduledSet>();
    public DbSet<WorkoutSession> WorkoutSessions { get; set; }
    public DbSet<SessionExercise> SessionExercises { get; set; }
    public DbSet<SessionSet> SessionSets { get; set; }
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<SharedPlan> SharedPlans => Set<SharedPlan>();
    public DbSet<BodyMeasurement> BodyMeasurements => Set<BodyMeasurement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Automatyczne application wszystkich konfiguracji z assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
```

**Kluczowe Cechy**:
1. **Property injection style** - `=> Set<T>()` vs `{ get; set; }`
2. **Auto-configuration** - `ApplyConfigurationsFromAssembly`
3. **Implementuje IApplicationDbContext** - Dependency Inversion z Application layer
4. **Primary constructor** (C# 12) - `AppDbContext(DbContextOptions<AppDbContext> options)`

### Connection String

**appsettings.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=fitmate;Username=postgres;Password=postgres"
  }
}
```

**Environment Variables (Production)**:
```bash
ConnectionStrings__DefaultConnection="Host=prod-db;Database=fitmate_prod;..."
```

### Database Provider: PostgreSQL

**Dlaczego PostgreSQL?**
- ✅ Open source, enterprise-grade
- ✅ JSONB support (przyszłe rozszerzenia)
- ✅ Full-text search capabilities
- ✅ `citext` typ dla case-insensitive strings
- ✅ Array types, custom types
- ✅ Excellent performance

**Konfiguracja**:
```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString,
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null)));
```

### DesignTimeDbContextFactory

**Opis**: Factory dla narzędzi EF Core (migrations, scaffold).

```csharp
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var builder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        builder.UseNpgsql(connectionString);

        return new AppDbContext(builder.Options);
    }
}
```

**Użycie**:
```bash
# Migrations używają tego factory
dotnet ef migrations add MigrationName
dotnet ef database update
```

---

## Security

### 1. TokenService (JWT)

**Opis**: Generowanie Access i Refresh tokenów JWT.

```csharp
public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenService(JwtSettings settings) => _settings = settings;

    public (string token, DateTime expiresAtUtc) CreateAccessToken(
        User user, IEnumerable<string> roles)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("preferred_username", user.UserName),
            new(ClaimTypes.Name, user.UserName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return (jwt, expires);
    }

    public (string token, DateTime expiresAtUtc) CreateRefreshToken()
    {
        // Secure random 64-byte token
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);

        var token = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');  // URL-safe base64

        var expires = DateTime.UtcNow.AddDays(_settings.RefreshTokenDays);
        return (token, expires);
    }
}
```

**Claims w Access Token**:
- `NameIdentifier` / `sub` - User ID (GUID)
- `email` - Email użytkownika
- `preferred_username` - Username
- `name` - Username (alias)
- `jti` - JWT ID (unique token identifier)
- `iat` - Issued At timestamp
- `role` - Wszystkie role użytkownika (Admin, User)

**Refresh Token**:
- 64-byte cryptographically secure random
- URL-safe Base64 encoding
- Przechowywany w bazie (RefreshTokens table)

### JwtSettings

**Konfiguracja**:
```csharp
public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public string Secret { get; set; } = null!; // minimum 32 chars
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}
```

**appsettings.json**:
```json
{
  "JwtSettings": {
    "Issuer": "FitMateAPI",
    "Audience": "FitMateClient",
    "Secret": "your-very-long-secret-key-at-least-32-characters-long",
    "AccessTokenMinutes": 60,
    "RefreshTokenDays": 7
  }
}
```

**Production Best Practices**:
- Secret key: minimum 256 bits (32 characters)
- Przechowuj Secret w Environment Variables lub Key Vault
- Rotate secrets regularnie
- AccessTokenMinutes: 15-60 minut
- RefreshTokenDays: 7-30 dni

### 2. PasswordHasher (BCrypt)

**Opis**: Hashowanie i weryfikacja haseł używając BCrypt.

```csharp
public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
```

**BCrypt Advantages**:
- ✅ **Adaptive** - Można zwiększyć work factor w przyszłości
- ✅ **Salt** - Automatycznie generowany i included w hash
- ✅ **Slow by design** - Odporność na brute force
- ✅ **Industry standard** - Widely vetted, secure

**Hash Format**:
```
$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy
│  │  │                             └── hash (31 chars)
│  │  └── salt (22 chars)
│  └── cost factor (10 = 2^10 = 1024 rounds)
└── algorithm version (2a = BCrypt)
```

**Default Work Factor**: 10 (BCrypt.Net default)
- Można zwiększyć: `BCrypt.HashPassword(password, workFactor: 12)`
- Work factor 10 = ~100ms per hash (2024 hardware)
- Work factor 12 = ~400ms per hash

---

## Konfiguracje Encji

### Fluent API Pattern

Każda encja ma dedykowany plik konfiguracji implementujący `IEntityTypeConfiguration<T>`.

**Zalety podejścia**:
- ✅ Separacja concerns (konfiguracja oddzielona od encji)
- ✅ Łatwiejsze testowanie
- ✅ Clean Domain models (POCO)
- ✅ Lepsze reuse i maintainability

### Przykład: UserConfiguration

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasMaxLength(200)
            .IsRequired()
            .HasColumnType("citext");  // Case-insensitive text (PostgreSQL)

        builder.Property(u => u.UserName)
            .HasMaxLength(50)
            .IsRequired()
            .HasColumnType("citext");

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(255)
            .IsRequired();

        // Indexes
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.UserName).IsUnique();

        // Check Constraint (PostgreSQL regex)
        builder.HasCheckConstraint("CK_users_email_format",
            "\"Email\" ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}$'");
    }
}
```

**Kluczowe Elementy**:
- `ToTable("users")` - nazwa tabeli (snake_case)
- `HasColumnType("citext")` - PostgreSQL case-insensitive type
- `HasIndex().IsUnique()` - Unique constraint
- `HasCheckConstraint()` - Database-level validation

### Przykład: WorkoutSessionConfiguration

```csharp
public class WorkoutSessionConfiguration : IEntityTypeConfiguration<WorkoutSession>
{
    public void Configure(EntityTypeBuilder<WorkoutSession> builder)
    {
        builder.ToTable("workout_sessions");
        builder.HasKey(ws => ws.Id);

        builder.Property(ws => ws.StartedAtUtc)
            .IsRequired();

        builder.Property(ws => ws.Status)
            .HasConversion<string>()  // Enum → string
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ws => ws.SessionNotes)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(ws => ws.User)
            .WithMany()
            .HasForeignKey(ws => ws.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ws => ws.Scheduled)
            .WithMany()
            .HasForeignKey(ws => ws.ScheduledId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ws => ws.Exercises)
            .WithOne(e => e.Session)
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(ws => ws.UserId);
        builder.HasIndex(ws => ws.ScheduledId);
        builder.HasIndex(ws => ws.StartedAtUtc);
    }
}
```

**Delete Behaviors**:
- `Restrict` - Zapobiega usunięciu (wymaga ręcznego cleanup)
- `Cascade` - Automatyczne usunięcie powiązanych rekordów
- `SetNull` - Ustawia FK na NULL

### Przykład: FriendshipConfiguration

```csharp
public class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> builder)
    {
        builder.ToTable("friendships");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Relationships - 3 foreign keys w jednej tabeli
        builder.HasOne(f => f.UserA)
            .WithMany()
            .HasForeignKey(f => f.UserAId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.UserB)
            .WithMany()
            .HasForeignKey(f => f.UserBId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.RequestedByUser)
            .WithMany()
            .HasForeignKey(f => f.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint - tylko jedna friendship per para
        builder.HasIndex(f => new { f.UserAId, f.UserBId }).IsUnique();

        // Check constraint - UserAId < UserBId
        builder.HasCheckConstraint("CK_friendships_user_order",
            "\"UserAId\" < \"UserBId\"");
    }
}
```

### Konwencje Konfiguracji

**Naming Conventions**:
- Tabele: `snake_case` (users, workout_sessions, plan_exercises)
- Kolumny: `PascalCase` (domyślnie, EF mapuje na snake_case w PostgreSQL)
- Indexes: `IX_{table}_{column}` (auto-generated)
- Constraints: `CK_{table}_{description}`, `FK_{table}_{reference}`

**Common Patterns**:
```csharp
// String columns
.HasMaxLength(200)
.IsRequired()

// Enum → String
.HasConversion<string>()

// Foreign Key
.HasOne(x => x.Parent)
.WithMany(p => p.Children)
.HasForeignKey(x => x.ParentId)
.OnDelete(DeleteBehavior.Cascade)

// Unique Index
.HasIndex(x => x.Email).IsUnique()

// Composite Index
.HasIndex(x => new { x.UserId, x.Date })
```

---

## Migracje

### EF Core Migrations

**Tworzenie migracji**:
```bash
# Z poziomu Infrastructure project
dotnet ef migrations add MigrationName --startup-project ../WebApi

# Lub z root directory
dotnet ef migrations add MigrationName --project src/Infrastructure --startup-project src/WebApi
```

**Update bazy danych**:
```bash
dotnet ef database update --startup-project ../WebApi
```

**Rollback**:
```bash
# Cofnij do konkretnej migracji
dotnet ef database update PreviousMigrationName --startup-project ../WebApi

# Cofnij wszystko
dotnet ef database update 0 --startup-project ../WebApi
```

**Usunięcie ostatniej migracji** (jeśli nie została apply):
```bash
dotnet ef migrations remove --startup-project ../WebApi
```

### Migration History

**Migracje w projekcie**:
1. `20241120_Initial` - Początkowa struktura (Users, Plans, Sessions)
2. `20241121_AddFriendships` - Dodanie Friendship
3. `20241122_AddSharedPlans` - Dodanie SharedPlan
4. `20241126_AddTargetWeightAndBiometricsPrivacyToUser` - Dodanie TargetWeight i Privacy do User

### Automatyczne Migracje przy Starcie (Development)

**Program.cs**:
```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    if (app.Environment.IsDevelopment())
    {
        await db.Database.MigrateAsync(); // Auto-apply pending migrations
    }
    
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await DbSeeder.SeedAsync(db, passwordHasher);
}
```

**Production**: Nigdy nie używaj auto-migrate! Wykonuj migrations ręcznie lub przez CI/CD.

---

## Data Seeding

### DbSeeder

**Opis**: Seeding początkowych danych (role, admin user, demo plan).

```csharp
public static class DbSeeder
{
    private const string AdminEmail = "admin@fitmate.local";
    private const string AdminUserName = "admin";
    private const string DefaultPassword = "Admin123!";
    private const string AdminRoleName = "Admin";
    private const string UserRoleName = "User";

    public static async Task SeedAsync(
        AppDbContext db, 
        IPasswordHasher passwordHasher, 
        CancellationToken ct = default)
    {
        // 1. Seed Roles
        if (!await db.Roles.AnyAsync(ct))
        {
            db.Roles.AddRange(
                new Role { Id = Guid.NewGuid(), Name = UserRoleName },
                new Role { Id = Guid.NewGuid(), Name = AdminRoleName }
            );
            await db.SaveChangesAsync(ct);
        }

        // 2. Seed Admin User
        var admin = await db.Users
            .FirstOrDefaultAsync(u => u.Email == AdminEmail, ct);
            
        if (admin is null)
        {
            admin = new User
            {
                Id = Guid.NewGuid(),
                Email = AdminEmail,
                PasswordHash = passwordHasher.HashPassword(DefaultPassword),
                FullName = "FitMate Admin",
                UserName = AdminUserName
            };
            db.Users.Add(admin);
            await db.SaveChangesAsync(ct);

            // Assign Admin role
            var adminRoleId = await db.Roles
                .Where(r => r.Name == AdminRoleName)
                .Select(r => r.Id)
                .FirstAsync(ct);
                
            db.UserRoles.Add(new UserRole 
            { 
                UserId = admin.Id, 
                RoleId = adminRoleId 
            });
            await db.SaveChangesAsync(ct);
        }

        // 3. Seed Demo Plan (optional)
        if (!await db.Plans.AnyAsync(ct))
        {
            db.Plans.Add(new Plan
            {
                Id = Guid.NewGuid(),
                PlanName = "Demo FBW",
                Type = "FBW",
                Notes = "Seed plan",
                CreatedByUserId = admin.Id
            });
            await db.SaveChangesAsync(ct);
        }
    }
}
```

**Seeded Data**:
- ✅ **Roles**: "User", "Admin"
- ✅ **Admin User**: admin@fitmate.local / Admin123!
- ✅ **Demo Plan**: Prosty plan FBW

**Idempotent**: Każde wywołanie sprawdza czy dane już istnieją, nie duplikuje.

---

## Dependency Injection

### Rejestracja w Program.cs

```csharp
// Infrastructure services registration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null)));

// Register DbContext as IApplicationDbContext
builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<AppDbContext>());

// JWT Settings
var jwtSettings = new JwtSettings();
builder.Configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);
builder.Services.AddSingleton(jwtSettings);

// Infrastructure services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
```

### JWT Authentication Configuration

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ClockSkew = TimeSpan.Zero  // No tolerance for expiration
    };
});

builder.Services.AddAuthorization();
```

---

## Technologie i Pakiety

### NuGet Packages

**Core**:
- `Microsoft.EntityFrameworkCore` (8.0.x)
- `Npgsql.EntityFrameworkCore.PostgreSQL` (8.0.x)
- `Microsoft.EntityFrameworkCore.Design` (for migrations)

**Security**:
- `BCrypt.Net-Next` (bcrypt hashing)
- `System.IdentityModel.Tokens.Jwt` (JWT generation)
- `Microsoft.AspNetCore.Authentication.JwtBearer` (JWT validation)

**Configuration**:
- `Microsoft.Extensions.Configuration` (settings binding)

### Database: PostgreSQL 15

**Features Used**:
- `citext` - Case-insensitive text type
- Regex check constraints (`~*` operator)
- JSONB (potencjalnie w przyszłości)
- Full-text search capabilities (potencjalnie)

**Setup (Docker)**:
```bash
docker run --name fitmate-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=fitmate \
  -p 5432:5432 \
  -d postgres:15-alpine
```

**Enable citext extension**:
```sql
CREATE EXTENSION IF NOT EXISTS citext;
```

---

## Podsumowanie

Warstwa Infrastructure FitMateBackend jest:
- ✅ **Kompletna** - Wszystkie implementacje interfejsów z Application
- ✅ **Secure** - BCrypt + JWT z best practices
- ✅ **Konfigur owalna** - Fluent API dla wszystkich encji
- ✅ **Production-ready** - Migracje, seeding, error handling
- ✅ **PostgreSQL** - Enterprise-grade database
- ✅ **Testowana** - 113 integration tests z Testcontainers

**Key Takeaways**:
1. **Clean Architecture** - Infrastructure depends on Application (Dependency Inversion)
2. **EF Core Fluent API** - Czyste POCO entities, konfiguracja w osobnych plikach
3. **BCrypt** - Industry standard password hashing
4. **JWT** - Stateless authentication z refresh tokens
5. **PostgreSQL** - Potężne features (citext, constraints, extensions)
6. **Migrations** - Pełna kontrola nad schema changes

Warstwa jest gotowa do produkcji i łatwa w rozszerzaniu.
