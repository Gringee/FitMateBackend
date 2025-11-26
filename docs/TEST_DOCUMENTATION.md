# FitMateBackend - Dokumentacja Testów

## Spis Treści
1. [Przegląd](#przegląd)
2. [Struktura Testów](#struktura-testów)
3. [Uruchamianie Testów](#uruchamianie-testów)
4. [Testy Jednostkowe](#testy-jednostkowe)
5. [Testy Integracyjne](#testy-integracyjne)
6. [Pokrycie Testami](#pokrycie-testami)
7. [Konwencje i Wzorce](#konwencje-i-wzorce)
8. [Dodawanie Nowych Testów](#dodawanie-nowych-testów)

---

## Przegląd

### Statystyki
- **Wszystkie testy**: 349/349 (100% ✅)
- **Testy jednostkowe**: 226/226 (100%)
- **Testy integracyjne**: 123/123 (100%)
- **Pokrycie kodu (Overall)**: 40.7%
- **Pokrycie kodu (Application)**: 86.5% line / 64.2% branch

### Technologie
- **Framework testowy**: xUnit
- **Asercje**: FluentAssertions
- **Mockowanie**: Moq
- **Baza danych (unit tests)**: Entity Framework Core InMemory
- **Baza danych (integration tests)**: Testcontainers.PostgreSql
- **Autentykacja**: JWT tokens w testach integracyjnych

---

## Struktura Testów

```
tests/
├── Application.UnitTests/              # Testy jednostkowe warstwy Application
│   ├── Services/                       # Testy serwisów
│   │   ├── AnalyticsServiceTests.cs
│   │   ├── AuthServiceTests.cs
│   │   ├── BodyMetricsServiceTests.cs
│   │   ├── FriendshipServiceTests.cs
│   │   ├── FriendWorkoutServiceTests.cs
│   │   ├── PlanServiceTests.cs
│   │   ├── ScheduledServiceTests.cs
│   │   ├── UserAdminServiceTests.cs
│   │   ├── UserProfileServiceTests.cs
│   │   ├── UserProfileTargetWeightTests.cs 
│   │   ├── UserServiceTests.cs
│   │   └── WorkoutSessionServiceTests.cs
│   └── Application.UnitTests.csproj
│
└── WebApi.IntegrationTests/            # Testy integracyjne API
    ├── Common/                          # Klasy wspólne
    │   ├── BaseIntegrationTest.cs      # Klasa bazowa dla testów
    │   └── IntegrationTestWebAppFactory.cs  # Factory z Testcontainers
    ├── Controllers/                     # Testy kontrolerów
    │   ├── AnalyticsControllerTests.cs
    │   ├── AuthControllerTests.cs
    │   ├── BodyMetricsControllerTests.cs
    │   ├── FriendsControllerTests.cs
    │   ├── FriendsWorkoutsControllerTests.cs
    │   ├── PlansControllerTests.cs
    │   ├── ScheduledControllerTests.cs
    │   ├── SessionsControllerTests.cs
    │   ├── UserProfileControllerTests.cs
    │   └── UsersControllerTests.cs
    └── WebApi.IntegrationTests.csproj
```

---

## Uruchamianie Testów

### Wszystkie testy
```bash
dotnet test
```

### Tylko testy jednostkowe
```bash
dotnet test tests/Application.UnitTests
```

### Tylko testy integracyjne
```bash
dotnet test tests/WebApi.IntegrationTests
```

### Konkretna klasa testowa
```bash
dotnet test --filter "FullyQualifiedName~AuthServiceTests"
```

### Konkretny test
```bash
dotnet test --filter "FullyQualifiedName~AuthServiceTests.LoginAsync_ShouldReturnTokens_WhenValidCredentials"
```

### Z szczegółowym outputem
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Z pokryciem kodu (wymaga coverlet)
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## Testy Jednostkowe

### Przegląd
Testy jednostkowe weryfikują logikę biznesową w warstwie `Application` w izolacji od zewnętrznych zależności.

### Charakterystyka
- ✅ Szybkie wykonanie (~700ms dla wszystkich 206 testów)
- ✅ Pełna izolacja (baza InMemory, mockowane zależności)
- ✅ Deterministyczne wyniki
- ✅ Testują pojedyncze metody/scenariusze

### Struktura Testu Jednostkowego

```csharp
public class ServiceNameTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IDependency> _dependencyMock;
    private readonly ServiceName _sut; // System Under Test

    public ServiceNameTests()
    {
        // Setup InMemory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _dependencyMock = new Mock<IDependency>();
        
        _sut = new ServiceName(_dbContext, _dependencyMock.Object);
    }

    [Fact]
    public async Task MethodName_ShouldReturnExpected_WhenCondition()
    {
        // Arrange
        var input = CreateTestData();
        _dependencyMock.Setup(x => x.Method()).Returns(expectedValue);

        // Act
        var result = await _sut.MethodName(input);

        // Assert
        result.Should().NotBeNull();
        result.Property.Should().Be(expectedValue);
    }
}
```

### Przykłady Testów Jednostkowych

#### 1. AuthService - LoginAsync
```csharp
[Fact]
public async Task LoginAsync_ShouldReturnTokens_WhenValidCredentials()
{
    // Arrange
    var userRole = new Role { Id = Guid.NewGuid(), Name = "User" };
    var userId = Guid.NewGuid();
    var user = new User
    {
        Id = userId,
        Email = "login@example.com",
        UserName = "loginuser",
        PasswordHash = "hashed_password",
        FullName = "Login User",
        UserRoles = new List<UserRole>
        {
            new UserRole { UserId = userId, RoleId = userRole.Id, Role = userRole }
        }
    };

    await _dbContext.Roles.AddAsync(userRole);
    await _dbContext.Users.AddAsync(user);
    await _dbContext.SaveChangesAsync();

    var request = new LoginRequest
    {
        UserNameOrEmail = "loginuser",
        Password = "correct_password"
    };

    _passwordHasherMock.Setup(x => x.VerifyPassword("correct_password", "hashed_password"))
        .Returns(true);

    var expiresAt = DateTime.UtcNow.AddHours(1);
    var refreshExpires = DateTime.UtcNow.AddDays(7);
    
    _tokenServiceMock.Setup(x => x.CreateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()))
        .Returns(("access_token_login", expiresAt));
    _tokenServiceMock.Setup(x => x.CreateRefreshToken())
        .Returns(("refresh_token_login", refreshExpires));

    // Act
    var result = await _sut.LoginAsync(request, CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result.AccessToken.Should().Be("access_token_login");
    result.RefreshToken.Should().Be("refresh_token_login");
}
```

#### 2. UserAdminService - DeleteUserAsync
```csharp
[Fact]
public async Task DeleteUserAsync_ShouldReturnIsAdmin_WhenUserIsAdmin()
{
    // Arrange
    var userId = Guid.NewGuid();
    var adminRoleId = Guid.NewGuid();
    
    var user = new User { Id = userId, UserName = "admin", Email = "admin@test.com", FullName = "Admin", PasswordHash = "hash" };
    var adminRole = new Role { Id = adminRoleId, Name = "Admin" };
    var userRole = new UserRole { UserId = userId, RoleId = adminRoleId, User = user, Role = adminRole };
    
    user.UserRoles.Add(userRole);
    
    await _dbContext.Users.AddAsync(user);
    await _dbContext.Roles.AddAsync(adminRole);
    await _dbContext.SaveChangesAsync();

    // Act
    var result = await _sut.DeleteUserAsync(userId, CancellationToken.None);

    // Assert
    result.Should().Be(DeleteUserResult.IsAdmin);
    var userStillExists = await _dbContext.Users.FindAsync(userId);
    userStillExists.Should().NotBeNull(); // Should not be deleted
}
```

---

## Testy Integracyjne

### Przegląd
Testy integracyjne weryfikują całe API end-to-end z prawdziwą bazą danych PostgreSQL.

### Charakterystyka
- ✅ Testują pełny przepływ HTTP request → response
- ✅ Prawdziwa baza danych (PostgreSQL w Dockerze via Testcontainers)
- ✅ Autentykacja i autoryzacja
- ✅ Walidacja DTO
- ✅ Kody statusu HTTP

### Infrastruktura

#### IntegrationTestWebAppFactory
```csharp
public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _dbContainer;

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("fitmate_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _dbContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Replace AppDbContext with test database
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer!.GetConnectionString());
            });

            // Override JWT settings for testing
            services.Configure<JwtSettings>(config =>
            {
                config.SecretKey = "test-secret-key-minimum-256-bits-long";
                config.Issuer = "TestIssuer";
                config.Audience = "TestAudience";
                config.ExpirationMinutes = 60;
            });
        });
    }
}
```

#### BaseIntegrationTest
```csharp
public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>
{
    protected readonly HttpClient Client;
    protected readonly IntegrationTestWebAppFactory Factory;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }
}
```

### Przykłady Testów Integracyjnych

#### 1. AuthController - Register z walidacją
```csharp
[Fact]
public async Task Register_ShouldReturn400BadRequest_WhenUsernameTaken()
{
    // Arrange - Register first user
    var firstRequest = new RegisterRequest
    {
        Email = "first@example.com",
        UserName = "duplicateuser",
        Password = "SecurePass123!",
        FullName = "First User"
    };
    await Client.PostAsJsonAsync("/api/auth/register", firstRequest);

    // Try to register with same username
    var secondRequest = new RegisterRequest
    {
        Email = "second@example.com",
        UserName = "duplicateuser",
        Password = "SecurePass123!",
        FullName = "Second User"
    };

    // Act
    var response = await Client.PostAsJsonAsync("/api/auth/register", secondRequest);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}
```

#### 2. SessionsController - Start session
```csharp
[Fact]
public async Task StartSession_ShouldReturn200OK_WhenScheduledWorkoutExists()
{
    // Arrange
    var token = await RegisterAndGetTokenAsync();
    Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Create plan and scheduled workout
    var planId = await CreatePlanAsync();
    var scheduledId = await CreateScheduledWorkoutAsync(planId);

    var request = new StartSessionRequest { ScheduledWorkoutId = scheduledId };

    // Act
    var response = await Client.PostAsJsonAsync("/api/sessions/start", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var session = await response.Content.ReadFromJsonAsync<SessionDto>();
    session.Should().NotBeNull();
    session!.Status.Should().Be("in_progress");
}
```

#### 3. UsersController - Admin operations
```csharp
[Fact]
public async Task AssignRole_ShouldReturn204NoContent_WhenValid()
{
    // Arrange
    var adminToken = await GetAdminTokenAsync();
    Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

    // Create a user
    var createDto = new CreateUserDto
    {
        UserName = $"roleuser_{Guid.NewGuid().ToString().Substring(0, 8)}",
        Email = $"roleuser_{Guid.NewGuid()}@test.com",
        FullName = "Role User"
    };
    var createResponse = await Client.PostAsJsonAsync("/api/users", createDto);
    var user = await createResponse.Content.ReadFromJsonAsync<UserDto>();

    // Act
    var response = await Client.PutAsync($"/api/users/{user!.Id}/role?roleName=User", null);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.NoContent);
}
```

---

## Pokrycie Testami

### Serwisy (Testy Jednostkowe)

| Serwis | Testy | Scenariusze | Status |
|--------|-------|-------------|--------|
| **AnalyticsService** | 15 | GetOverview, GetVolume (day/week/exercise), GetE1RM, GetAdherence, GetPlanVsActual | ✅ 100% |
| **AuthService** | 10 | Register (success, duplicate user/email), Login (success, wrong password, user not found), Refresh, Logout | ✅ 100% |
| **FriendshipService** | 12 | SendRequest, RespondRequest (accept/reject), GetRequests (incoming/outgoing), GetFriends, AreFriends | ✅ 100% |
| **FriendWorkoutService** | 6 | GetFriendsScheduled (visibility, date range), GetFriendsSessions, filtering | ✅ 100% |
| **PlanService** | 8 | Create, GetById, GetAll (with type filter), Delete, Duplicate | ✅ 100% |
| **ScheduledService** | 12 | Create, GetById, GetAll (with date/status filters), Update, Delete | ✅ 100% |
| **UserAdminService** | 9 | AssignRole (all result types), DeleteUser (admin protection), ResetPassword | ✅ 100% |
| **UserProfileService** | 8 | GetCurrent, UpdateProfile (validation), ChangePassword | ✅ 100% |
| **UserService** | 7 | GetAll (with search), Create (validation), Update | ✅ 100% |
| **WorkoutSessionService** | 16 | Start, GetById, GetAll, AddExercise, PatchSet, Complete, Abort (all scenarios) | ✅ 100% |

### Kontrolery (Testy Integracyjne)

| Kontroler | Testy | Kluczowe Scenariusze | Status |
|-----------|-------|----------------------|--------|
| **AnalyticsController** | 14 | Wszystkie endpointy analytics z walidacją parametrów | ✅ 100% |
| **AuthController** | 12 | Register, Login, Refresh, Logout + wszystkie scenariusze błędów | ✅ 100% |
| **FriendsController** | 12 | SendRequest, Respond, GetFriends, Delete + błędy (duplicate, not found) | ✅ 100% |
| **FriendsWorkoutsController** | 6 | GetScheduled, GetSessions + visibility filtering | ✅ 100% |
| **PlansController** | 12 | CRUD operations + ownership validation | ✅ 100% |
| **ScheduledController** | 13 | CRUD operations + date filtering | ✅ 100% |
| **SessionsController** | 18 | Start, AddExercise, PatchSet, Complete, Abort + wszystkie błędy | ✅ 100% |
| **UserProfileController** | 9 | GetMe, UpdateMe (validation), ChangePassword | ✅ 100% |
| **UsersController** | 16 | Admin: GetAll, Create, Update, AssignRole, Delete, ResetPassword | ✅ 100% |

---

## Konwencje i Wzorce

### Nazewnictwo Testów

#### Format
```
MethodName_ShouldExpectedBehavior_WhenCondition
```

#### Przykłady
```csharp
✅ CreateAsync_ShouldCreateUser_WhenValid
✅ LoginAsync_ShouldReturnNull_WhenPasswordIncorrect
✅ DeleteAsync_ShouldReturn404NotFound_WhenPlanDoesNotExist
✅ GetAll_ShouldReturn401Unauthorized_WhenNotAuthenticated
```

### Struktura AAA (Arrange-Act-Assert)

```csharp
[Fact]
public async Task MethodName_ShouldBehavior_WhenCondition()
{
    // Arrange - Przygotowanie danych testowych
    var input = new InputDto { /* ... */ };
    var expectedResult = /* ... */;

    // Act - Wywołanie testowanej metody
    var result = await _sut.MethodName(input);

    // Assert - Sprawdzenie wyników
    result.Should().NotBeNull();
    result.Property.Should().Be(expectedResult);
}
```

### FluentAssertions - Najczęściej Używane

```csharp
// Equality
result.Should().Be(expected);
result.Should().NotBe(unexpected);

// Nullability
result.Should().NotBeNull();
result.Should().BeNull();

// Collections
list.Should().HaveCount(5);
list.Should().Contain(item);
list.Should().NotContain(item);
list.Should().BeEmpty();
list.Should().OnlyContain(x => x.Status == "active");

// Strings
str.Should().StartWith("prefix");
str.Should().EndWith("suffix");
str.Should().Contain("substring");
str.Should().Match("test_*");

// Booleans
result.Should().BeTrue();
result.Should().BeFalse();

// Exceptions
Func<Task> act = async () => await _sut.Method();
await act.Should().ThrowAsync<InvalidOperationException>()
    .WithMessage("Expected message");

// HTTP Status
response.StatusCode.Should().Be(HttpStatusCode.OK);
response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
```

### Mockowanie z Moq

```csharp
// Setup return value
_mockService.Setup(x => x.GetData(It.IsAny<int>()))
    .Returns(expectedData);

// Setup async method
_mockService.Setup(x => x.GetDataAsync(It.IsAny<int>()))
    .ReturnsAsync(expectedData);

// Setup with specific parameter
_mockService.Setup(x => x.Process(5))
    .Returns(true);

// Setup to throw exception
_mockService.Setup(x => x.ValidateAsync(It.IsAny<string>()))
    .ThrowsAsync(new InvalidOperationException("Validation failed"));

// Verify method was called
_mockService.Verify(x => x.Method(), Times.Once);
_mockService.Verify(x => x.Method(It.IsAny<int>()), Times.Never);
```

---

## Dodawanie Nowych Testów

### 1. Testy Jednostkowe dla Nowego Serwisu

```csharp
// tests/Application.UnitTests/Services/NewServiceTests.cs
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Application.UnitTests.Services;

public class NewServiceTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IDependency> _dependencyMock;
    private readonly NewService _sut;

    public NewServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new AppDbContext(options);
        _dependencyMock = new Mock<IDependency>();
        
        _sut = new NewService(_dbContext, _dependencyMock.Object);
    }

    [Fact]
    public async Task MethodName_ShouldReturnExpected_WhenValid()
    {
        // Arrange
        var input = new InputDto();
        
        // Act
        var result = await _sut.MethodName(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }
}
```

### 2. Testy Integracyjne dla Nowego Kontrolera

```csharp
// tests/WebApi.IntegrationTests/Controllers/NewControllerTests.cs
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.DTOs;
using Application.DTOs.Auth;
using FluentAssertions;
using WebApi.IntegrationTests.Common;
using Xunit;

namespace WebApi.IntegrationTests.Controllers;

public class NewControllerTests : BaseIntegrationTest
{
    public NewControllerTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task EndpointName_ShouldReturnUnauthorized_WhenNotAuthenticated()
{
        // Act
        var response = await Client.GetAsync("/api/new-endpoint");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EndpointName_ShouldReturn200OK_WhenValid()
    {
        // Arrange
        var token = await RegisterAndGetTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.GetAsync("/api/new-endpoint");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<string> RegisterAndGetTokenAsync()
    {
        var registerRequest = new RegisterRequest
        {
            Email = $"test_{Guid.NewGuid()}@example.com",
            UserName = $"test_{Guid.NewGuid().ToString().Substring(0, 8)}",
            Password = "SecurePass123!",
            FullName = "Test User"
        };

        var response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.AccessToken;
    }
}
```

### 3. Checklist dla Nowych Testów

#### Testy Jednostkowe
- [ ] Utworzyć plik `ServiceNameTests.cs`
- [ ] Dodać setup z InMemory database
- [ ] Mock wszystkich zależności
- [ ] Test happy path (sukces)
- [ ] Testy edge cases (granice)
- [ ] Testy walidacji
- [ ] Testy błędów (exceptions)
- [ ] Sprawdzić czy wszystkie metody publiczne są pokryte

#### Testy Integracyjne
- [ ] Utworzyć plik `ControllerNameTests.cs`
- [ ] Dziedziczyć po `BaseIntegrationTest`
- [ ] Test 401 Unauthorized (jeśli wymaga auth)
- [ ] Test 403 Forbidden (jeśli ma role)
- [ ] Test happy path (200/201)
- [ ] Test 400 Bad Request (walidacja)
- [ ] Test 404 Not Found
- [ ] Test specyficznych przypadków biznesowych

---

## Rozwiązywanie Problemów

### Testy Jednostkowe Nie Przechodzą

#### Problem: InMemory database nie obsługuje ExecuteSqlRaw
```csharp
// ❌ Nie zadziała z InMemory
await _db.Database.ExecuteSqlRawAsync("DELETE FROM...");

// ✅ Rozwiązanie: Przenieś do testów integracyjnych
// lub użyj normalnych operacji EF Core
_db.RemoveRange(items);
await _db.SaveChangesAsync();
```

#### Problem: Transactions w InMemory
```csharp
// ⚠️ Transactions są ignorowane w InMemory
await _dbContext.Database.BeginTransactionAsync(); // Zwróci null

// ✅ Dodaj konfigurację ignorowania ostrzeżeń
.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
```

### Testy Integracyjne Nie Przechodzą

#### Problem: Testcontainers nie startuje
```bash
# Sprawdź czy Docker działa
docker ps

# Sprawdź logi kontenera
docker logs <container-id>
```

#### Problem: Konflikty w danych testowych
```csharp
// ❌ Użycie stałych wartości
UserName = "testuser" // Może konfliktować między testami

// ✅ Użyj unikalnych wartości
UserName = $"test_{Guid.NewGuid().ToString().Substring(0, 8)}"
```

#### Problem: Token JWT wygasł
```csharp
// Sprawdź konfigurację w IntegrationTestWebAppFactory
config.ExpirationMinutes = 60; // Wystarczająco długo dla testów
```

---

## Najlepsze Praktyki

### ✅ DO

1. **Izolacja testów** - każdy test jest niezależny
2. **Czytelne nazwy** - używaj konwencji `Method_Should_When`
3. **AAA pattern** - Arrange, Act, Assert
4. **FluentAssertions** - dla lepszych komunikatów błędów
5. **Unikalne dane** - użyj `Guid.NewGuid()` dla unikalności
6. **Testuj tylko publiczne API** - nie testuj prywatnych metod
7. **Jeden koncept na test** - nie łącz wielu scenariuszy

### ❌ DON'T

1. **Zależności między testami** - testy muszą działać niezależnie
2. **Hardcoded wartości** - użyj zmiennych/constów
3. **Catch-all testy** - testuj konkretne scenariusze
4. **Ignorowanie testów** - napraw lub usuń, nie skip
5. **Nadmierne mockowanie** - mockuj tylko zewnętrzne zależności
6. **Testy na implementację** - testuj zachowanie, nie szczegóły

---

## Metryki Jakości Testów

### Code Coverage
```bash
# Generuj raport pokrycia
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Wygeneruj HTML raport
reportgenerator -reports:coverage.cobertura.xml -targetdir:coveragereport
```

### Czas Wykonania
- Testy jednostkowe: < 1s (cel: < 500ms)
- Testy integracyjne: < 30s (cel: < 20s)
- Wszystkie testy: < 35s

### Niezawodność
- Pass rate: 100% (324/324 ✅)
- Flakiness: 0% (wszystkie deterministyczne)

---

## Zasoby

### Dokumentacja
- [xUnit Documentation](https://xunit.net)
- [FluentAssertions Documentation](https://fluentassertions.com)
- [Moq Documentation](https://github.com/moq/moq4)
- [Testcontainers Documentation](https://dotnet.testcontainers.org)

### Przykłady
- Wszystkie testy w folderze `tests/`
- Wzorce w `BaseIntegrationTest.cs`
- Factory pattern w `IntegrationTestWebAppFactory.cs`

---

## Podsumowanie

FitMateBackend posiada kompleksowy zestaw testów:
- ✅ **324 testów** (206 jednostkowych + 118 integracyjnych)
- ✅ **100% pass rate**
- ✅ **40.7% overall coverage** (86.5% Application layer)
- ✅ **All critical paths tested**
- ✅ **Production ready**

Testy są utrzymywane, rozszerzane i stanowią integralną część procesu CI/CD.
