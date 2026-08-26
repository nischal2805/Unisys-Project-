# 5. Project Plan & Phased Roadmap

## Overview

This document outlines a detailed, phase-by-phase implementation plan for the Arazzo Workflow Generator Platform. Each phase builds upon the previous one, ensuring incremental delivery of value while maintaining system stability.

## Timeline Summary

- **Phase 0**: Foundation & Tooling (Week 1) - 5 days
- **Phase 1**: Ingestion & Retrieval (Weeks 2-3) - 10 days
- **Phase 2**: Core Orchestration (Weeks 4-5) - 10 days
- **Phase 3**: Service Generation & UI (Weeks 6-7) - 10 days
- **Phase 4**: Polish & Monitoring (Week 8) - 5 days

**Total Duration**: 8 weeks (40 working days)

---

## Phase 0: Foundation & Tooling (Week 1)

### Objectives
- Set up development infrastructure
- Establish CI/CD pipeline
- Implement observability stack
- Create project scaffolding

### Deliverables

#### 1. Repository Setup (Day 1)
- [x] Initialize Git repository with proper .gitignore
- [x] Set up branch protection rules
- [x] Create README with setup instructions
- [x] Define directory structure
- [x] Add LICENSE file
- [x] Create CONTRIBUTING.md

#### 2. Docker Compose Infrastructure (Days 1-2)
- [x] Create docker-compose.yml with all services
- [x] Configure PostgreSQL with initialization scripts
- [x] Set up Qdrant vector database
- [x] Configure Phi LLM container with Ollama
- [x] Set up Jaeger for tracing
- [x] Create health check endpoints
- [x] Write setup scripts (PowerShell & Bash)

#### 3. Observability Stack (Day 2)
- [ ] Configure OpenTelemetry SDK for .NET services
- [ ] Set up OpenTelemetry for React frontend
- [ ] Configure Jaeger exporter
- [ ] Create custom metrics
- [ ] Set up structured logging with Serilog
- [ ] Create log aggregation dashboard

#### 4. CI/CD Pipeline (Day 3)
- [ ] Set up GitHub Actions workflow
- [ ] Configure automated builds
- [ ] Set up automated testing
- [ ] Configure Docker image building
- [ ] Set up code quality checks (SonarQube/CodeQL)
- [ ] Create deployment automation

#### 5. Project Scaffolding (Days 4-5)
- [ ] Create .NET solution structure for all services
- [ ] Set up Clean Architecture projects (Domain, Application, Infrastructure, WebAPI)
- [ ] Configure Entity Framework Core with migrations
- [ ] Set up React project with TypeScript
- [ ] Configure Redux Toolkit
- [ ] Set up shared libraries and utilities
- [ ] Create initial API contracts (DTOs)

### Success Criteria
- ✅ All services start successfully with `docker-compose up`
- ✅ Health checks pass for all services
- ✅ Traces appear in Jaeger UI
- ✅ CI/CD pipeline runs successfully
- ✅ Database schema created and migrations work

### Technical Tasks

```powershell
# Day 1: Infrastructure
cd arazzo-workflow-platform
git init
# Copy provided files: docker-compose.yml, .env.example, setup.ps1
.\scripts\setup.ps1

# Day 2: Observability
# Install OpenTelemetry packages in all C# projects
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.OpenTelemetry

# Day 3: CI/CD
# Create .github/workflows/ci.yml

# Days 4-5: Scaffolding
dotnet new sln -n ArazzoWorkflowPlatform
dotnet new webapi -n BackendAPI.WebAPI
dotnet new classlib -n BackendAPI.Domain
dotnet new classlib -n BackendAPI.Application
dotnet new classlib -n BackendAPI.Infrastructure

npx create-react-app frontend --template typescript
cd frontend
npm install @reduxjs/toolkit react-redux axios
```

---

## Phase 1: Ingestion & Retrieval (Weeks 2-3)

### Objectives
- Build OpenAPI ingestion pipeline
- Implement vector storage in Qdrant
- Create context retrieval service
- Validate end-to-end knowledge retrieval

### Week 2: Ingest Service

#### Day 6-7: Core Ingestion Logic
**Tasks:**
- [ ] Create Ingest Service project structure
- [ ] Implement OpenAPI parser (using Swashbuckle/NSwag)
- [ ] Create file validation logic
- [ ] Build API endpoint: `POST /api/ingest`
- [ ] Write unit tests for parser

