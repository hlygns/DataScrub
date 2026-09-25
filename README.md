# 🧹 DataScrub — Autonomous Data Cleaning Platform

![CI](https://github.com/hlygns/DataScrub/actions/workflows/ci.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![Python](https://img.shields.io/badge/Python-FastAPI-3776AB)
![React](https://img.shields.io/badge/React-Vite-61DAFB)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED)

A full-stack tool that detects and fixes common data quality issues — **duplicate records, missing values and inconsistent formats** — in uploaded CSV/Excel files. Every fix is a *suggestion* with a confidence score; nothing changes in the data until a user approves it.

Built as a generalized version of real-world data-matching problems I worked on during my internship (company matching, data normalization and duplicate record detection in a CRM application).

---

## ✨ Features

- **Duplicate detection** — fuzzy string matching with RapidFuzz, using blocking to avoid comparing every row with every other row
- **Missing value suggestions** — group-aware predictions (e.g. filling a missing city based on similar records)
- **Format standardization** — dates, phone numbers and names normalized to a consistent format
- **Human-in-the-loop review** — each issue has a 0.0–1.0 confidence score and can be approved or rejected individually
- **Clean export** — download a cleaned file containing only the approved fixes

---

## 🚀 Quick Start (Docker)

Requires [Docker Desktop](https://www.docker.com/products/docker-desktop/).

```bash
git clone https://github.com/hlygns/DataScrub.git
cd DataScrub
docker compose up --build
```

| Service | URL |
|---|---|
| Frontend | http://localhost:5173 |
| Backend API (Swagger) | http://localhost:5000/swagger |
| ML Service (docs) | http://localhost:8000/docs |

> The database credentials in `docker-compose.yml` are for local development only.

---

## 🏗️ Architecture

```
            CSV / Excel upload
                    │
                    ▼
┌──────────────────────────────────────┐
│  React Frontend (Vite)               │
│  upload · issue review · export      │
└──────────────────┬───────────────────┘
                   │ REST
                   ▼
┌──────────────────────────────────────┐
│  ASP.NET Core Web API                │
│  (Clean Architecture)                │
│  ├─ Domain          entities         │
│  ├─ Application     services, DTOs   │
│  ├─ Infrastructure  EF Core, HTTP    │
│  └─ API             controllers      │
└─────────┬────────────────────┬───────┘
          │ EF Core            │ REST
          ▼                    ▼
   ┌─────────────┐   ┌─────────────────────────┐
   │ SQL Server  │   │ Python ML Service       │
   └─────────────┘   │ (FastAPI)               │
                     │ ├─ duplicate_detection  │
                     │ ├─ missing_data         │
                     │ ├─ format_fixing        │
                     │ └─ cleaning             │
                     └─────────────────────────┘
```

The backend and the ML service share a Docker volume for uploaded files: the API stores the file and sends its path to the ML service, which reads it and writes the cleaned output back.

---

## 🧠 Design Decisions

- **Microservice split:** C# and Python are independently deployable services communicating over REST. Python handles what it is best at (Pandas-based analysis); C# owns the workflow, persistence and public API.
- **Suggestions, not silent changes:** automated cleaning can destroy valid data. Every detected issue carries a confidence score, and only user-approved fixes are applied.
- **Blocking for performance:** comparing every row pair is O(n²). Records are first grouped into blocks and fuzzy matching runs only within each block.
- **N+1-safe queries:** repository methods use EF Core's `.Include()` where related data is needed.
- **Clean Architecture:** the domain layer has no dependency on EF Core or HTTP, which keeps business logic testable with mocked interfaces.

---

## 🛠️ Tech Stack

| Layer | Technologies |
|---|---|
| Backend | C#, ASP.NET Core Web API, Entity Framework Core, SQL Server |
| ML Service | Python, FastAPI, Pandas, RapidFuzz, OpenPyXL |
| Frontend | React (Vite), Axios |
| Testing | xUnit, Moq |
| DevOps | Docker, Docker Compose, GitHub Actions, Nginx |
| Patterns | Clean Architecture, Repository Pattern, Dependency Injection |

---

## 🔌 ML Service Endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | `/health` | Health check |
| POST | `/inspect` | Reads the file and returns column/row information |
| POST | `/detect-duplicates` | Finds likely duplicate records |
| POST | `/detect-missing` | Suggests values for missing fields |
| POST | `/fix-format` | Detects and suggests format fixes |
| POST | `/apply-cleaning` | Applies approved fixes and writes the clean file |

---

## 📋 Usage Flow

1. Upload a `.csv` or `.xlsx` file
2. The system analyzes it for duplicates, missing values and format errors
3. Review each detected issue with its confidence score
4. Approve or reject individual suggestions
5. Export a cleaned file containing only the approved fixes

---

## 💻 Running Without Docker

<details>
<summary>Manual setup</summary>

**1. ML Service**
```bash
cd ml-service
python -m venv venv
source venv/bin/activate       # Windows: venv\Scripts\activate
pip install -r requirements.txt
python main.py
```

**2. Backend** — requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) and SQL Server (LocalDB works)
```bash
cd backend
dotnet restore
dotnet ef database update --project DataScrub.Infrastructure --startup-project DataScrub.API
dotnet run --project DataScrub.API
```

**3. Frontend**
```bash
cd frontend
npm install
npm run dev
```

**4. Tests**
```bash
cd backend
dotnet test
```
</details>

---

## 🗺️ Roadmap

- [x] Clean Architecture .NET API + Python ML microservice
- [x] Confidence-scored, user-approved fixes
- [x] Docker Compose for one-command startup
- [x] CI with GitHub Actions
- [ ] Async processing (Hangfire) for large files
- [ ] Let users choose identifier columns instead of auto-guessing
- [ ] JWT authentication for multi-user support
- [ ] Unit tests for the ML service (pytest)
- [ ] Evaluation script measuring duplicate detection precision/recall

---

## 👩‍💻 Author

**Hülya Güneş** — [GitHub](https://github.com/hlygns) · [LinkedIn](https://www.linkedin.com/in/hulyaguness)
