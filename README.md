# 🏋️‍♂️ FitMate Backend

**FitMateBackend** to aplikacja backendowa w **ASP.NET Core 8** z bazą danych **PostgreSQL**, służąca do zarządzania planami treningowymi, harmonogramami, sesjami treningowymi i analizą postępów.

---

## 📦 Technologie

- **.NET 8 / ASP.NET Core Web API**
- **Entity Framework Core (PostgreSQL)**
- **Docker + Docker Compose**
- **Swagger UI**
- **Clean Architecture**
- **REST API + OpenAPI 3.0**

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

## 🧪 Testowanie API

### Sprawdzenie działania API
```bash
curl http://localhost:8080/
```

### Przykładowa odpowiedź:
```json
{ "ok": true, "name": "FitMate API" }
```

### Swagger UI:
[http://localhost:8080/swagger](http://localhost:8080/swagger)

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

## 🔒 Autentykacja (planowana)

W kolejnych wersjach zostanie dodane:
- Rejestracja i logowanie użytkowników (JWT)
- Role: `user`, `admin`
- Ograniczenie dostępu do prywatnych planów i analiz

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
