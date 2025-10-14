# 🏋️‍♂️ FitMate Backend

**FitMateBackend** to aplikacja backendowa w **ASP.NET Core 8** z bazą danych **PostgreSQL**, służąca do zarządzania planami treningowymi i harmonogramami (`plans` i `scheduled workouts`).

---

## 📦 Technologie

- **.NET 8 / ASP.NET Core Web API**
- **Entity Framework Core (PostgreSQL)**
- **Docker + Docker Compose**
- **Swagger UI**

---

## 🚀 Uruchomienie w Dockerze

### 1️⃣ Wymagania
Upewnij się, że masz zainstalowane:
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Git](https://git-scm.com/)

---

### 2️⃣ Klonowanie repozytorium

```bash
git clone https://github.com/Gringee/FitMateBackend.git
cd FitMateBackend
```

---

### 3️⃣ Uruchomienie kontenerów

Uruchom aplikację razem z bazą danych PostgreSQL:

```bash
docker compose up -d --build
```

💡 **Pierwsze uruchomienie** może potrwać chwilę (pobranie obrazów i wykonanie migracji bazy danych).

---

## 🌐 Dostępne usługi

| Usługa           | Adres                                   | Opis                                |
|------------------|------------------------------------------|-------------------------------------|
| **API (Web)**    | [http://localhost:8080](http://localhost:8080) | Endpoint testowy `/`               |
| **Swagger UI**   | [http://localhost:8080/swagger](http://localhost:8080/swagger) | Interaktywna dokumentacja API |
| **PostgreSQL**   | `localhost:5433`                        | Dostęp z zewnątrz <br>login: `training` <br>hasło: `devpass` |

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

Jeśli chcesz **wymusić pełne odtworzenie środowiska**:

```bash
docker compose down -v
docker compose up -d --build
```

---

## 🧪 Testowe endpointy

### 1️⃣ Sprawdzenie działania API

```bash
curl http://localhost:8080/
```

Odpowiedź:

```json
{ "ok": true, "name": "TrainingPlanner API" }
```

### 2️⃣ Swagger

Otwórz w przeglądarce:

👉 [http://localhost:8080/swagger](http://localhost:8080/swagger)

---

## 🧰 Dodatkowe komendy Docker

**Zatrzymanie kontenerów:**
```bash
docker compose down
```

**Podgląd logów API:**
```bash
docker compose logs -f api
```

**Restart API po zmianach:**
```bash
docker compose restart api
```

---

## 📁 Struktura projektu

```
FitMateBackend/
├── src/
│   ├── Domain/              # encje i logika domenowa
│   ├── Application/         # DTO, interfejsy i serwisy aplikacyjne
│   ├── Infrastructure/      # EF Core, DbContext, implementacje serwisów
│   └── WebApi/              # kontrolery i konfiguracja API
├── docker-compose.yml
├── README.md
└── .gitignore
```

---

## 🧩 Autorzy

- **Filip Kulig** — Backend (.NET, EF Core, Docker)  

---

🟢 Projekt rozwijany w ramach nauki architektury **Clean Architecture** oraz integracji **Docker + PostgreSQL**.