**Deliverables:**
```csharp
public class IngestController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> IngestOpenApiSpec(IFormFile file)
    {
        // Parse OpenAPI
        // Validate structure
        // Return ingestion job ID
    }
}
```

#### Day 8-9: Chunking Strategy
**Tasks:**
- [ ] Implement configurable chunking strategies:
  - ByPath: Each API path becomes a chunk
  - ByOperation: Each operation (GET, POST, etc.) is a chunk
  - BySchema: Each schema definition is a chunk
- [ ] Use Semantic Kernel's TextChunker
- [ ] Add chunk metadata (path, method, tags)
- [ ] Write integration tests

**Example:**
```csharp
public interface IChunkingStrategy
{
    IEnumerable<DocumentChunk> ChunkOpenApiSpec(OpenApiDocument doc);
}

public class ByPathChunkingStrategy : IChunkingStrategy
{
    public IEnumerable<DocumentChunk> ChunkOpenApiSpec(OpenApiDocument doc)
    {
        foreach (var path in doc.Paths)
        {
            yield return new DocumentChunk
            {
                Content = SerializePath(path),
                Metadata = new { Path = path.Key, Operations = path.Value.Operations.Keys }
            };
        }
    }
}
```

#### Day 10: Embedding Generation & Storage
**Tasks:**
- [ ] Integrate with Phi LLM via Semantic Kernel
- [ ] Generate embeddings for each chunk
- [ ] Store vectors in Qdrant with metadata
- [ ] Implement batch processing for large specs
- [ ] Add retry logic for LLM failures

**Code:**
```csharp
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("phi3", "http://phi-llm:11434")
    .Build();

var embedding = await kernel.GetService<ITextEmbeddingGenerationService>()
    .GenerateEmbeddingAsync(chunkContent);

await _qdrantClient.UpsertAsync("openapi_embeddings", new PointStruct
{
    Id = Guid.NewGuid(),
    Vector = embedding,
    Payload = new { path, method, content = chunkContent }
});
```

### Week 3: Context Retriever Service

#### Day 11-12: User Input Retriever
**Tasks:**
- [ ] Create Context Retriever service
- [ ] Implement chat history storage in PostgreSQL
- [ ] Build endpoint: `POST /api/retrieve/user-context`
- [ ] Query last N user messages
- [ ] Format context for LLM consumption

#### Day 13-14: Private Knowledge Retriever
**Tasks:**
- [ ] Implement vector search against Qdrant
- [ ] Build semantic search functionality
- [ ] Add relevance scoring
- [ ] Implement result re-ranking
- [ ] Cache frequent queries

**Endpoint:**
```csharp
[HttpPost("context")]
public async Task<ContextResult> RetrieveContext([FromBody] ContextRequest request)
{
    // 1. Get user chat history
    var userContext = await _historyRepo.GetRecentMessages(request.SessionId, 10);
    
    // 2. Generate query embedding
    var queryEmbedding = await _embeddingService.GenerateEmbedding(request.Query);
    
    // 3. Vector search in Qdrant
    var vectorResults = await _qdrantClient.SearchAsync("openapi_embeddings", 
        queryEmbedding, limit: 5);
    
    // 4. Aggregate and return
    return new ContextResult
    {
        UserHistory = userContext,
        RelevantSpecs = vectorResults,
        Metadata = new { QueryTime = DateTime.UtcNow }
    };
}
```

#### Day 15: Integration Testing
**Tasks:**
- [ ] End-to-end test: Upload → Ingest → Retrieve
- [ ] Validate embedding quality
- [ ] Test vector search accuracy
- [ ] Performance testing (response time < 500ms)
- [ ] Load testing with multiple concurrent requests

### Success Criteria
- ✅ Can upload OpenAPI spec and see it chunked in Qdrant
- ✅ Vector search returns relevant results
- ✅ Context retriever aggregates multiple sources
- ✅ All integration tests pass
- ✅ Performance targets met

---

## Phase 2: Core Orchestration (Weeks 4-5)

### Objectives
- Implement Semantic Kernel orchestration
- Build AI flows for workflow generation
- Generate Arazzo workflow JSON
- Store and version workflows

### Week 4: Semantic Kernel Integration

