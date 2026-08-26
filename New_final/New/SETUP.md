# Arazzo Workflow Platform - Setup Guide

Complete guide for setting up the Arazzo Workflow Platform locally, with both Docker and non-Docker options.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Quick Start with Docker](#quick-start-with-docker)
3. [Manual Setup (Without Docker)](#manual-setup-without-docker)
4. [Configuration Reference](#configuration-reference)
5. [Troubleshooting](#troubleshooting)
6. [Architecture Overview](#architecture-overview)

---

## Prerequisites

### For Docker Setup
- **Docker Desktop** (v20.10+) - [Download](https://www.docker.com/products/docker-desktop/)
- **Docker Compose** (v2.0+) - Usually included with Docker Desktop
- **Git** - [Download](https://git-scm.com/downloads)
- **8GB+ RAM** recommended (for Ollama LLM)

### For Manual Setup
- **Node.js** (v18+) - [Download](https://nodejs.org/)
- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Ollama** - [Download](https://ollama.ai/)
- **Git** - [Download](https://git-scm.com/downloads)

### Optional (for full features)
- **PostgreSQL 16** - For production database (SQLite used by default)
- **Qdrant** - For vector search/RAG features

---

## Quick Start with Docker

### Step 1: Clone the Repository

```bash
git clone https://github.com/ASP-369/MCP_Unisys_New.git
cd MCP_Unisys_New/New_final/New
git checkout final-2
```

### Step 2: Start All Services

```bash
# Start all containers (first run will take ~5-10 minutes to download images)
docker-compose up -d

# Watch the logs to see startup progress
docker-compose logs -f
```

### Step 3: Pull Required AI Models

After the Ollama container is running, pull the required models:

```bash
# Pull the LLM model (Phi3 - ~2.3GB)
docker exec -it arazzo-phi-llm ollama pull phi3:latest

# Pull the embedding model for RAG (nomic-embed-text - ~274MB)
docker exec -it arazzo-phi-llm ollama pull nomic-embed-text
```

### Step 4: Access the Application

| Service | URL | Description |
|---------|-----|-------------|
| **Frontend** | http://localhost:3000 | Main application UI |
| **Backend API** | http://localhost:5000 | REST API |
| **API Docs** | http://localhost:5000/swagger | Swagger documentation |
| **Qdrant Dashboard** | http://localhost:6333/dashboard | Vector DB admin |
| **Jaeger UI** | http://localhost:16686 | Distributed tracing |

### Docker Commands Reference

```bash
# Start all services
docker-compose up -d

# Stop all services
docker-compose down

# Stop and remove all data (clean start)
docker-compose down -v

# View logs
docker-compose logs -f

# View logs for specific service
docker-compose logs -f backend

# Rebuild after code changes
docker-compose up -d --build

# Check service health
docker-compose ps
```

---

## Manual Setup (Without Docker)

### Step 1: Clone the Repository

```bash
git clone https://github.com/ASP-369/MCP_Unisys_New.git
cd MCP_Unisys_New/New_final/New
git checkout final-2
```

### Step 2: Install and Start Ollama

#### Windows
1. Download from https://ollama.ai/download
2. Run the installer
3. Open terminal and run:
```powershell
# Start Ollama server (runs on http://localhost:11434)
ollama serve

# In a new terminal, pull the models
ollama pull phi3:latest
ollama pull nomic-embed-text
```

#### macOS
```bash
brew install ollama
ollama serve

# In a new terminal
ollama pull phi3:latest
ollama pull nomic-embed-text
```

#### Linux
```bash
curl -fsSL https://ollama.ai/install.sh | sh
ollama serve

# In a new terminal
ollama pull phi3:latest
ollama pull nomic-embed-text
```

### Step 3: Install and Start Qdrant (Optional - for RAG)

#### Option A: Using Docker (Recommended)
```bash
docker run -d -p 6333:6333 -p 6334:6334 \
  -v qdrant_storage:/qdrant/storage \
  qdrant/qdrant:v1.7.4
```

#### Option B: Download Binary
1. Download from https://github.com/qdrant/qdrant/releases
2. Extract and run:
```bash
./qdrant
```

### Step 4: Setup Backend

```powershell
# Navigate to backend directory
cd src/backend

# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build

# Run the backend (starts on http://localhost:5000)
cd src/WebAPI
dotnet run
```

The backend will:
- Automatically create SQLite database (`arazzo_platform.db`)
- Run database migrations
- Connect to Ollama at `http://localhost:11434`
- Connect to Qdrant at `localhost:6334` (if available)

### Step 5: Setup Frontend

```powershell
# Open a new terminal
cd src/frontend

# Install dependencies
npm install

# Start development server (starts on http://localhost:3000)
npm start
```

### Step 6: Verify Setup

1. Open http://localhost:3000 in your browser
2. You should see the Arazzo Workflow Platform UI
3. Try uploading an OpenAPI specification file
4. Create a workflow to test the LLM integration

---

## Configuration Reference

### Backend Configuration (`src/backend/src/WebAPI/appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=arazzo_platform.db"
  },
  "Phi": {
    "Endpoint": "http://localhost:11434",
    "ModelName": "phi3:latest"
  },
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "EmbeddingModel": "nomic-embed-text",
    "VectorDimension": "768"
  },
  "Qdrant": {
    "Host": "localhost",
    "Port": "6334",
    "CollectionName": "openapi_embeddings"
  }
}
```

### Environment Variables (Docker)

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Development | .NET environment |
| `Phi__Endpoint` | http://phi-llm:11434 | Ollama LLM endpoint |
| `Phi__ModelName` | phi3:latest | LLM model to use |
| `Ollama__EmbeddingModel` | nomic-embed-text | Embedding model |
| `Qdrant__Endpoint` | http://qdrant:6333 | Qdrant endpoint |
| `POSTGRES_USER` | arazzo_user | PostgreSQL username |
| `POSTGRES_PASSWORD` | arazzo_password | PostgreSQL password |
| `POSTGRES_DB` | arazzo_platform | PostgreSQL database |

### Frontend Configuration

The frontend connects to the backend API. In development:
- Default API URL: `http://localhost:5000`

For production builds, set the environment variable:
```bash
REACT_APP_API_URL=http://your-backend-url:5000 npm run build
```

---

## Troubleshooting

### Common Issues

#### 1. Ollama Connection Failed
**Symptoms:** "Failed to connect to Ollama" or timeout errors

**Solutions:**
```bash
# Check if Ollama is running
curl http://localhost:11434

# Check if models are downloaded
ollama list

# Pull models if missing
ollama pull phi3:latest
ollama pull nomic-embed-text
```

#### 2. Backend Build Errors
**Symptoms:** .NET build fails

**Solutions:**
```powershell
# Clean and rebuild
cd src/backend
dotnet clean
dotnet restore
dotnet build
```

#### 3. Frontend Can't Connect to Backend
**Symptoms:** API errors in browser console

**Solutions:**
- Ensure backend is running on port 5000
- Check CORS settings in `Program.cs`
- Verify no firewall blocking

#### 4. Docker Container Won't Start
**Symptoms:** Container exits immediately

**Solutions:**
```bash
# Check logs
docker-compose logs backend

# Check resource limits
docker stats

# Increase Docker memory (Settings > Resources)
```

#### 5. Qdrant Connection Issues
**Symptoms:** Vector search not working

**Solutions:**
```bash
# Check Qdrant is running
curl http://localhost:6333

# In Docker
docker logs arazzo-qdrant
```

#### 6. Out of Memory
**Symptoms:** System slowdown, crashes

**Solutions:**
- Increase Docker memory allocation (8GB+ recommended)
- Use smaller LLM model: `ollama pull phi3:mini`
- Reduce worker processes

### Health Check Endpoints

```bash
# Backend health
curl http://localhost:5000/health

# Ollama health
curl http://localhost:11434

# Qdrant health
curl http://localhost:6333/
```

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        User Browser                              │
│                    http://localhost:3000                         │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Frontend (React + Nginx)                      │
│                       Port: 3000 (80)                            │
│  - File Upload UI                                                │
│  - Workflow Designer                                             │
│  - Chat Interface                                                │
│  - Response Refining                                             │
└───────────────────────────┬─────────────────────────────────────┘
                            │ REST API
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Backend API (.NET 8)                          │
│                        Port: 5000                                │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │ Controllers │  │  Services   │  │   CQRS      │              │
│  │ - Files     │  │ - Chunking  │  │ - Commands  │              │
│  │ - Workflows │  │ - Embedding │  │ - Queries   │              │
│  │ - Chat      │  │ - RAG       │  │ - Handlers  │              │
│  └─────────────┘  └─────────────┘  └─────────────┘              │
└─────────┬─────────────────┬─────────────────┬───────────────────┘
          │                 │                 │
          ▼                 ▼                 ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│   SQLite/PG     │ │     Qdrant      │ │     Ollama      │
│   Port: 5432    │ │  Port: 6333/34  │ │   Port: 11434   │
│                 │ │                 │ │                 │
│  - Files        │ │  - Embeddings   │ │  - phi3:latest  │
│  - Workflows    │ │  - Vector       │ │  - nomic-embed  │
│  - History      │ │    Search       │ │    -text        │
└─────────────────┘ └─────────────────┘ └─────────────────┘
```

### Data Flow for Large OpenAPI Files

```
1. Upload OpenAPI Spec
        │
        ▼
2. Detect Large File (>2000 tokens)
        │
        ▼
3. Chunk Document
   - By Operation
   - By Path  
   - By Schema
        │
        ▼
4. Generate Embeddings (nomic-embed-text)
        │
        ▼
5. Store in Qdrant Vector DB
        │
        ▼
6. User Creates Workflow
        │
        ▼
7. Semantic Search for Relevant Context
        │
        ▼
8. LLM Generates Workflow with Focused Context
```

---

## Development Tips

### Running Tests
```bash
cd src/backend
dotnet test
```

### Watching for Changes
```bash
# Backend (hot reload)
cd src/backend/src/WebAPI
dotnet watch run

# Frontend (auto-reload included)
cd src/frontend
npm start
```

### Building for Production

```bash
# Backend
cd src/backend
dotnet publish -c Release -o ./publish

# Frontend
cd src/frontend
npm run build
```

### Useful Docker Commands

```bash
# Enter container shell
docker exec -it arazzo-backend /bin/bash

# Check container resource usage
docker stats

# Prune unused resources
docker system prune -a
```

---

## Support

For issues and feature requests, please create an issue on GitHub:
https://github.com/ASP-369/MCP_Unisys_New/issues
