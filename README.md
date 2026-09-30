# Opportunity Shield

Paste an internship, scholarship, or job posting — get a structured trust assessment before you apply.

Opportunity Shield is a scam-detection API for students. It investigates a submitted URL or text, gathers evidence from web research and content analysis, and returns a risk level, a confidence level, and the evidence behind them — not just a single AI opinion.

**Live API:** `<add deployed URL>`
**Frontend:** `<add repo link>`

## Features

- Accepts a URL or pasted text
- Verifies the organization via web search, checks the domain, and scans the content for scam patterns (payment requests, urgency tactics, sensitive-info asks)
- Returns a deterministic risk level (`low` / `medium` / `high` / `unableToVerify`) and confidence, not just a raw AI response
- Explicitly reports "unable to verify" instead of guessing when evidence is insufficient
- Async processing with live progress updates while an analysis runs
- Anonymous session-based history, no account required

## Tech Stack

- .NET 9 / ASP.NET Core Web API
- EF Core 9 + PostgreSQL
- Google Gemini (search-grounded research) and Groq (fast text analysis)
- Swagger / OpenAPI

## Architecture

Four layers, dependencies pointing inward only:

```
Domain  <--  Application  <--  Infrastructure  <--  Api
```

- **Domain** — entities, the risk engine, signal definitions. No external dependencies.
- **Application** — orchestrates an analysis end to end; defines the interfaces Infrastructure implements.
- **Infrastructure** — EF Core, PostgreSQL, and the Gemini/Groq clients.
- **Api** — controllers, DTOs, and app startup.

A key design decision: **the AI never decides the risk level.** Gemini and Groq only gather facts and explain results in plain language; a separate, deterministic engine in `Domain` makes the actual risk/confidence decision from that evidence.

## Getting Started

### Prerequisites

- .NET 9 SDK
- PostgreSQL
- A Gemini API key and a Groq API key

### Setup

```bash
# Store secrets locally (never commit these)
cd src/OpportunityShield.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=opportunityshield;Username=postgres;Password=postgres"
dotnet user-secrets set "Gemini:ApiKey" "<your key>"
dotnet user-secrets set "Groq:ApiKey" "<your key>"

# Apply database migrations
dotnet ef database update -p ../OpportunityShield.Infrastructure -s .

# Run
dotnet run
```

In `Development`, migrations apply automatically on startup and Swagger is available at `/swagger`.

### Configuration

| Setting | Env var (production) |
|---|---|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` |
| `Gemini:ApiKey` / `Gemini:Model` | `Gemini__ApiKey` / `Gemini__Model` |
| `Groq:ApiKey` / `Groq:Model` | `Groq__ApiKey` / `Groq__Model` |
| `Cors:AllowedOrigins` | `Cors__AllowedOrigins__0` |

`appsettings.json` ships with empty placeholders — never commit real keys.

## API

All endpoints are under `/api/v1`. There's no login — the server issues an anonymous session ID in the `X-Session-Id` response header on your first call; send it back on every later request.

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/analyses` | Submit an opportunity. Returns `202` immediately with an `analysisId`. |
| `GET` | `/analyses/{id}` | Poll for status/result. |
| `GET` | `/analyses` | List this session's past analyses. |
| `POST` | `/analyses/{id}/feedback` | Rate a completed analysis. |

**Example**

```json
POST /api/v1/analyses
{ "inputType": "text", "content": "Pay a $50 registration fee to secure your spot!" }
```

```json
GET /api/v1/analyses/{id}
{
  "status": "completed",
  "result": {
    "riskLevel": "high",
    "confidence": "high",
    "trustScore": 17,
    "summary": "...",
    "positiveSignals": [...],
    "warningSignals": [...],
    "evidence": [...],
    "recommendation": "..."
  }
}
```

Full endpoint and field reference is available via Swagger at `/swagger`.

### Demo mode

Include a marker anywhere in the submitted content to get an instant canned result with no external API calls — useful for frontend development and live demos:

- `[DEMO:LEGIT]` — low risk
- `[DEMO:SCAM]` — high risk
- `[DEMO:UNCERTAIN]` — unable to verify

## Known Limitations

- URL analysis reads static HTML only; JavaScript-rendered pages may yield little content
- Free-tier AI provider quotas apply; results may degrade to "unable to verify" under rate limiting
- Anonymous sessions are an identifier, not authentication
- In-memory job queue — not durable across restarts

## Roadmap

- Screenshot/image analysis
- Real accounts and personalized history
- Automated test coverage for the risk engine