#### Day 16-17: Semantic Kernel Setup
**Tasks:**
- [ ] Install Semantic Kernel NuGet packages
- [ ] Configure Phi LLM connector
- [ ] Create kernel builder service
- [ ] Implement prompt templates
- [ ] Set up function calling/plugins

**Configuration:**
```csharp
services.AddSingleton(sp =>
{
    var builder = Kernel.CreateBuilder();
    
    builder.Services.AddOpenAIChatCompletion(
        modelId: "phi3:latest",
        endpoint: new Uri("http://phi-llm:11434"),
        apiKey: "not-needed-for-ollama"
    );
    
    builder.Plugins.AddFromType<WorkflowGenerationPlugin>();
    builder.Plugins.AddFromType<ContextRetrievalPlugin>();
    
    return builder.Build();
});
```

#### Day 18-19: Question Refining Flow
**Tasks:**
- [ ] Create prompt template for question refinement
- [ ] Implement flow to clarify user intent
- [ ] Extract entities from user request
- [ ] Generate clarifying questions if needed
- [ ] Store refined questions

**Prompt Template** (`refine-question.txt`):
```
You are a workflow architect assistant. Your task is to refine the user's question to be more specific and actionable.

User Question: {{$user_question}}

Previous Context: {{$context}}

Analyze the question and:
1. Identify the main intent
2. Extract key entities (API operations, data models)
3. If the question is vague, generate clarifying questions
4. Output a refined, actionable version

Refined Question:
```

**Implementation:**
```csharp
public class QuestionRefiningFlow
{
    private readonly Kernel _kernel;
    
    public async Task<RefinedQuestion> RefineAsync(string userQuestion, string context)
    {
        var function = _kernel.CreateFunctionFromPromptYaml(
            File.ReadAllText("Prompts/refine-question.txt"));
        
        var result = await _kernel.InvokeAsync(function, new KernelArguments
        {
            ["user_question"] = userQuestion,
            ["context"] = context
        });
        
        return ParseRefinedQuestion(result.ToString());
    }
}
```

#### Day 20: Context Retrieving Flow
**Tasks:**
- [ ] Create plugin to call Context Retriever service
- [ ] Implement automatic context aggregation
- [ ] Pass context to subsequent flows
- [ ] Handle context size limits

**Plugin:**
```csharp
public class ContextRetrievalPlugin
{
    [KernelFunction]
    [Description("Retrieves relevant context from OpenAPI specs and chat history")]
    public async Task<string> RetrieveContext(
        [Description("The user's query")] string query,
        [Description("Session ID for chat history")] string sessionId)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "http://context-retriever:5002/api/retrieve/context",
            new { query, sessionId });
        
        var context = await response.Content.ReadFromJsonAsync<ContextResult>();
        
        return FormatContextForLLM(context);
    }
}
```

### Week 5: Workflow Generation

#### Day 21-22: Task Breakdown Flow
**Tasks:**
- [ ] Create prompt for breaking down tasks into steps
- [ ] Implement step generation logic
- [ ] Map steps to OpenAPI operations
- [ ] Generate runtime expressions
- [ ] Validate step dependencies

**Prompt Template** (`breakdown-tasks.txt`):
```
You are generating an Arazzo workflow. Given the user's intent and available API operations, create workflow steps.

User Intent: {{$user_intent}}

Available OpenAPI Operations:
{{$openapi_operations}}

Previous Steps:
{{$previous_steps}}

Generate the next step in JSON format following the Arazzo Step schema:
{
  "stepId": "unique-step-id",
  "description": "What this step does",
  "operationId": "from OpenAPI spec",
  "parameters": [...],
  "successCriteria": [...],
  "onSuccess": [...],
  "onFailure": [...]
}

Step JSON:
```

#### Day 23-24: Result Summarizing Flow
**Tasks:**
- [ ] Aggregate all generated steps
- [ ] Create complete Arazzo workflow object
- [ ] Add workflow metadata (info, sourceDescriptions)
- [ ] Validate against Arazzo schema
- [ ] Store in PostgreSQL

