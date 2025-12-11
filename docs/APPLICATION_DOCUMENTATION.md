# FitMateBackend - Dokumentacja Warstwy Application

## Spis Treści
1. [Przegląd](#przegląd)
2. [Architektura](#architektura)
3. [Serwisy](#serwisy)
4. [Abstrakcje](#abstrakcje)
5. [DTOs](#dtos)
6. [Walidacja](#walidacja)
7. [Helpers i Utilities](#helpers-i-utilities)
8. [Wzorce i Konwencje](#wzorce-i-konwencje)

---

## Przegląd

Warstwa Application zawiera logikę biznesową aplikacji FitMateBackend. Jest to warstwa pośrednicząca między API (WebApi) a domeną (Domain), implementująca use case'y i reguły biznesowe.

### Charakterystyka
- ✅ **Business Logic** - Implementacja wszystkich use case'ów
- ✅ **Service Layer** - 11 serwisów pokrywających całą funkcjonalność
- ✅ **DTO Pattern** - Separacja kontraktów API od modelu domenowego
- ✅ **Dependency Inversion** - Wszystkie zależności przez interfejsy
- ✅ **Testowalna** - 100% pokrycie testami jednostkowymi

### Statystyki
- **Serwisy**: 12
- **Interfejsy**: 16
- **DTOs**: 30+
- **Walidatory**: Data Annotations & IValidatableObject
- **Helpers**: 3

---

## Architektura

```
Application/
├── Abstractions/              # Interfejsy (15)
│   ├── IAnalyticsService.cs
│   ├── IApplicationDbContext.cs
│   ├── IAuthService.cs
│   ├── IBodyMetricsService.cs
│   ├── ICurrentUserService.cs
│   ├── IFriendWorkoutService.cs
│   ├── IFriendshipService.cs
│   ├── IPasswordHasher.cs
│   ├── IPlanService.cs
│   ├── IScheduledService.cs
│   ├── ITokenService.cs
│   ├── IUserAdminService.cs
│   ├── IUserProfileService.cs
│   ├── IUserService.cs
│   ├── IUserValidationHelpers.cs
│   └── IWorkoutSessionService.cs
│
├── Services/                  # Implementacje (11)
│   ├── AnalyticsService.cs   # Statystyki i analityka
│   ├── AuthService.cs        # Autentykacja
│   ├── BodyMetricsService.cs # Pomiary ciała
│   ├── CurrentUserService.cs # Context użytkownika
│   ├── FriendWorkoutService.cs # Treningi znajomych
│   ├── FriendshipService.cs  # Zarządzanie znajomościami
│   ├── PlanService.cs        # Plany treningowe
│   ├── ScheduledService.cs   # Zaplanowane treningi
│   ├── UserAdminService.cs   # Administracja użytkownikami
│   ├── UserProfileService.cs # Profile użytkowników
│   ├── UserService.cs        # CRUD użytkowników
│   └── WorkoutSessionService.cs # Sesje treningowe
│
├── DTOs/                      # Data Transfer Objects
│   ├── Auth/                  # DTOs autentykacji
│   │   └── AuthDtos.cs
│   ├── AnalyticsDto.cs       # DTOs analityki
│   ├── FriendDto.cs          # DTOs znajomości
│   ├── FriendsWorkoutsDto.cs # DTOs treningów znajomych
│   ├── BodyMetricsDto.cs     # DTOs pomiarów ciała
│   ├── PlanDto.cs            # DTOs planów
│   ├── ScheduledDto.cs       # DTOs zaplanowanych treningów
│   ├── SessionsByRangeRequest.cs
│   ├── UserDto.cs            # DTOs użytkowników
│   ├── UserProfileDto.cs     # DTOs profili
│   └── WorkoutSessionDto.cs  # DTOs sesji
│
├── Common/                    # Współdzielone komponenty
│   ├── Security/
│   │   └── HttpContextExtensions.cs
│   ├── DateHelpers.cs
│   └── ValidationConstants.cs
│
└── Application.csproj
```

---

## Serwisy

### 1. AuthService (Autentykacja)

**Odpowiedzialność**: Zarządzanie autentykacją i tokenami JWT.

**Interfejs**: `IAuthService`

**Metody**:
```csharp
Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct);
Task LogoutAsync(LogoutRequestDto request, CancellationToken ct);
```

**Funkcjonalność**:
- ✅ **Rejestracja** - Tworzenie nowego użytkownika z hashowaniem hasła (BCrypt)
- ✅ **Login** - Weryfikacja credentials i generowanie tokenów JWT
- ✅ **Refresh** - Odświeżanie access token używając refresh token
- ✅ **Logout** - Unieważnianie refresh token

**Zależności**:
- `IApplicationDbContext` - Dostęp do bazy danych
- `ITokenService` - Generowanie JWT tokenów
- `IPasswordHasher` - Hashowanie/weryfikacja haseł
- `IUserValidationHelpers` - Walidacja unikalności email/username

**Reguły Biznesowe**:
1. Email i Username muszą być unikalne
2. Hasło jest zawsze hashowane (BCrypt)
3. Każdy nowy użytkownik otrzymuje rolę "User"
4. Access token wygasa po 60 minutach
5. Refresh token wygasa po 7 dniach
6. Tylko aktywny refresh token może być użyty

**Przykład Użycia**:
```csharp
// Rejestracja
var request = new RegisterRequest 
{ 
    Email = "user@example.com",
    UserName = "username",
    Password = "SecurePass123!",
    FullName = "John Doe"
};
var response = await _authService.RegisterAsync(request, ct);
// response.AccessToken, response.RefreshToken

// Login
var loginRequest = new LoginRequest 
{
    UserNameOrEmail = "username", // lub email
    Password = "SecurePass123!"
};
var loginResponse = await _authService.LoginAsync(loginRequest, ct);
```

---

### 2. PlanService (Plany Treningowe)

**Odpowiedzialność**: Zarządzanie szablonami planów treningowych.

**Interfejs**: `IPlanService`

**Metody**:
```csharp
Task<PlanDto> CreateAsync(CreatePlanDto dto, CancellationToken ct);
Task<IReadOnlyList<PlanDto>> GetAllAsync(bool includeShared, CancellationToken ct);
Task<PlanDto?> GetByIdAsync(Guid id, CancellationToken ct);
Task<PlanDto?> UpdateAsync(Guid id, CreatePlanDto dto, CancellationToken ct);
Task<bool> DeleteAsync(Guid id, CancellationToken ct);
Task<PlanDto?> DuplicateAsync(Guid id, CancellationToken ct);
Task<bool> ShareToUserAsync(Guid planId, string targetUsername, CancellationToken ct);
Task<IReadOnlyList<SharedPlanDto>> GetSharedWithMeAsync(CancellationToken ct);
// ... więcej metod dla shared plans
```

**Funkcjonalność**:
- ✅ **CRUD** - Tworzenie, odczyt, aktualizacja, usuwanie planów
- ✅ **Duplikacja** - Głęboka kopia planu (nowy GUID + wszystkie ćwiczenia/serie)
- ✅ **Udostępnianie** - Sharing planów między użytkownikami
- ✅ **Filtrowanie** - Po typie, włącznie z udostępnionymi
- ✅ **Ownership** - Walidacja właściciela przed modyfikacją

**Zależności**:
- `IApplicationDbContext` - Dostęp do bazy danych
- `IHttpContextAccessor` - Pobranie ID zalogowanego użytkownika

**Reguły Biznesowe**:
1. Użytkownik może edytować/usuwać tylko własne plany
2. Update używa `ExecuteSqlRaw` do usunięcia starych exercises/sets
3. PlanName jest required, Type jest tekstem (nie enum)
4. GetAll może zwracać plany własne + shared (z flagą)
5. Duplikacja tworzy niezależną kopię (właściciel = current user)

**Przykład Użycia**:
```csharp
// Tworzenie planu
var dto = new CreatePlanDto
{
    PlanName = "PPL Beginner",
    Type = "PPL",
    Notes = "Push/Pull/Legs split",
    Exercises = new List<ExerciseDto>
    {
        new ExerciseDto
        {
            Name = "Bench Press",
            Rest = 120,
            Sets =  new List<SetDto>
            {
                new SetDto { Reps = 8, Weight = 80 },
                new SetDto { Reps = 8, Weight = 80 },
                new SetDto { Reps = 8, Weight = 80 }
            }
        }
    }
};
var plan = await _planService.CreateAsync(dto, ct);
```

---

### 3. ScheduledService (Zaplanowane Treningi)

**Odpowiedzialność**: Zarządzanie kalendarzem treningów (konkretne daty/czasy).

**Interfejs**: `IScheduledService`

**Metody**:
```csharp
Task<ScheduledDto> CreateAsync(CreateScheduledDto dto, CancellationToken ct);
Task<IReadOnlyList<ScheduledDto>> GetAllAsync(CancellationToken ct);
Task<ScheduledDto?> GetByIdAsync(Guid id, CancellationToken ct);
Task<IReadOnlyList<ScheduledDto>> GetByDateAsync(DateOnly date, CancellationToken ct);
Task<ScheduledDto?> UpdateAsync(Guid id, CreateScheduledDto dto, CancellationToken ct);
Task<bool> DeleteAsync(Guid id, CancellationToken ct);
Task<ScheduledDto?> DuplicateAsync(Guid id, DateOnly newDate, CancellationToken ct);
```

**Funkcjonalność**:
- ✅ **CRUD** - Pełne zarządzanie calendar workouts
- ✅ **Filtrowanie** - Po dacie, zakresie dat, statusie
- ✅ **Kopiowanie** - Z planu do scheduled (denormalizacja)
- ✅ **Duplikacja** - Scheduled na nową datę
- ✅ **Visibility** - IsVisibleToFriends dla znajomych

**Zależności**:
- `IApplicationDbContext`
- `IHttpContextAccessor`

**Reguły Biznesowe**:
1. Przy tworzeniu kopiuje exercises/sets z Plan (denormalizacja)
2. PlanName jest kopiowany (zachowanie historii)
3. Status domyślnie `Planned`
4. Użytkownik może zarządzać tylko własnymi scheduled workouts
5. Filtrowanie po statusie: planned/completed

**Przykład Użycia**:
```csharp
// Zaplanowanie workout
var dto = new CreateScheduledDto
{
    PlanId = planId,
    Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
    Time = new TimeOnly(18, 0), // 18:00
    Status = "planned",
    VisibleToFriends = true
};
var scheduled = await _scheduledService.CreateAsync(dto, ct);
```

---

### 4. WorkoutSessionService (Sesje Treningowe)

**Odpowiedzialność**: Zarządzanie rzeczywistym wykonaniem treningów.

**Interfejs**: `IWorkoutSessionService`

**Metody**:
```csharp
Task<SessionDto> StartSessionAsync(StartSessionRequest request, CancellationToken ct);
Task<SessionDto?> GetByIdAsync(Guid id, CancellationToken ct);
Task<IReadOnlyList<SessionDto>> GetAllAsync(CancellationToken ct);
Task<IReadOnlyList<SessionDto>> GetByDateRangeAsync(SessionsByRangeRequest request, CancellationToken ct);
Task<SessionDto?> AddExerciseAsync(Guid sessionId, AddSessionExerciseRequest request, CancellationToken ct);
Task<SessionDto?> PatchSetAsync(Guid sessionId, Guid exerciseId, int setNumber, PatchSetRequest request, CancellationToken ct);
Task<SessionDto?> CompleteSessionAsync(Guid sessionId, CancellationToken ct);
Task<bool> AbortSessionAsync(Guid sessionId, CancellationToken ct);
```

**Funkcjonalność**:
- ✅ **Start** - Rozpoczęcie sesji z scheduled workout
- ✅ **Add Exercise** - Dodawanie exercises podczas sesji
- ✅ **Patch Set** - Aktualizacja pojedynczych series (Reps/Weight/Completed)
- ✅ **Complete** - Zakończenie sesji (status Completed, duration)
- ✅ **Abort** - Przerwanie sesji (status Aborted)
- ✅ **History** - Pobieranie historii sesji (z filtrowaniem)

**Zależności**:
- `IApplicationDbContext`
- `IHttpContextAccessor`

**Reguły Biznesowe**:
1. Tylko jedna aktywna sesja (InProgress) per user
2. Kopiu je exercises/sets z ScheduledWorkout na start
3. Użytkownik może dodać dodatkowe exercises podczas sesji
4. PatchSet aktualizuje Reps, Weight, Completed flag
5. Complete oblicza DurationSec i ustawia CompletedAtUtc
6. Abort nie ustawia CompletedAtUtc (sesja przerwana)

**Przykład Użycia**:
```csharp
// Start sesji
var request = new StartSessionRequest { ScheduledWorkoutId = scheduledId };
var session = await _sessionService.StartSessionAsync(request, ct);

// Aktualizacja serii
var patchRequest = new PatchSetRequest 
{ 
    Reps = 10, 
    Weight = 85.5m, 
    Completed = true 
};
var updated = await _sessionService.PatchSetAsync(
    session.Id, exerciseId, setNumber: 1, patchRequest, ct);

// Zakończenie
var completed = await _sessionService.CompleteSessionAsync(session.Id, ct);
```

---

### 5. AnalyticsService (Statystyki i Analityka)

**Odpowiedzialność**: Obliczanie statystyk treningowych i metryk postępu.

**Interfejs**: `IAnalyticsService`

**Metody**:
```csharp
Task<OverviewDto> GetOverviewAsync(CancellationToken ct);
Task<IReadOnlyList<VolumeDataPoint>> GetVolumeAsync(VolumeRequest request, CancellationToken ct);
Task<IReadOnlyList<E1RmDataPoint>> GetE1RmAsync(E1RmRequest request, CancellationToken ct);
Task<AdherenceDto> GetAdherenceAsync(DateOnly from, DateOnly to, CancellationToken ct);
Task<PlanVsActualDto> GetPlanVsActualAsync(DateOnly from, DateOnly to, CancellationToken ct);
```

**Funkcjonalność**:
- ✅ **Overview** - Podsumowanie: total sessions, volume, streaks
- ✅ **Volume** - Wykres volume by day/week/exercise
- ✅ **E1RM** - Estimated 1RM progression (wzór Brzycki)
- ✅ **Adherence** - % ukończonych zaplanowanych treningów
- ✅ **Plan vs Actual** - Porównanie planu do execution

**Zależności**:
- `IApplicationDbContext`
- `ICurrentUserService`

**Wzory**:
```csharp
// Volume = Suma(Reps × Weight) dla wszystkich sets
Volume = sessions.Sum(s => s.Sets.Sum(set => set.Reps * set.Weight));

// E1RM (Brzycki formula)
E1RM = Weight × (1 + Reps / 30);

// Adherence Rate
Adherence = (CompletedWorkouts / TotalScheduledWorkouts) × 100%;
```

**Przykład Użycia**:
```csharp
// Przegląd
var overview = await _analyticsService.GetOverviewAsync(ct);
// overview.TotalSessions, TotalVolume, CurrentStreak

// Volume progression
var volumeRequest = new VolumeRequest
{
    ExerciseName = "Bench Press",
    From = DateOnly.FromDateTime(DateTime.Today.AddMonths(-3)),
    To = DateOnly.FromDateTime(DateTime.Today),
    GroupBy = "week" // "day", "week", "month"
};
var volumeData = await _analyticsService.GetVolumeAsync(volumeRequest, ct);
```

---

### 6. FriendshipService (Znajomości)

**Odpowiedzialność**: Zarządzanie relacjami znajomości między użytkownikami.

**Interfejs**: `IFriendshipService`

**Metody**:
```csharp
Task<SendFriendRequestResult> SendRequestAsync(string targetUsername, CancellationToken ct);
Task<RespondRequestResult> RespondToRequestAsync(Guid friendshipId, bool accept, CancellationToken ct);
Task<IReadOnlyList<FriendRequestDto>> GetIncomingRequestsAsync(CancellationToken ct);
Task<IReadOnlyList<FriendRequestDto>> GetOutgoingRequestsAsync(CancellationToken ct);
Task<IReadOnlyList<FriendDto>> GetFriendsAsync(CancellationToken ct);
Task<bool> AreFriendsAsync(Guid userId, Guid otherUserId, CancellationToken ct);
Task<bool> DeleteFriendshipAsync(Guid friendId, CancellationToken ct);
```

**Funkcjonalność**:
- ✅ **Send Request** - Wysłanie zaproszenia do znajomych
- ✅ **Respond** - Akceptacja/odrzucenie zaproszenia
- ✅ **Get Requests** - Lista incoming/outgoing zaproszeń
- ✅ **Get Friends** - Lista aktywnych znajomych
- ✅ **Check Friendship** - Sprawdzenie czy użytkownicy są znajomymi
- ✅ **Delete** - Usunięcie znajomości

**Zależności**:
- `IApplicationDbContext`
- `IHttpContextAccessor`

**Reguły Biznesowe**:
1. UserAId < UserBId zawsze (optymalizacja, jeden rekord)
2. Tylko odbiorca może zaakceptować/odrzucić pending request
3. Duplikaty są blokowane (sprawdzenie przed utworzeniem)
4. Nie możesz wysłać zaproszenia samemu sobie
5. Status: Pending → Accepted/Rejected

**Przykład Użycia**:
```csharp
// Wysłanie zaproszenia
var result = await _friendshipService.SendRequestAsync("john_doe", ct);
// result: Ok, UserNotFound, AlreadyFriends, PendingRequest, SelfRequest

// Akceptacja
var respondResult = await _friendshipService.RespondToRequestAsync(
    friendshipId, accept: true, ct);
// respondResult: Ok, NotFound, AlreadyResponded
```

---

### 7. FriendWorkoutService (Treningi Znajomych)

**Odpowiedzialność**: Wyświetlanie treningów znajomych (widoczność).

**Interfejs**: `IFriendWorkoutService`

**Metody**:
```csharp
Task<IReadOnlyList<FriendScheduledDto>> GetFriendsScheduledAsync(
    DateOnly? from, DateOnly? to, CancellationToken ct);
Task<IReadOnlyList<FriendSessionDto>> GetFriendsSessionsAsync(
    DateOnly? from, DateOnly? to, CancellationToken ct);
```

**Funkcjonalność**:
- ✅ **Scheduled Workouts** - Zaplanowane treningi znajomych (IsVisibleToFriends=true)
- ✅ **Sessions** - Ukończone sesje znajomych
- ✅ **Filtrowanie** - Po zakresie dat
- ✅ **Privacy** - Tylko workouts z flagą visibility

**Zależności**:
- `IApplicationDbContext`
- `ICurrentUserService`

**Reguły Biznesowe**:
1. Tylko workouts z IsVisibleToFriends=true
2. Tylko dla potwierdzonych znajomych (Status=Accepted)
3. Filtrowanie po date range (opcjonalne)
4. Sortowanie chronologiczne

---

### 8. UserService (Użytkownicy - CRUD)

**Odpowiedzialność**: Podstawowe operacje CRUD na użytkownikach (dla Adminów).

**Interfejs**: `IUserService`

**Metody**:
```csharp
Task<IReadOnlyList<UserDto>> GetAllAsync(string? search, CancellationToken ct);
Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken ct);
Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken ct);
```

**Funkcjonalność**:
- ✅ **Get All** - Lista użytkowników (z opcjonalnym wyszukiwaniem)
- ✅ **Create** - Tworzenie użytkownika (Admin)
- ✅ **Update** - Aktualizacja danych użytkownika

**Reguły Biznesowe**:
1. Search działa na: Username, Email, FullName
2. Email i Username muszą być unikalne
3. Przy tworzeniu ustawia pusty PasswordHash (Admin set later)
4. Walidacja username pattern: `^[A-Za-z0-9._-]+$`

---

### 9. UserAdminService (Administracja Użytkownikami)

**Odpowiedzialność**: Operacje administracyjne na użytkownikach.

**Interfejs**: `IUserAdminService`

**Metody**:
```csharp
Task<AssignRoleResult> AssignRoleAsync(Guid userId, string roleName, CancellationToken ct);
Task<DeleteUserResult> DeleteUserAsync(Guid userId, CancellationToken ct);
Task<ResetPasswordResult> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct);
```

**Funkcjonalność**:
- ✅ **Assign Role** - Przypisanie roli użytkownikowi
- ✅ **Delete User** - Usunięcie użytkownika (z ochroną Adminów)
- ✅ **Reset Password** - Reset hasła

**Reguły Biznesowe**:
1. Nie można usunąć użytkownika z rolą Admin
2. Sprawdzanie istnienia roli przed przypisaniem
3. Sprawdzanie duplikatów przy assign role
4. ResetPassword hashuje nowe hasło (BCrypt)

**Result Enums**:
```csharp
public enum AssignRoleResult { Ok, UserNotFound, RoleNotFound, AlreadyHasRole }
public enum DeleteUserResult { Ok, UserNotFound, IsAdmin }
public enum ResetPasswordResult { Ok, UserNotFound }
```

---

### 10. UserProfileService (Profile Użytkowników)

**Odpowiedzialność**: Zarządzanie własnym profilem użytkownika.

**Interfejs**: `IUserProfileService`

**Metody**:
```csharp
Task<UserProfileDto> GetCurrentAsync(CancellationToken ct);
Task<UserProfileDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct);
Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct);
Task<TargetWeightDto> GetTargetWeightAsync(CancellationToken ct);
Task UpdateTargetWeightAsync(UpdateTargetWeightRequest request, CancellationToken ct);
Task UpdateBiometricsPrivacyAsync(bool shareWithFriends, CancellationToken ct);
```

**Funkcjonalność**:
- ✅ **Get Profile** - Pobranie własnego profilu
- ✅ **Update** - Aktualizacja FullName, Email, Username
- ✅ **Change Password** - Zmiana hasła (wymagane stare hasło)
- ✅ **Target Weight** - Zarządzanie wagą docelową (40-200 kg)
- ✅ **Biometrics Privacy** - Kontrola widoczności danych dla znajomych

**Reguły Biznesowe**:
1. Walidacja aktualnego hasła przed zmianą
2. Email/Username unikalność
3. Username pattern validation
4. Użytkownik może edytować tylko własny profil
5. **Target Weight**: można ustawić (40-200kg) lub wyczyścić (null/0)
6. **Privacy**: domyślnie `ShareBiometricsWithFriends = false`

**Nowe Funkcje (Target Weight)**:

```csharp
// Pobranie wagi docelowej
var targetWeight = await _userProfileService.GetTargetWeightAsync(ct);
// targetWeight.TargetWeightKg może być null (nie ustawiona)

// Ustawienie wagi docelowej
var updateRequest = new UpdateTargetWeightRequest { TargetWeightKg = 75.5m };
await _userProfileService.UpdateTargetWeightAsync(updateRequest, ct);

// Wyczyszczenie wagi docelowej (opcja 1: null)
var clearRequest = new UpdateTargetWeightRequest { TargetWeightKg = null };
await _userProfileService.UpdateTargetWeightAsync(clearRequest, ct);

// Wyczyszczenie wagi docelowej (opcja 2: 0)
var clearRequest2 = new UpdateTargetWeightRequest { TargetWeightKg = 0 };
await _userProfileService.UpdateTargetWeightAsync(clearRequest2, ct);

// Zmiana ustawień prywatności biometryki
await _userProfileService.UpdateBiometricsPrivacyAsync(shareWithFriends: true, ct);
```

**Walidacja Target Weight**:
- **Zakres**: 40-200 kg (pokrywa 99% dorosłych)
- **Clearing**: `0` lub `null` czyści wartość
- **Wartości poniżej 40kg**: Błąd walidacji (oprócz 0)
- **Wartości powyżej 200kg**: Błąd walidacji

**DTOs**:
```csharp
public class UserProfileDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public IReadOnlyList<string> Roles { get; set; }
    public decimal? TargetWeightKg { get; set; }  // NEW
    public bool ShareBiometricsWithFriends { get; set; }  // NEW
}

public class TargetWeightDto
{
    public decimal? TargetWeightKg { get; set; }
}

public class UpdateTargetWeightRequest : IValidatableObject
{
    public decimal? TargetWeightKg { get; set; }
    // Walidacja: 0 lub 40-200kg
}

public class UpdateBiometricsPrivacyRequest
{
    [Required]
    public bool ShareWithFriends { get; set; }
}
```

**Endpoints**:
- `GET /api/userprofile` - Zwraca profil z `TargetWeightKg` i `ShareBiometricsWithFriends`
- `GET /api/userprofile/target-weight` - Dedykowany endpoint dla wagi docelowej
- `PUT /api/userprofile/target-weight` - Ustawienie/wyczyszczenie wagi
- `PUT /api/userprofile/biometrics-privacy` - Toggle widoczności biometryki


---

### 11. BodyMetricsService (Pomiary Ciała)

**Odpowiedzialność**: Zarządzanie pomiarami ciała i statystykami.

**Interfejs**: `IBodyMetricsService`

**Metody**:
```csharp
Task<BodyMeasurementDto> AddMeasurementAsync(CreateBodyMeasurementDto dto, CancellationToken ct);
Task<IReadOnlyList<BodyMeasurementDto>> GetMeasurementsAsync(DateTime? from, DateTime? to, CancellationToken ct);
Task<BodyMeasurementDto?> GetLatestMeasurementAsync(CancellationToken ct);
Task<BodyMetricsStatsDto> GetStatsAsync(CancellationToken ct);
Task<IReadOnlyList<BodyMetricsProgressDto>> GetProgressAsync(DateTime from, DateTime to, CancellationToken ct);
Task DeleteMeasurementAsync(Guid id, CancellationToken ct);
Task<IReadOnlyList<BodyMeasurementDto>> GetFriendMetricsAsync(Guid friendId, DateTime? from, DateTime? to, CancellationToken ct);
```

**Funkcjonalność**:
- ✅ **Add Measurement** - Dodanie nowego pomiaru (auto BMI)
- ✅ **Get History** - Pobranie historii pomiarów
- ✅ **Get Latest** - Szybki dostęp do aktualnej wagi/BMI
- ✅ **Stats** - Min/Max waga, zmiany, kategoria BMI
- ✅ **Progress** - Dane do wykresów

**Reguły Biznesowe**:
1. BMI obliczane automatycznie: `Weight / (Height * Height)`
2. Waga i Wzrost muszą być dodatnie
3. Pomiary są prywatne

---

### 12. CurrentUserService

**Odpowiedzialność**: Pobranie ID zalogowanego użytkownika z HttpContext.

**Interfejs**: `ICurrentUserService`

**Metody**:
```csharp
Guid GetUserId();
```

**Funkcjonalność**:
- ✅ Ekstrakcja User ID z JWT claim (`ClaimTypes.NameIdentifier`)
- ✅ Throw exception jeśli brak autentykacji

---

## Abstrakcje

### Interfejsy Zewnętrzne (Infrastructure)

#### IApplicationDbContext
**Opis**: Abstrakcja dostępu do bazy danych.

```csharp
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Plan> Plans { get; }
    DbSet<PlanExercise> PlanExercises { get; }
    DbSet<PlanSet> PlanSets { get; }
    DbSet<ScheduledWorkout> ScheduledWorkouts { get; }
    DbSet<ScheduledExercise> ScheduledExercises { get; }
    DbSet<ScheduledSet> ScheduledSets { get; }
    DbSet<WorkoutSession> WorkoutSessions { get; }
    DbSet<SessionExercise> SessionExercises { get; }
    DbSet<SessionSet> SessionSets { get; }
    DbSet<Friendship> Friendships { get; }
    DbSet<SharedPlan> SharedPlans { get; }
    
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

**Cel**: Dependency Inversion - warstwa Application nie zależy od Entity Framework bezpośrednio.

#### IPasswordHasher
**Opis**: Abstrakcja hashowania haseł.

```csharp
public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
```

**Implementacja**: BCrypt (Infrastructure layer).

#### ITokenService
**Opis**: Abstrakcja generowania JWT tokenów.

```csharp
public interface ITokenService
{
    (string AccessToken, DateTime ExpiresAt) CreateAccessToken(User user, IEnumerable<string> roles);
    (string RefreshToken, DateTime ExpiresAt) CreateRefreshToken();
}
```

**Implementacja**: JWT (Infrastructure layer).

---

## DTOs

### Kategorie DTOs

#### 1. Auth DTOs
```csharp
// Request
public class RegisterRequest
{
    public string Email { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string FullName { get; set; }
}

public class LoginRequest
{
    public string UserNameOrEmail { get; set; }
    public string Password { get; set; }
}

// Response
public class AuthResponse
{
    public string AccessToken { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; }
}
```

#### 2. Plan DTOs
```csharp
public class PlanDto
{
    public Guid Id { get; set; }
    public Guid? SharedPlanId { get; set; } // ID udostępnienia (jeśli jest to plan udostępniony)
    public string PlanName { get; set; } = null!;
    public string Type { get; set; }
    public string? Notes { get; set; }
    public List<ExerciseDto> Exercises { get; set; }
}

public class ExerciseDto
{
    public string Name { get; set; }
    public int Rest { get; set; }
    public List<SetDto> Sets { get; set; }
}

public class SetDto
{
    public int Reps { get; set; }
    public decimal Weight { get; set; }
}
```

#### 3. Session DTOs
```csharp
public class SessionDto
{
    public Guid Id { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int? DurationSec { get; set; }
    public string Status { get; set; } // "in_progress", "completed", "aborted"
    public List<SessionExerciseDto> Exercises { get; set; }
}

public class SessionExerciseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int RestSeconds { get; set; }
    public List<SessionSetDto> Sets { get; set; }
}

public class SessionSetDto
{
    public int SetNumber { get; set; }
    public int Reps { get; set; }
    public decimal Weight { get; set; }
    public bool Completed { get; set; }
}
```

#### 4. Analytics DTOs
```csharp
public class OverviewDto
{
    public int TotalSessions { get; set; }
    public decimal TotalVolume { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
}

public class VolumeDataPoint
{
    public string Date { get; set; } // "2024-11-23" or "2024-W47"
    public decimal Volume { get; set; }
}

public class E1RmDataPoint
{
    public DateOnly Date { get; set; }
    public decimal E1Rm { get; set; }
}

public class AdherenceDto
{
    public int TotalScheduled { get; set; }
    public int Completed { get; set; }
    public decimal AdherenceRate { get; set; } // percentage
}
```

#### 5. Body Metrics DTOs
```csharp
public class CreateBodyMeasurementDto
{
    public decimal WeightKg { get; set; }
    public int HeightCm { get; set; }
    public decimal? BodyFatPercentage { get; set; }
    public string? Notes { get; set; }
    // ... opcjonalne obwody
}

public class BodyMeasurementDto : CreateBodyMeasurementDto
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public decimal BMI { get; set; }
}

public class BodyMetricsStatsDto
{
    public decimal CurrentWeight { get; set; }
    public decimal CurrentBMI { get; set; }
    public string BMICategory { get; set; }
    public decimal LowestWeight { get; set; }
    public decimal HighestWeight { get; set; }
    public int TotalMeasurements { get; set; }
}
```

---

## Walidacja

### Data Annotations & IValidatableObject

Projekt wykorzystuje standardowy mechanizm walidacji w ASP.NET Core oparty na atrybutach `DataAnnotations` oraz interfejsie `IValidatableObject` dla bardziej złożonej logiki.

**Podejście**:
1. **Atrybuty** (`[Required]`, `[StringLength]`, `[Range]`) - dla prostych reguł pojedynczych pól.
2. **IValidatableObject** - dla reguł zależnych od wielu pól lub list (np. "Plan musi mieć min. 1 ćwiczenie").

#### Przykład: CreatePlanDto

```csharp
public sealed class CreatePlanDto : IValidatableObject
{
    [Required, StringLength(100, MinimumLength = 3, ErrorMessage = "Plan name must be 3-100 chars.")]
    public string PlanName { get; set; } = null!;
    
    [Required, StringLength(50, MinimumLength = 2, ErrorMessage = "Type must be 2-50 chars.")]
    public string Type { get; set; } = null!;

    public List<ExerciseDto> Exercises { get; set; } = new();

    // Złożona walidacja
    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (Exercises is null || Exercises.Count == 0)
            yield return new ValidationResult("Plan must contain at least one exercise.", new[] { nameof(Exercises) });

        if (Exercises.Count > 100)
            yield return new ValidationResult("Too many exercises (max 100 per plan).", new[] { nameof(Exercises) });
    }
}
```

#### Przykład: ExerciseDto

```csharp
public class ExerciseDto : IValidatableObject
{
    [Required]
    public string Name { get; set; } = null!;
    
    [Range(0, 3600)]
    public int Rest { get; set; } = 90;

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (Sets is null || Sets.Count == 0)
            yield return new ValidationResult("Exercise must contain at least one set.", new[] { nameof(Sets) });
    }
}
```

**Rejestracja w DI**:
```csharp
services.AddValidatorsFromAssemblyContaining<CreatePlanDtoValidator>();
services.AddFluentValidationAutoValidation();
```

---

## Helpers i Utilities

### 1. UserValidationHelpers

**Opis**: Helper do walidacji unikalności email/username.

```csharp
public interface IUserValidationHelpers
{
    Task EnsureEmailIsUniqueAsync(string email, CancellationToken ct);
    Task EnsureUserNameIsUniqueAsync(string userName, CancellationToken ct);
}

public class UserValidationHelpers : IUserValidationHelpers
{
    public async Task EnsureEmailIsUniqueAsync(string email, CancellationToken ct)
    {
        var exists = await _db.Users.AnyAsync(u => u.Email == email, ct);
        if (exists)
            throw new InvalidOperationException("Email is already taken.");
    }

    public async Task EnsureUserNameIsUniqueAsync(string userName, CancellationToken ct)
    {
        var exists = await _db.Users.AnyAsync(u => u.UserName == userName, ct);
        if (exists)
            throw new InvalidOperationException("Username is already taken.");
    }
}
```

### 2. DateHelpers

**Opis**: Pomocnicze metody do operacji na datach.

```csharp
public static class DateHelpers
{
    public static (DateOnly start, DateOnly end) GetWeekRange(DateOnly date)
    {
        int daysToSunday = ((int)date.DayOfWeek - (int)DayOfWeek.Sunday + 7) % 7;
        var sunday = date.AddDays(-daysToSunday);
        var saturday = sunday.AddDays(6);
        return (sunday, saturday);
    }
}
```

### 3. HttpContextExtensions

**Opis**: Extension methods dla HttpContext.

```csharp
public static class HttpContextExtensions
{
    public static Guid GetUserId(this HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("User is not authenticated");

        return Guid.Parse(userIdClaim);
    }
}
```

### 4. ValidationConstants

**Opis**: Stałe walidacyjne.

```csharp
public static class ValidationConstants
{
    public const string UsernamePattern = @"^[A-Za-z0-9._-]+$";
    public const int MinPasswordLength = 8;
    public const int MaxPlanNameLength = 200;
}
```

---

## Wzorce i Konwencje

### 1. Service Pattern

**Struktura**:
```csharp
public class ServiceName : IServiceName
{
    private readonly IApplicationDbContext _db;
    private readonly IDependency _dependency;

    public ServiceName(IApplicationDbContext db, IDependency dependency)
    {
        _db = db;
        _dependency = dependency;
    }

    public async Task<ResultDto> MethodAsync(RequestDto request, CancellationToken ct)
    {
        // 1. Validation
        // 2. Business logic
        // 3. Database operations
        // 4. Return DTO
    }
}
```

### 2. Result Pattern (dla serwisów Admin)

**Enumy zamiast wyjątków** dla przewidywalnych błędów:
```csharp
public enum OperationResult
{
    Ok,
    NotFound,
    Conflict,
    Forbidden
}
```

### 3. DTO Mapping

**Ręczne mapowanie** (bez AutoMapper dla kontroli):
```csharp
private static PlanDto Map(Plan plan) => new()
{
    Id = plan.Id,
    PlanName = plan.PlanName,
    Type = plan.Type,
    Notes = plan.Notes,
    Exercises = plan.Exercises.Select(e => new ExerciseDto
    {
        Name = e.Name,
        Rest = e.RestSeconds,
        Sets = e.Sets.Select(s => new SetDto
        {
            Reps = s.Reps,
            Weight = s.Weight
        }).ToList()
    }).ToList()
};
```

### 4. Dependency Injection

**Rejestracja serwisów**:
```csharp
// Application layer registration
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IPlanService, PlanService>();
services.AddScoped<IScheduledService, ScheduledService>();
services.AddScoped<IWorkoutSessionService, WorkoutSessionService>();
services.AddScoped<IAnalyticsService, AnalyticsService>();
services.AddScoped<IFriendshipService, FriendshipService>();
services.AddScoped<IFriendWorkoutService, FriendWorkoutService>();
services.AddScoped<IUserService, UserService>();
services.AddScoped<IUserAdminService, UserAdminService>();
services.AddScoped<IUserProfileService, UserProfileService>();
services.AddScoped<ICurrentUserService, CurrentUserService>();
services.AddScoped<IUserValidationHelpers, UserValidationHelpers>();
```

### 5. Async/Await

**Zawsze używaj**:
- `async Task<T>` dla metod zwracających wartość
- `async Task` dla metod void
- `CancellationToken` jako ostatni parametr
- `ConfigureAwait(false)` nie jest potrzebny w ASP.NET Core

### 6. Null Safety

**Konwencje**:
- `Entity?` - nullable reference
- `Entity` - non-nullable (throw jeśli null)
- `IReadOnlyList<T>` zamiast `List<T>` w zwracanych wartościach

---

## Podsumowanie

Warstwa Application FitMateBackend jest:
- ✅ **Kompletna** - 11 serwisów pokrywających cały use case
- ✅ **Testowalna** - 100% pokrycie testami jednostkowymi (105 testów)
- ✅ **SOLID** - Dependency Inversion, Single Responsibility
- ✅ **Clean Architecture** - Niezależność od infrastruktury
- ✅ **Walidowana** - FluentValidation + custom validators
- ✅ **Bezpieczna** - Proper authorization checks, password hashing

Wszystkie serwisy są produkcyjnie gotowe i przetestowane.
