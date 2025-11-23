# 🏋️‍♂️ FitMate Backend

Profesjonalne REST API do zarządzania treningami, planowania aktywności oraz analizy postępów. Projekt tworzony w ramach pracy inżynierskiej, oparty o **.NET 8**, **PostgreSQL** i zasady **Clean Architecture**.

---

## 🌟 Kluczowe Funkcjonalności

### 🔐 Bezpieczeństwo i Autoryzacja
- **JWT Authentication** – krótkotrwałe Access Tokeny (60 min).
- **Refresh Token Rotation** – bezpieczna rotacja tokenów odświeżających.
- **Password Hashing** – algorytm BCrypt.
- **Role-Based Access Control (RBAC)** – role `User` i `Admin`.

### 📅 Planowanie i Trening
- **Zarządzanie planami treningowymi** – tworzenie, edycja, duplikowanie planów (zestawy, serie).
- **Kalendarz (Scheduling)** – planowanie treningów na konkretne dni (`DateOnly`, `TimeOnly`).
- **Live Sessions** – monitorowanie sesji na żywo, logowanie wyników serii (RPE, ciężar, powtórzenia).
- **Ad-Hoc Exercises** – dodawanie niezaplanowanych ćwiczeń w trakcie trwającego treningu.

### 🤝 Funkcje Społecznościowe
- **System znajomych** – zaproszenia, akceptacja, odrzucanie.
- **Udostępnianie planów** – możliwość dzielenia się treningami ze znajomymi.
- **Widoczność aktywności** – opcja `VisibleToFriends` pozwala znajomym przeglądać Twój kalendarz.

### 📊 Analityka (High Performance)
- **Agregacja SQL** – obliczenia wykonywane po stronie bazy (Volume, 1RM, Intensity).
- **KPI Dashboard** – podsumowania (Adherence, liczba sesji).
- **Wykresy** – dane przygotowane do wizualizacji liniowej i słupkowej.

---

## 📦 Technologie

- **Core**: .NET 8 / ASP.NET Core Web API
- **Database**: PostgreSQL + `citext` + Entity Framework Core
- **Architecture**: Clean (Onion) Architecture
- **Containerization**: Docker & Docker Compose
- **Docs**: Swagger UI (OpenAPI 3.0)
- **Logging**: ILogger
- **Validation**: DataAnnotations + IValidatableObject

---

## 🚀 Uruchomienie (Docker)

Najprostszy sposób na szybkie postawienie środowiska API + baza danych.

### 1️⃣ Wymagania
- Docker Desktop
- Git

### 2️⃣ Uruchomienie

```bash
git clone https://github.com/Gringee/FitMateBackend.git
cd FitMateBackend

docker compose up -d --build
```

💡 *Pierwsze uruchomienie wykona migracje bazy danych automatycznie i utworzy domyślne role (User, Admin).*

### 🌐 Dostęp do usług

| Usługa           | URL                          | Opis                       |
|------------------|------------------------------|-----------------------------|
| API Health Check | http://localhost:8080        | Status API                 |
| Swagger UI       | http://localhost:8080/swagger | Dokumentacja (tylko Development) |
| PostgreSQL       | localhost:5433               | User: `training`, Pass: `devpass` |

---

## 🧱 Struktura Projektu (Clean Architecture)

```
src/
├── Domain/              # Encje, Enumy, logika rdzeniowa (brak zależności)
├── Application/         # Logika biznesowa: interfejsy, DTO, implementacje serwisów
├── Infrastructure/      # EF Core, Persistence, Autentykacja (BCrypt, JWT)
└── WebApi/              # Kontrolery, Middleware, Swagger, Converters
```

---

## 🧪 Testowanie API

### 🔎 Swagger UI
1. Uruchom aplikację.
2. Wejdź na: `http://localhost:8080/swagger`.
3. Zarejestruj nowe konto (`/api/auth/register`).
4. Zaloguj się (`/api/auth/login`) i skopiuj Access Token.
5. Kliknij **Authorize** i wklej token (Swagger doda `Bearer` sam).

### Przykładowe formaty danych
- **Data**: `yyyy-MM-dd` → `2026-11-18`
- **Czas**: `HH:mm:ss` → `18:30:00`
- **DateTime UTC**: `yyyy-MM-ddTHH:mm:ssZ`

### ⚙️ Środowisko
- **Development**: Swagger enabled, CORS AllowAnyOrigin, detailed errors
- **Production**: Swagger disabled, CORS restricted (via AllowedOrigins), minimal error disclosure

---

## ⚙️ Rozwiązywanie problemów

### 1. Błąd połączenia z bazą danych
```bash
docker compose ps
```

### 2. Zmiany w kodzie nie są widoczne
```bash
docker compose up -d --build --force-recreate
```

### 3. Reset środowiska (usunięcie danych)
```bash
docker compose down -v
```

---

## 👥 Autor
**Filip Kulig** – Projekt inżynierski.