**Implementation:**
```csharp
public class ResultSummarizingFlow
{
    public async Task<ArazzoWorkflow> SummarizeWorkflow(
        List<WorkflowStep> steps,
        WorkflowMetadata metadata)
    {
        var workflow = new ArazzoWorkflow
        {
            Arazzo = "1.0.0",
            Info = new WorkflowInfo
            {
                Title = metadata.Name,
                Description = metadata.Description,
                Version = "1.0.0"
            },
            SourceDescriptions = new List<SourceDescription>
            {
                new() { Name = "api", Url = metadata.OpenApiUrl, Type = "openapi" }
            },
            Workflows = new List<Workflow>
            {
                new()
                {
                    WorkflowId = metadata.WorkflowId,
                    Steps = steps
                }
            }
        };
        
        // Validate
        var validator = new ArazzoWorkflowValidator();
        var validationResult = await validator.ValidateAsync(workflow);
        
        if (!validationResult.IsValid)
            throw new InvalidWorkflowException(validationResult.Errors);
        
        // Store
        await _workflowRepository.SaveAsync(workflow);
        
        return workflow;
    }
}
```

#### Day 25: Integration & Testing
**Tasks:**
- [ ] End-to-end workflow generation test
- [ ] Test with multiple OpenAPI specs
- [ ] Validate generated workflows
- [ ] Performance optimization
- [ ] Error handling refinement

### Success Criteria
- ✅ Can generate complete Arazzo workflow from chat
- ✅ Workflows pass schema validation
- ✅ Steps correctly reference OpenAPI operations
- ✅ Runtime expressions properly formed
- ✅ Workflows stored in database

---

## Phase 3: Service Generation & UI (Weeks 6-7)

### Week 6: Service Generator

#### Day 26-27: Scriban Template Engine
**Tasks:**
- [ ] Install Scriban NuGet package
- [ ] Create C# code generation templates
- [ ] Implement template rendering logic
- [ ] Handle template inheritance
- [ ] Add helper functions

**Controller Template Example** (`controller.scriban`):
```scriban
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace {{ namespace }}.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class {{ workflow.workflow_id | pascal_case }}Controller : ControllerBase
    {
        {{ for step in workflow.steps }}
        [Http{{ step.operation_method }}("{{ step.operation_path }}")]
        public async Task<IActionResult> {{ step.step_id | pascal_case }}(
            {{ for param in step.parameters }}
            [From{{ param.in | pascal_case }}] {{ param.type }} {{ param.name }}{{ if !for.last }},{{ end }}
            {{ end }}
        )
        {
            // TODO: Implement {{ step.description }}
            {{ if step.success_criteria }}
            // Success criteria: {{ step.success_criteria[0].condition }}
            {{ end }}
            
            return Ok();
        }
        {{ end }}
    }
}
```

#### Day 28-29: Code Generation Pipeline
**Tasks:**
- [ ] Generate Controllers from workflow steps
- [ ] Generate Domain models
- [ ] Generate Application services
- [ ] Generate Dockerfile
- [ ] Generate docker-compose.yml
- [ ] Create README for generated service

**Service Generator Endpoint:**
```csharp
[HttpPost("generate")]
public async Task<IActionResult> GenerateService([FromBody] GenerateRequest request)
{
    // 1. Fetch workflow from database
    var workflow = await _workflowRepo.GetByIdAsync(request.WorkflowId);
    
    // 2. Load templates
    var templates = await _templateLoader.LoadAllTemplatesAsync();
    
    // 3. Render each template
    var generatedFiles = new List<GeneratedFile>();
    foreach (var template in templates)
    {
        var rendered = await _templateEngine.RenderAsync(template, workflow);
        generatedFiles.Add(new GeneratedFile
        {
            Path = template.OutputPath,
            Content = rendered
        });
    }
    
    // 4. Create project structure
    var projectStructure = _projectBuilder.BuildStructure(generatedFiles);
    
    // 5. Compile to verify
    var compilationResult = await _compiler.CompileAsync(projectStructure);
    if (!compilationResult.Success)
        return BadRequest(compilationResult.Errors);
    
    // 6. Create ZIP archive
    var zipStream = await _zipService.CreateArchiveAsync(projectStructure);
    
    // 7. Store metadata
    await _generatedServiceRepo.SaveAsync(new GeneratedService
    {
        WorkflowId = request.WorkflowId,
        FilePath = $"/generated/{request.WorkflowId}.zip",
        GeneratedAt = DateTime.UtcNow
    });
    
    return File(zipStream, "application/zip", $"{workflow.Name}.zip");
}
```

#### Day 30: Testing & Documentation
**Tasks:**
- [ ] Generate sample services
- [ ] Verify compilability
- [ ] Test generated APIs
- [ ] Create generation documentation
- [ ] Add customization options

