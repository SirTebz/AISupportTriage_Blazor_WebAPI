# 🤖 AI Support Triage Platform

A production-grade, AI-powered helpdesk SaaS application that automatically classifies, prioritises, and routes support tickets in real time. Built with Clean Architecture on .NET 8.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Web_API-512BD4?style=flat-square&logo=dotnet)
![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?style=flat-square&logo=blazor)
![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC2927?style=flat-square&logo=microsoftsqlserver)
![OpenAI](https://img.shields.io/badge/OpenAI-GPT--4o--mini-74AA9C?style=flat-square&logo=openai)
![SignalR](https://img.shields.io/badge/SignalR-Real--Time-512BD4?style=flat-square)
![Docker](https://img.shields.io/badge/Docker-Containerised-2496ED?style=flat-square&logo=docker)

---

## 📋 Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Demo Accounts](#demo-accounts)
- [AI Triage Engine](#ai-triage-engine)
- [API Endpoints](#api-endpoints)
- [Real-Time Events](#real-time-events)
- [Docker](#docker)
- [CI/CD](#cicd)
- [Roadmap](#roadmap)

---

## Overview

AI Support Triage Platform is a multi-tenant SaaS helpdesk solution that removes the manual work from ticket management. When a customer submits a ticket, the platform immediately saves it and triggers an asynchronous AI analysis pipeline that:

1. Classifies the ticket into a category (Technical, Billing, Security, etc.)
2. Scores sentiment (how frustrated is the customer?)
3. Scores urgency (how critical is the issue?)
4. Suggests relevant tags
5. Generates a suggested first reply
6. Auto-assigns the ticket to the most appropriate available agent
7. Pushes real-time updates to all connected dashboards via SignalR

The user is never blocked — submission is instant and AI works silently in the background.

---

## Features

### 🎫 Ticket Management
- Create, view, update, and track support tickets
- Full conversation thread with internal agent notes
- Ticket audit log tracking every status change
- Soft delete with full history preservation

### 🤖 AI Engine
- Automatic sentiment analysis (0.0 → 1.0 scale)
- Urgency detection (0.0 → 1.0 scale)
- Category classification across 7 categories
- Suggested reply generation
- Tag suggestions
- Mock AI mode when no API key is configured (great for development)

### ⚡ Smart Routing
- Priority auto-set from urgency score
  - `> 0.85` → Critical
  - `> 0.65` → High
  - `> 0.40` → Medium
  - `≤ 0.40` → Low
- Department matching by category
- Workload balancing — assigns to agent with fewest open tickets

### 📊 Real-Time Dashboard
- Live ticket stats (total, open, resolved, SLA breached)
- Tickets by category breakdown
- Per-agent workload view
- Average resolution time
- SignalR-powered live updates — no page refresh needed

### ⏱️ SLA Tracking
- SLA deadline auto-calculated on ticket creation
  - Free plan → 24 hours
  - Pro plan → 8 hours
  - Enterprise plan → 4 hours
- Automated breach detection runs every 15 minutes via Hangfire
- Breached tickets escalated to High priority automatically
- Visual countdown on ticket list

### 🔐 Multi-Tenant Authentication
- JWT-based authentication
- Four roles: `SuperAdmin`, `CompanyAdmin`, `SupportAgent`, `Customer`
- Tenant isolation via global EF Core query filters
- Company self-registration with admin account auto-created

### 🏢 Multi-Tenant SaaS
- Shared database with `TenantId` column isolation
- Per-company plan and SLA configuration
- Tenant-aware query filters prevent data leakage between companies

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│              Blazor WebAssembly Client               │
│         (AISupportTriage.BlazorClient.Client)        │
└──────────────────────┬──────────────────────────────┘
                       │ HTTP + JWT
                       │ SignalR WebSocket
┌──────────────────────▼──────────────────────────────┐
│              ASP.NET Core Web API                    │
│              (AISupportTriage.API)                   │
│                                                      │
│  ┌─────────────┐  ┌──────────────┐  ┌────────────┐  │
│  │    Auth     │  │   Tickets    │  │ Analytics  │  │
│  │ Controller  │  │  Controller  │  │ Controller │  │
│  └─────────────┘  └──────────────┘  └────────────┘  │
│                                                      │
│  ┌─────────────────────────────────────────────────┐ │
│  │           SignalR Hub (TicketHub)               │ │
│  └─────────────────────────────────────────────────┘ │
└──────────────────────┬──────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────┐
│              Application Layer                       │
│              (AISupportTriage.Application)           │
│                                                      │
│   TicketService    RoutingEngine    DTOs             │
│   IAuthService     Validators       Interfaces       │
└──────┬───────────────────────────────────┬──────────┘
       │                                   │
┌──────▼──────────────┐   ┌───────────────▼──────────┐
│  Infrastructure     │   │  Infrastructure           │
│  (Data)             │   │  (AI + Jobs)              │
│                     │   │                           │
│  ApplicationDbCtx   │   │  OpenAiTriageService      │
│  SQL Server         │   │  AiAnalysisJob (Hangfire) │
│  EF Core            │   │  SlaCheckJob  (Hangfire)  │
│  Identity           │   │  TicketNotificationHub    │
└─────────────────────┘   └───────────────────────────┘
```

### Clean Architecture Layers

| Layer | Project | Responsibility |
|---|---|---|
| **Domain** | `AISupportTriage.Domain` | Entities, enums, domain interfaces |
| **Application** | `AISupportTriage.Application` | Business logic, DTOs, service interfaces, validators |
| **Infrastructure** | `AISupportTriage.Infrastructure` | EF Core, AI service, Hangfire jobs, SignalR hub |
| **Presentation** | `AISupportTriage.API` | Controllers, JWT auth, middleware |
| **Client** | `AISupportTriage.BlazorClient.Client` | Blazor WASM UI |

---

## Tech Stack

### Backend
| Technology | Purpose |
|---|---|
| ASP.NET Core 8 Web API | REST API and host |
| Entity Framework Core 8 | ORM and database access |
| SQL Server (LocalDB / Docker) | Relational database |
| ASP.NET Core Identity | User management and roles |
| JWT Bearer Authentication | Stateless auth tokens |
| Hangfire | Background job processing |
| SignalR | Real-time WebSocket communication |
| Serilog | Structured logging to console and file |
| FluentValidation | Input validation |

### Frontend
| Technology | Purpose |
|---|---|
| Blazor Web App (.NET 8) | Host server |
| Blazor WebAssembly | Client-side SPA (runs in browser) |
| Bootstrap 5.3 | UI framework |
| Bootstrap Icons 1.11 | Icon library |

### AI
| Technology | Purpose |
|---|---|
| OpenAI API (GPT-4o-mini) | Ticket triage and analysis |
| Mock AI fallback | Development mode without API key |

### DevOps
| Technology | Purpose |
|---|---|
| Docker + Docker Compose | Containerisation |
| GitHub Actions | CI/CD pipeline |

---

## Project Structure

```
AISupportTriage/
│
├── AISupportTriage.Domain/
│   ├── Entities/
│   │   ├── Tenant.cs
│   │   ├── ApplicationUser.cs
│   │   ├── Ticket.cs
│   │   ├── TicketMessage.cs
│   │   └── TicketAuditLog.cs
│   ├── Enums/
│   │   ├── TicketStatus.cs
│   │   ├── TicketPriority.cs
│   │   └── TicketCategory.cs
│   └── Interfaces/
│       └── IHasTenant.cs
│
├── AISupportTriage.Application/
│   ├── DTOs/
│   │   ├── Auth/
│   │   ├── Tickets/
│   │   ├── AI/
│   │   └── Analytics/
│   ├── Interfaces/
│   │   ├── IApplicationDbContext.cs
│   │   ├── IAiTriageService.cs
│   │   ├── IAuthService.cs
│   │   ├── ICurrentTenantService.cs
│   │   ├── IRoutingEngine.cs
│   │   └── ITicketService.cs
│   ├── Services/
│   │   ├── TicketService.cs
│   │   └── RoutingEngine.cs
│   ├── Validators/
│   │   └── CreateTicketValidator.cs
│   └── Extensions/
│       └── ApplicationServiceExtensions.cs
│
├── AISupportTriage.Infrastructure/
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   ├── ApplicationDbContextFactory.cs   ← EF migrations design-time factory
│   │   └── Seed/
│   │       └── DataSeeder.cs
│   ├── AI/
│   │   └── OpenAiTriageService.cs
│   ├── Hubs/
│   │   └── TicketNotificationHub.cs
│   ├── Jobs/
│   │   ├── AiAnalysisJob.cs
│   │   └── SlaCheckJob.cs
│   ├── Services/
│   │   └── CurrentTenantService.cs
│   └── Extensions/
│       └── InfrastructureServiceExtensions.cs
│
├── AISupportTriage.API/
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── TicketsController.cs
│   │   └── AnalyticsController.cs
│   ├── Middleware/
│   │   └── ExceptionMiddleware.cs
│   ├── Services/
│   │   └── AuthService.cs
│   ├── Program.cs
│   └── appsettings.json
│
├── AISupportTriage.BlazorClient/             ← Host (server)
│   └── Components/
│       └── App.razor
│
├── AISupportTriage.BlazorClient.Client/      ← WASM app (all UI code)
│   ├── Auth/
│   │   └── CustomAuthStateProvider.cs
│   ├── Models/
│   │   └── AppModels.cs
│   ├── Pages/
│   │   ├── Dashboard.razor
│   │   ├── Auth/
│   │   │   ├── Login.razor
│   │   │   └── Register.razor
│   │   └── Tickets/
│   │       ├── TicketList.razor
│   │       ├── CreateTicket.razor
│   │       └── TicketDetail.razor
│   ├── Services/
│   │   ├── LocalStorageService.cs
│   │   ├── AuthClientService.cs
│   │   ├── TicketApiService.cs
│   │   └── SignalRService.cs
│   ├── Shared/
│   │   ├── MainLayout.razor
│   │   ├── NavMenu.razor
│   │   ├── EmptyLayout.razor
│   │   ├── RedirectToLogin.razor
│   │   ├── StatusBadge.razor
│   │   ├── PriorityBadge.razor
│   │   └── MetaRow.razor
│   ├── App.razor
│   ├── _Imports.razor
│   ├── Program.cs
│   └── wwwroot/
│       ├── appsettings.json
│       └── css/
│           └── app.css
│
├── .github/
│   └── workflows/
│       └── ci-cd.yml
├── docker-compose.yml
└── README.md
```

---

## Prerequisites

Before you begin, make sure you have the following installed:

| Tool | Version | Download |
|---|---|---|
| Visual Studio | 2022 (v17.8+) | [visualstudio.microsoft.com](https://visualstudio.microsoft.com/) |
| .NET SDK | 8.0 | Included with Visual Studio 2022 |
| SQL Server | Any edition | LocalDB included with Visual Studio |
| Docker Desktop | Latest | [docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop/) |
| Git | Latest | [git-scm.com](https://git-scm.com/) |

### Visual Studio Workloads Required
Open **Visual Studio Installer** and ensure these workloads are checked:
- ✅ ASP.NET and web development
- ✅ .NET desktop development

---

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/your-username/AISupportTriage.git
cd AISupportTriage
```

### 2. Open in Visual Studio

Double-click `AISupportTriage.sln` or open Visual Studio and use
**File → Open → Project/Solution**.

### 3. Configure the Connection String

Open `AISupportTriage.API/appsettings.json` and verify the connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AISupportTriageDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

For a full SQL Server instance, replace with:
```
Server=YOUR_SERVER;Database=AISupportTriageDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True
```

### 4. Add Your OpenAI API Key (Optional)

In `AISupportTriage.API/appsettings.json`:

```json
{
  "OpenAI": {
    "ApiKey": "sk-your-openai-api-key-here"
  }
}
```

> **No key? No problem.** The platform includes a mock AI mode that automatically
> activates when no key is configured. It analyses keywords to classify and score
> tickets — perfect for development and demos.

### 5. Run the Database Migration

Open **Tools → NuGet Package Manager → Package Manager Console** and run:

```powershell
Add-Migration InitialCreate -Project AISupportTriage.Infrastructure -StartupProject AISupportTriage.API
Update-Database -Project AISupportTriage.Infrastructure -StartupProject AISupportTriage.API
```

The database is created automatically and seeded with demo data on first run.

### 6. Align the Ports

Check `AISupportTriage.API/Properties/launchSettings.json` for the API HTTPS port
and make sure it matches in these two files:

- `AISupportTriage.API/appsettings.json` → `"ClientUrl": "https://localhost:7200"`
- `AISupportTriage.BlazorClient.Client/wwwroot/appsettings.json` → `"ApiBaseUrl": "https://localhost:7100"`

### 7. Set Multiple Startup Projects

Right-click the **Solution** → **Set Startup Projects** → select
**Multiple startup projects**:

| Project | Action |
|---|---|
| `AISupportTriage.API` | Start |
| `AISupportTriage.BlazorClient` | Start |
| `AISupportTriage.BlazorClient.Client` | None |

### 8. Run the Application

Press **F5**. Two browser tabs will open:

| URL | What it is |
|---|---|
| `https://localhost:7100/swagger` | API Swagger documentation |
| `https://localhost:7200` | Blazor frontend |
| `https://localhost:7100/hangfire` | Background job dashboard |

---

## Configuration

### `appsettings.json` Reference

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "JwtSettings": {
    "Secret": "your-secret-key-min-32-chars",
    "Issuer": "AISupportTriage",
    "Audience": "AISupportTriageClients",
    "ExpirationInMinutes": 480
  },
  "OpenAI": {
    "ApiKey": "sk-..."
  },
  "ClientUrl": "https://localhost:7200"
}
```

### Environment Variables (Docker / Production)

| Variable | Description |
|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `JwtSettings__Secret` | JWT signing secret (min 32 characters) |
| `OpenAI__ApiKey` | OpenAI API key |
| `ClientUrl` | Blazor client origin for CORS |

---

## Demo Accounts

The following accounts are seeded automatically on first run:

| Role | Email | Password | Access |
|---|---|---|---|
| Company Admin | admin@acme.com | Admin@123 | Full access — all tickets, analytics, agent management |
| Support Agent | agent@acme.com | Agent@123 | View and manage assigned tickets, add notes |
| Customer | customer@acme.com | Customer@123 | Submit tickets, view own tickets, reply to messages |

---

## AI Triage Engine

### How It Works

```
Customer submits ticket
        │
        ▼
API saves ticket (status: PendingAnalysis)
        │
        ▼
Hangfire enqueues AiAnalysisJob (async — user gets instant response)
        │
        ▼
OpenAI GPT-4o-mini analyses title + description
        │
        ▼
Returns JSON: { category, sentimentScore, urgencyScore, confidence,
                suggestedTags, suggestedReply }
        │
        ▼
Ticket updated: category, scores, tags, AI reply added as internal note
        │
        ▼
RoutingEngine determines priority + assigns agent
        │
        ▼
SignalR notifies all connected dashboards
```

### Categories

| Category | Description |
|---|---|
| Technical | Bugs, errors, crashes, integration issues |
| Billing | Invoices, charges, payment failures |
| Security | Account breaches, suspicious activity |
| Sales | Pricing, upgrades, plan enquiries |
| AccountManagement | Profile, access, permissions |
| General | Anything else |
| Other | Unclassifiable |

### Urgency → Priority Mapping

| Urgency Score | Assigned Priority |
|---|---|
| > 0.85 | Critical |
| > 0.65 | High |
| > 0.40 | Medium |
| ≤ 0.40 | Low |

### Mock AI Mode

When `OpenAI:ApiKey` is missing or set to `YOUR_OPENAI_API_KEY`, the platform
automatically uses keyword-based analysis:

- Detects `billing`, `invoice`, `payment` → Billing category
- Detects `hack`, `breach`, `security` → Security category
- Detects `urgent`, `asap`, `critical`, `down` → High urgency score
- Detects `terrible`, `awful`, `hate` → Low sentiment score

This makes the platform fully functional for demos without any API costs.

---

## API Endpoints

### Authentication

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | None | Register a new company + admin |
| POST | `/api/auth/login` | None | Login, returns JWT token |

### Tickets

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/tickets` | Required | List all tickets for tenant |
| GET | `/api/tickets/{id}` | Required | Get ticket with messages and audit log |
| POST | `/api/tickets` | Required | Create ticket (triggers AI job) |
| PUT | `/api/tickets/{id}/status` | Agent/Admin | Update ticket status |
| PUT | `/api/tickets/{id}/assign` | Agent/Admin | Assign agent to ticket |
| POST | `/api/tickets/{id}/messages` | Required | Add message or internal note |

### Analytics

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/analytics/summary` | Agent/Admin | Dashboard stats and workloads |

Full interactive documentation available at `/swagger` when running.

---

## Real-Time Events

SignalR hub is available at `/hubs/tickets`.

| Event | Payload | Fired When |
|---|---|---|
| `TicketCreated` | `ticketId, title` | New ticket submitted |
| `TicketUpdated` | `ticketId` | Status, agent, or AI analysis changes |
| `NewMessage` | `ticketId` | Message added to a ticket |
| `SlaWarning` | `ticketId[]` | SLA check job detects breaches |

### Connecting from JavaScript

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/tickets", {
        accessTokenFactory: () => yourJwtToken
    })
    .withAutomaticReconnect()
    .build();

connection.on("TicketCreated", (id, title) => {
    console.log(`New ticket: ${title}`);
});

await connection.start();
```

---

## Docker

### Run with Docker Compose

Make sure Docker Desktop is running, then from the solution root:

```bash
docker compose up --build
```

This starts three containers:

| Container | Port | Description |
|---|---|---|
| `sqlserver` | 1433 | SQL Server 2022 Express |
| `api` | 7100 | ASP.NET Core Web API |
| `blazor` | 7200 | Blazor client (served via nginx) |

### Environment Variables for Docker

Create a `.env` file in the solution root:

```env
OPENAI_API_KEY=sk-your-key-here
SA_PASSWORD=YourStrong@Password123
```

### Stop and Clean Up

```bash
docker compose down
docker compose down -v   # also removes database volume
```

---

## CI/CD

The GitHub Actions pipeline at `.github/workflows/ci-cd.yml` runs on every push
to `main` or `develop`:

```
Push to main/develop
        │
        ▼
┌───────────────────┐
│  build-and-test   │
│  • dotnet restore │
│  • dotnet build   │
│  • dotnet publish │
└────────┬──────────┘
         │ (main branch only)
         ▼
┌───────────────────┐
│  docker-build     │
│  • Build API image│
│  • Build UI image │
│  • Push to Hub    │
└───────────────────┘
```

### Required GitHub Secrets

Go to **Settings → Secrets and Variables → Actions** and add:

| Secret | Value |
|---|---|
| `DOCKER_USERNAME` | Your Docker Hub username |
| `DOCKER_TOKEN` | Your Docker Hub access token |
| `OPENAI_API_KEY` | Your OpenAI API key |

---

## Roadmap

- [ ] Email notifications on ticket assignment and SLA breach
- [ ] Customer portal with ticket history
- [ ] Agent performance reports with charts
- [ ] Webhook integrations (Slack, Teams)
- [ ] Redis caching for analytics queries
- [ ] Rate limiting on AI calls
- [ ] SuperAdmin tenant management panel
- [ ] Mobile-responsive layout improvements
- [ ] Export tickets to CSV / PDF

---

## License

This project is licensed under the MIT License.

---

## Acknowledgements

- [OpenAI](https://openai.com/) — GPT-4o-mini for ticket analysis
- [Hangfire](https://www.hangfire.io/) — Background job processing
- [SignalR](https://learn.microsoft.com/aspnet/core/signalr/introduction) — Real-time communications
- [Bootstrap](https://getbootstrap.com/) — UI framework
- [Serilog](https://serilog.net/) — Structured logging

---

*Built as a full-stack .NET 8 SaaS MVP demonstrating Clean Architecture,
multi-tenancy, AI integration, real-time communications, and production DevOps practices.*
