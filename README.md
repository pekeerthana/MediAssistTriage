# AI-Powered Patient Triage & Care Routing Agent

An intelligent, API-driven triage system built to assess patient-reported symptoms, classify severity, detect life-threatening red-flag patterns, and route patients to the appropriate medical department.

## 🏗 Architecture & Tech Stack
* **Framework:** C# 12 / .NET 8 (Minimal APIs for high-performance routing)
* **AI Orchestration:** Microsoft Semantic Kernel
* **LLM:** Google Gemini (`gemini-2.5-flash`)
* **Data Store:** In-memory JSON provider

**Architecture Flow:**
1. **Ingestion:** Validates incoming patient JSON payloads.
2. **Red-Flag Engine (Safety Override):** Deterministic, hard-coded checks bypass the LLM for critical indicators (e.g., Cardiac, Stroke, Pediatric Fever), forcing an `EMERGENCY` or `URGENT` status.
3. **LLM Assessment:** Analyzes symptoms to generate a triage level, confidence score, and natural-language reasoning. Includes a graceful fallback if the LLM is unreachable.
4. **Department Matcher:** Maps the triage level to an available department, triggering a capacity fallback flag if the primary department is full.

## 🚀 Setup & Installation

### Prerequisites
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* Google Gemini API Key

### 1. Clone the repository
```bash
git clone <your-repo-url>
cd MediAssistTriage