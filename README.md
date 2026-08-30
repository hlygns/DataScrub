# DataScrub — Autonomous Data Cleaning Platform

A full-stack tool that detects and fixes common data quality issues (duplicate records,
missing values, inconsistent formats) in uploaded CSV/Excel files — with human-approved
suggestions rather than blind automated changes.

Built as a generalized version of real-world data-matching problems I worked on during my
internship (company matching, data normalization, duplicate record detection in a CRM app).

## Architecture
CSV/Excel upload
↓
React frontend (upload UI, issue review table, export button)
↓ (REST)
ASP.NET Core Web API — Clean Architecture
├── Domain → Dataset, DetectedIssue entities
├── Application → orchestration service, DTOs, interfaces
└── Infrastructure → EF Core (SQL Server), HTTP client to the ML service
↓ (REST)
Python ML microservice (FastAPI)
├── duplicate_detection.py → fuzzy matching (RapidFuzz) with blocking for performance
├── missing_data.py → group-aware smart value suggestions
├── format_fixing.py → date/phone/name standardization
└── cleaning.py → applies only user-approved fixes to produce a clean file

## Tech Stack

- **Backend:** C#, ASP.NET Core Web API, Entity Framework Core, SQL Server
- **ML Service:** Python, FastAPI, Pandas, RapidFuzz
- **Frontend:** React (Vite), Axios
- **Testing:** xUnit, Moq
- **Architecture:** Clean Architecture (Domain / Application / Infrastructure / API), Dependency Injection, Repository Pattern

## Why this design

- **Microservice split:** C# and Python are independent, deployable services communicating
  purely over REST. Python handles what it's best at (Pandas-based analysis); C# owns the
  workflow, persistence, and API surface.
- **Confidence-scored suggestions:** every detected issue carries a 0.0–1.0 confidence
  score, and nothing is changed in the actual data until a user explicitly approves it.
- **N+1-safe queries:** repository methods use EF Core's `.Include()` where related data is
  needed, avoiding the N+1 query problem.

## Running Locally

### 1. ML Service (Python)
```bash
cd ml-service
python -m venv venv
source venv/bin/activate       # Windows: venv\Scripts\activate
pip install -r requirements.txt
python main.py
```
→ http://localhost:8000/docs

### 2. Backend (ASP.NET Core)
Requires [.NET SDK](https://dotnet.microsoft.com/download) and SQL Server (LocalDB works).
```bash
cd backend
dotnet restore
dotnet ef database update --project DataScrub.Infrastructure --startup-project DataScrub.API
dotnet run --project DataScrub.API
```
→ http://localhost:5000/swagger

### 3. Frontend (React)
```bash
cd frontend
npm install
npm run dev
```
→ http://localhost:5173

### 4. Run tests
```bash
cd backend
dotnet test
```

## Usage Flow

1. Upload a `.csv` or `.xlsx` file
2. The system runs three parallel analyses (duplicates, missing values, format errors)
3. Review each detected issue with its confidence score
4. Approve or reject individual suggestions
5. Export a cleaned file containing only the approved fixes

## Next Steps

- [ ] Docker Compose for one-command startup
- [ ] Async processing (Hangfire) for large files
- [ ] Let users choose which columns are "identifier" columns instead of auto-guessing
- [ ] JWT authentication for multi-user support
