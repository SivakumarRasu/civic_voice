# CivicVoice — Municipal Grievance Redressal System (Milestone 1)

A complete, production-grade municipal grievance reporting and tracking web application built with **ASP.NET Core MVC 10**, **SQL Server Developer Edition**, **Entity Framework Core 10**, and **Clean Architecture**.

---

## 🏛️ Project Overview

Citizens can register municipal complaints regarding problems such as water leakage, damaged roads, garbage collection, streetlights, drainage, and sanitation.

- Every grievance belongs to a **Ward** and a **Category**.
- The **Category** automatically determines the responsible **Department** and assigns a binding **SLA deadline** (e.g., 24h, 48h, 72h).
- The system manages multi-stage status lifecycles (`Submitted` → `Assigned` → `InProgress` → `Resolved` / `Rejected`).
- All status transitions are immutably audited via a SQL Server database trigger (`trg_GrievanceStatusHistory`).
- Citizens can rate and submit feedback for resolved grievances with duplicate-prevention enforcement.

---

## 🏗️ Clean Architecture Structure

```
CivicVoice/
├── CivicVoice.Domain/              # Core Domain: Entities, Enums, and Domain Events (Zero dependencies)
│   ├── Entities/                   # Ward, Department, Category, Grievance, Assignment, StatusHistory, Feedback
│   ├── Enums/                      # GrievanceStatus (Submitted, Assigned, InProgress, Resolved, Rejected)
│   └── Events/                     # GrievanceResolvedEvent & delegate
├── CivicVoice.Application/         # Business Logic, DTOs, and Interfaces
│   ├── Interfaces/                 # IRepository<T>, IGrievanceRepository, IServices, etc.
│   ├── Services/                   # GrievanceService, FeedbackService, DepartmentService, WardService
│   └── DTOs/                       # CreateGrievanceDto, ServiceResult<T>, DepartmentReportDto
├── CivicVoice.Infrastructure/      # Data Access, EF Core 10, Migrations, Repositories, and SQL Objects
│   ├── Data/                       # CivicVoiceDbContext, DbInitializer (Identity seeding)
│   ├── Repositories/               # Repository<T>, GrievanceRepository, SpecializedRepositories
│   ├── Migrations/                 # Code-first EF Core Migrations
│   └── Sql/                        # vw_DepartmentSlaReport, usp_SearchGrievances, trg_GrievanceStatusHistory
├── CivicVoice.Web/                 # ASP.NET Core MVC 10 Presentation Layer
│   ├── Controllers/                # Home, Grievances, Officer, Commissioner, Feedback, Account, Validation
│   ├── Views/                      # Razor Views with Bootstrap 5.3 & custom styling
│   ├── ViewModels/                 # Input models with DataAnnotations & IValidatableObject
│   ├── Filters/                    # AuditLoggingActionFilter (IAsyncActionFilter)
│   └── Validation/                 # NotFutureDateAttribute, Remote Validation
├── CivicVoice.Tests/               # Unit Testing Suite (xUnit, Moq)
└── CivicVoice.slnx                 # Solution configuration
```

---

## 👥 User Roles & Academic Demonstration Credentials

| Role | Email | Password | Responsibilities |
|---|---|---|---|
| **Citizen** | `citizen@civicvoice.local` | `Password123!` | Report complaints, upload photos, track SLA status, submit ratings |
| **Roads Officer** | `roads_officer@civicvoice.local` | `Password123!` | Department 2 worklist, update status (`Assigned` → `InProgress` → `Resolved`) |
| **Water Officer** | `water_officer@civicvoice.local` | `Password123!` | Department 1 worklist, resolve water leakage complaints |
| **Commissioner** | `commissioner@civicvoice.local` | `Password123!` | City-wide SLA analytics, department performance report, CSV exports |

> 💡 **Quick Demo Feature:** The navigation bar and login page include **1-Click Evaluation Logins** for instant role switching without typing credentials.

---

## 🗄️ SQL Server Database Objects

The database `CivicVoiceDb` includes all required native SQL Server objects:

1. **`vw_DepartmentSlaReport` (SQL View):**
   Aggregates total, open, resolved, and SLA-breached grievances per department without double-counting.
2. **`usp_SearchGrievances` (Stored Procedure):**
   Full parameterized multi-field query for description, ward, category, department, and SLA breach status.
3. **`trg_GrievanceStatusHistory` (Audit Trigger):**
   AFTER UPDATE trigger on `Grievances` table that detects `Status` changes, captures timestamps (`SYSUTCDATETIME()`), multi-row updates, and avoids recursion via `TRIGGER_NESTLEVEL()`.

---

## ⚙️ How to Run the Application

### Prerequisites
- .NET 10 SDK
- SQL Server (Developer Edition or Express) running locally

### 1. Build Solution
```powershell
dotnet build
```

### 2. Run Database Migrations & Apply Custom SQL
```powershell
dotnet ef database update --project CivicVoice.Infrastructure --startup-project CivicVoice.Web
powershell -File "C:\Users\sivak\.gemini\antigravity-ide\brain\e53c6a53-4ee1-4bcc-a739-dcebcdd42b02\scratch\apply_sql.ps1"
```
*(Note: `CivicVoice.Web` also automatically executes `DbInitializer.SeedAsync()` upon startup to ensure tables and demo users are present).*

### 3. Run Web Application
```powershell
cd CivicVoice.Web
dotnet run
```
Then navigate to: `https://localhost:7187` or `http://localhost:5187` (check terminal output for exact assigned port).

### 4. Execute Unit Tests
```powershell
dotnet test
```
**Results:** 21 passing tests, 0 failures.

---

## 🧪 Verified M1 Requirements Checklist

- [x] **Compiling Solution (.NET 10, C# 13, SQL Server)**
- [x] **7 Core Domain Entities & Relationships Configured**
- [x] **Clean Architecture (Domain, Application, Infrastructure, Web, Tests)**
- [x] **EF Core 10 Migrations Applied to SQL Server (`CivicVoiceDb`)**
- [x] **SQL View `vw_DepartmentSlaReport` Created & Verified**
- [x] **Stored Procedure `usp_SearchGrievances` Created & Verified**
- [x] **Database Trigger `trg_GrievanceStatusHistory` Created & Verified**
- [x] **Grievance Registration with Department Routing & SLA Calculation**
- [x] **Photo Evidence Upload with 5MB & Extension Validation**
- [x] **Anonymous Grievance Masking**
- [x] **State Machine Lifecycle Validation (`Submitted` → `Assigned` → `InProgress` → `Resolved` / `Rejected`)**
- [x] **Citizen Satisfaction Feedback (1–5 Stars) with Unique Constraint**
- [x] **ASP.NET Core Identity Role-Based Access Control (Citizen, Officer, Commissioner)**
- [x] **Remote Validation (`/Validation/ValidateWard`)**
- [x] **Custom Validation Attribute (`[NotFutureDate]`) & `IValidatableObject`**
- [x] **Attribute Routing with Integer Constraint (`/complaint/{id:int}`)**
- [x] **Action Filter for Audit Logging (`AuditLoggingActionFilter`)**
- [x] **Live AJAX Search & Filtering with Debouncing (Phase 9)**
- [x] **CSV Export with Safe Escaping and Authorization (Phase 12)**
- [x] **21 Comprehensive xUnit Unit Tests (100% Passing)**
