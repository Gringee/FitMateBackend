# 🏋️‍♂️ FitMate Backend

Backend w **ASP.NET Core 8** + **PostgreSQL**, z modułami: **Plans**, **Scheduled**, **Sessions**, **Analytics** oraz **JWT Auth (Users/Roles)**.

---

## 📦 Technologie

- **.NET 8 / ASP.NET Core Web API**
- **Entity Framework Core (PostgreSQL)**
- **Docker + Docker Compose**
- **Swagger UI**
- **Clean Architecture**
- **REST API + OpenAPI 3.0**
- **JWT Authentication (Bearer)**
---

## 🚀 Uruchomienie w Dockerze

### 1️⃣ Wymagania
Upewnij się, że masz zainstalowane:
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Git](https://git-scm.com/)

### 2️⃣ Klonowanie repozytorium
```bash
git clone https://github.com/Gringee/FitMateBackend.git
cd FitMateBackend
```

### 3️⃣ Uruchomienie kontenerów
Uruchom aplikację razem z bazą danych PostgreSQL:
```bash
docker compose up -d --build
```

💡 Pierwsze uruchomienie może potrwać chwilę (pobranie obrazów i wykonanie migracji bazy danych).

---

## 🌐 Dostępne usługi

| Usługa | Adres | Opis |
|--------|--------|------|
| **API (Web)** | [http://localhost:8080](http://localhost:8080) | Endpoint testowy `/` |
| **Swagger UI** | [http://localhost:8080/swagger](http://localhost:8080/swagger) | Interaktywna dokumentacja API |
| **PostgreSQL (DB)** | `localhost:5433` | Dostęp z zewnątrz (`training/devpass`) |

---

## ⚙️ Ustawienia środowiska

Domyślny connection string (ustawiony w `docker-compose.yml`):
```
Host=db;Port=5432;Database=fitmatedb;Username=training;Password=devpass
```

Baza danych jest przechowywana w wolumenie Dockera:
```
pg_FitMate_data
```

---

## 🔁 Aktualizacja po zmianach w kodzie

Jeśli zmienisz kod aplikacji:
```bash
docker compose build api
docker compose up -d
```

Jeśli chcesz wymusić pełne odtworzenie środowiska:
```bash
docker compose down -v
docker compose up -d --build
```

---

## 🧪 Szybkie testy API (curl)

```bash
# Health
curl http://localhost:8080/
curl http://localhost:8080/health/db

# Plans
curl -H "Authorization: Bearer <TOKEN>" http://localhost:8080/api/plans

# Scheduled (po dacie)
curl -H "Authorization: Bearer <TOKEN>" "http://localhost:8080/api/scheduled/by-date?date=2025-11-07"

# Sessions – start
curl -X POST http://localhost:8080/api/sessions/start   -H "Authorization: Bearer <TOKEN>" -H "Content-Type: application/json"   -d '{"scheduledId":"<GUID>"}'

# Analytics – overview
curl -H "Authorization: Bearer <TOKEN>" "http://localhost:8080/api/analytics/overview?from=2025-11-01&to=2025-11-30"
```

---

## 🧱 Struktura projektu

```
FitMateBackend/
├── src/
│   ├── Domain/              # encje i logika domenowa
│   ├── Application/         # DTO, interfejsy i serwisy aplikacyjne
│   ├── Infrastructure/      # EF Core, DbContext, konfiguracje
│   └── WebApi/              # kontrolery, punkty wejścia, Swagger
├── tests/                   # testy integracyjne
├── docker-compose.yml
└── README.md
```

---

## 📊 Moduł Analytics API

System oferuje analizę postępów treningowych na podstawie zapisanych sesji.

| Endpoint | Opis |
|-----------|------|
| `GET /api/analytics/overview` | Zwraca kluczowe KPI z wybranego zakresu (objętość, sesje, adherence). |
| `GET /api/analytics/volume` | Zwraca sumaryczną objętość treningową pogrupowaną po dniu, tygodniu lub ćwiczeniu. |
| `GET /api/analytics/exercises/{name}/e1rm` | Zwraca historię estymowanego 1RM dla wybranego ćwiczenia. |
| `GET /api/analytics/adherence` | Zwraca współczynnik zrealizowanych treningów (plan vs wykonanie). |
| `GET /api/analytics/plan-vs-actual` | Porównuje zaplanowane powtórzenia i ciężary z rzeczywistymi. |

💡 Wyniki tych endpointów są używane we frontendzie (React/TypeScript) do generowania wykresów i podsumowań w dashboardzie.

---

## 🔐 Auth – jak działa (skrót dla frontendu)

- Rejestracja: `POST /api/auth/register` → zwraca `accessToken` (JWT) + `expiresAtUtc`.
- Logowanie: `POST /api/auth/login` → zwraca `accessToken` (JWT) + `expiresAtUtc`.
- Każde wywołanie chronione: dodaj nagłówek  
  `Authorization: Bearer <ACCESS_TOKEN>`
- Role: `User` (domyślnie), `Admin` (dostęp do /api/users).

### Przykłady (curl)
**Rejestracja**
```bash
curl -X POST http://localhost:8080/api/auth/register   -H "Content-Type: application/json"   -d '{"email":"user1@test.local","password":"Pass123!","fullName":"User One"}'
```

**Logowanie**
```bash
curl -X POST http://localhost:8080/api/auth/login   -H "Content-Type: application/json"   -d '{"email":"user1@test.local","password":"Pass123!"}'
```

**Wywołanie chronionego endpointu**
```bash
curl http://localhost:8080/api/plans -H "Authorization: Bearer <TOKEN>"
```

**Endpoint admina**
```bash
curl http://localhost:8080/api/users -H "Authorization: Bearer <ADMIN_TOKEN>"
```

---

## 🧰 Dodatkowe komendy Docker

Zatrzymanie kontenerów:
```bash
docker compose down
```

Podgląd logów API:
```bash
docker compose logs -f api
```

Restart API po zmianach:
```bash
docker compose restart api
```

---

## 👥 Autorzy

- Filip Kulig

---

## 🟢 Status projektu
Projekt rozwijany w ramach pracy inżynierskiej.