### Week 7: Frontend Development

#### Day 31-32: Core Components
**Tasks:**
- [ ] Build FileUploadPage with drag-and-drop
- [ ] Create ChatPane with message history
- [ ] Implement WorkflowVisualization component
- [ ] Build MonitorPage dashboard
- [ ] Add loading states and error handling

#### Day 33-34: Redux Integration
**Tasks:**
- [ ] Implement all Redux slices
- [ ] Connect components to Redux
- [ ] Add async thunks for API calls
- [ ] Implement optimistic updates
- [ ] Add error recovery

#### Day 35: Polish & Integration
**Tasks:**
- [ ] Style with CSS/Tailwind
- [ ] Add animations and transitions
- [ ] Implement responsive design
- [ ] Test all user flows
- [ ] Fix bugs and UX issues

### Success Criteria
- ✅ Can generate compilable C# service from workflow
- ✅ Generated service includes all required files
- ✅ Frontend allows complete workflow creation
- ✅ UI is responsive and user-friendly
- ✅ All features work end-to-end

---

## Phase 4: Polish & Monitoring (Week 8)

### Day 36-37: Progress Monitor Implementation
**Tasks:**
- [ ] Build Progress Monitor service
- [ ] Consume OpenTelemetry traces
- [ ] Store business events in database
- [ ] Create progress calculation logic
- [ ] Build monitoring dashboard

### Day 38: Error Handling & Resiliency
**Tasks:**
- [ ] Implement Polly policies across all services
- [ ] Add circuit breakers
- [ ] Improve error messages
- [ ] Add user-friendly error pages
- [ ] Implement retry logic

### Day 39: Testing & Documentation
**Tasks:**
- [ ] Write comprehensive unit tests (target: 80% coverage)
- [ ] Create integration test suite
- [ ] Write API documentation
- [ ] Create user guide
- [ ] Record demo video

### Day 40: Final Polish & Release
**Tasks:**
- [ ] Performance optimization
- [ ] Security audit
- [ ] Load testing
- [ ] Final bug fixes
- [ ] Release v1.0.0

### Success Criteria
- ✅ 80%+ test coverage
- ✅ All documentation complete
- ✅ Performance targets met
- ✅ Production-ready release

---

## Resource Allocation

### Team Structure (Recommended)

1. **Backend Lead** (2 developers)
   - Main API service
   - Semantic Kernel orchestration
   - Database design

2. **Services Team** (2 developers)
   - Ingest Service
   - Context Retriever
   - Service Generator

3. **Frontend Developer** (1 developer)
   - React components
   - Redux state management
   - UI/UX

4. **DevOps Engineer** (1 developer)
   - Docker infrastructure
   - CI/CD pipeline
   - Observability setup

5. **QA Engineer** (1 developer)
   - Test automation
   - Integration testing
   - Performance testing

**Total Team Size**: 7 developers

---

## Risk Mitigation

### Identified Risks

1. **LLM Performance**: Phi may be too slow or inaccurate
   - **Mitigation**: Test early, consider model swapping capability

2. **Vector Search Quality**: Embeddings may not capture semantic meaning
   - **Mitigation**: Implement multiple chunking strategies, test with real specs

3. **Code Generation Complexity**: Templates may not cover all cases
   - **Mitigation**: Start with simple workflows, iterate based on feedback

4. **Docker Resource Constraints**: Services may require more than 16GB RAM
   - **Mitigation**: Document minimum requirements, optimize container limits

---

## Success Metrics

### Phase 0
- Infrastructure uptime: 99%
- All health checks passing

### Phase 1
- Ingestion success rate: >95%
- Vector search precision: >70%
- API response time: <500ms

### Phase 2
- Workflow generation success: >90%
- Valid Arazzo output: 100%
- User satisfaction: >4/5

### Phase 3
- Generated code compilability: 100%
- Test coverage: >80%
- UI responsiveness: <200ms interactions

### Phase 4
- System stability: 99.9%
- Zero critical bugs
- Complete documentation

---

## Next Steps After v1.0

1. **Enhanced LLM Support**: Add support for GPT-4, Claude
2. **Workflow Execution Engine**: Actually run the generated workflows
3. **Multi-tenancy**: Support multiple users/organizations
4. **Cloud Deployment**: Kubernetes manifests for production
5. **Workflow Marketplace**: Share and discover workflows

---

**End of Roadmap**
