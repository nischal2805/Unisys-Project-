# Detailed Service & Component Breakdown

## Table of Contents

1. [Frontend (React & Redux)](#frontend-react--redux)
2. [Backend API Service](#backend-api-service)
3. [Ingest Service](#ingest-service)
4. [Context Retriever Service](#context-retriever-service)
5. [Service Generator](#service-generator)

---

## Frontend (React & Redux)

### Component Structure

#### 1. FileUploadPage Component

**Purpose**: Handle OpenAPI specification file uploads

**Props Interface**:
```typescript
interface FileUploadPageProps {
  onUploadSuccess: (fileId: string, fileName: string) => void;
  maxFileSize?: number; // Default: 10MB
  acceptedFormats?: string[]; // Default: ['.yaml', '.yml', '.json']
}
```

**Key Features**:
- Drag-and-drop file upload
- File validation (format, size)
- Upload progress indicator
- Previous uploads list
- File preview

**Component Structure**:
```typescript
<FileUploadPage>
  <DropZone onDrop={handleFileDrop} />
  <FileValidator />
  <ProgressBar progress={uploadProgress} />
  <UploadHistory items={previousUploads} />
  <FilePreview content={fileContent} />
</FileUploadPage>
```

#### 2. ChatPane Component

**Purpose**: Interactive chat interface for workflow development

**Props Interface**:
```typescript
interface ChatPaneProps {
  workflowId: string;
  sessionId: string;
  onWorkflowUpdate: (workflow: ArazzoWorkflow) => void;
  initialMessages?: ChatMessage[];
}

interface ChatMessage {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: Date;
  metadata?: {
    workflowStepGenerated?: boolean;
    contextUsed?: string[];
  };
}
```

**Key Features**:
- Real-time message streaming
- Markdown rendering for LLM responses
- Code syntax highlighting
- Workflow visualization panel
- Message history with infinite scroll
- Typing indicators
- Error handling and retry

**Component Structure**:
```typescript
<ChatPane>
  <MessageList messages={messages}>
    <MessageBubble />
    <CodeBlock />
    <WorkflowStepCard />
  </MessageList>
  <WorkflowVisualization workflow={currentWorkflow} />
  <ChatInput 
    onSend={handleSendMessage}
    placeholder="Describe your workflow..."
    disabled={isProcessing}
  />
  <SuggestedPrompts prompts={suggestions} />
</ChatPane>
```

#### 3. ResponseRefiningPage Component

**Purpose**: Review and refine generated Arazzo workflows

**Props Interface**:
```typescript
interface ResponseRefiningPageProps {
  workflow: ArazzoWorkflow;
  onSave: (workflow: ArazzoWorkflow) => void;
  onRegenerate: (stepIndex: number) => void;
  validationErrors?: ValidationError[];
}
```

**Key Features**:
- JSON editor with syntax highlighting
- Visual workflow builder (drag-and-drop)
- Step-by-step validation
- Comparison view (before/after)
- Test workflow functionality
- Export options (JSON, YAML)

**Component Structure**:
```typescript
<ResponseRefiningPage>
  <WorkflowEditor>
    <JsonEditor value={workflowJson} onChange={handleChange} />
    <VisualBuilder steps={workflow.steps} />
  </WorkflowEditor>
  <ValidationPanel errors={validationErrors} />
  <WorkflowTester workflow={workflow} />
  <ActionBar>
    <Button onClick={handleSave}>Save</Button>
    <Button onClick={handleRegenerate}>Regenerate</Button>
    <Button onClick={handleExport}>Export</Button>
  </ActionBar>
</ResponseRefiningPage>
```

#### 4. MonitorPage Component

**Purpose**: Track workflow progress and system health

**Props Interface**:
```typescript
interface MonitorPageProps {
  workflowId?: string;
  refreshInterval?: number; // Default: 5000ms
}

interface WorkflowProgress {
  workflowId: string;
  name: string;
  status: 'Draft' | 'InProgress' | 'Completed' | 'Failed';
  currentStep: number;
  totalSteps: number;
  interactions: InteractionHistory[];
  createdAt: Date;
  updatedAt: Date;
}
```

**Key Features**:
- Real-time progress tracking
- Interaction history timeline
- System health indicators
- Performance metrics
- Error logs
- Export progress report

**Component Structure**:
```typescript
<MonitorPage>
  <WorkflowStatusCard workflow={currentWorkflow} />
  <ProgressTimeline interactions={interactions} />
  <SystemHealthDashboard>
    <ServiceHealthIndicator service="backend-api" />
    <ServiceHealthIndicator service="ingest-service" />
    <ServiceHealthIndicator service="context-retriever" />
    <ServiceHealthIndicator service="service-generator" />
  </SystemHealthDashboard>
  <MetricsPanel metrics={performanceMetrics} />
  <LogViewer logs={errorLogs} />
</MonitorPage>
```

### State Management (Redux Toolkit)

#### Slice Structure

**1. fileSlice**

```typescript
interface FileState {
  uploads: UploadedFile[];
  currentFile: UploadedFile | null;
  uploadProgress: number;
  loading: boolean;
  error: string | null;
}

interface UploadedFile {
  id: string;
  name: string;
  size: number;
  uploadedAt: Date;
  status: 'processing' | 'ready' | 'error';
  url: string;
}

// Actions
const fileSlice = createSlice({
  name: 'file',
  initialState,
  reducers: {
    setUploadProgress: (state, action: PayloadAction<number>) => {
      state.uploadProgress = action.payload;
    },
    setCurrentFile: (state, action: PayloadAction<UploadedFile>) => {
      state.currentFile = action.payload;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(uploadFile.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(uploadFile.fulfilled, (state, action) => {
        state.loading = false;
        state.uploads.unshift(action.payload);
        state.currentFile = action.payload;
      })
      .addCase(uploadFile.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Upload failed';
      });
  },
});

// Async Thunks
export const uploadFile = createAsyncThunk(
  'file/upload',
  async (file: File, { dispatch }) => {
    const formData = new FormData();
    formData.append('file', file);
    
    const response = await apiClient.post('/api/files/upload', formData, {
      onUploadProgress: (progressEvent) => {
        const progress = Math.round(
          (progressEvent.loaded * 100) / progressEvent.total
        );
        dispatch(fileSlice.actions.setUploadProgress(progress));
      },
    });
    
    return response.data;
  }
);
```

**2. chatSlice**

```typescript
interface ChatState {
  sessions: Record<string, ChatSession>;
  currentSessionId: string | null;
  isTyping: boolean;
  error: string | null;
}

interface ChatSession {
  sessionId: string;
  workflowId: string;
  messages: ChatMessage[];
  createdAt: Date;
}

const chatSlice = createSlice({
  name: 'chat',
  initialState,
  reducers: {
    addMessage: (state, action: PayloadAction<ChatMessage>) => {
      const session = state.sessions[state.currentSessionId!];
      session.messages.push(action.payload);
    },
    setTyping: (state, action: PayloadAction<boolean>) => {
      state.isTyping = action.payload;
    },
    createSession: (state, action: PayloadAction<string>) => {
      const sessionId = crypto.randomUUID();
      state.sessions[sessionId] = {
        sessionId,
        workflowId: action.payload,
        messages: [],
        createdAt: new Date(),
      };
      state.currentSessionId = sessionId;
    },
  },
});

// Async Thunks
export const sendMessage = createAsyncThunk(
  'chat/sendMessage',
  async ({ message, sessionId, workflowId }: SendMessagePayload, { dispatch }) => {
    // Add user message immediately
    dispatch(chatSlice.actions.addMessage({
      id: crypto.randomUUID(),
      role: 'user',
      content: message,
      timestamp: new Date(),
    }));
    
    dispatch(chatSlice.actions.setTyping(true));
    
    const response = await apiClient.post('/api/chat/message', {
      message,
      sessionId,
      workflowId,
    });
    
    dispatch(chatSlice.actions.setTyping(false));
    
    // Add assistant response
    dispatch(chatSlice.actions.addMessage({
      id: response.data.messageId,
      role: 'assistant',
      content: response.data.content,
      timestamp: new Date(),
      metadata: response.data.metadata,
    }));
    
    return response.data;
  }
);
```

**3. workflowSlice**

```typescript
interface WorkflowState {
  workflows: Record<string, ArazzoWorkflow>;
  currentWorkflowId: string | null;
  generationStatus: 'idle' | 'generating' | 'completed' | 'failed';
  validationErrors: ValidationError[];
  loading: boolean;
}

const workflowSlice = createSlice({
  name: 'workflow',
  initialState,
  reducers: {
    updateWorkflow: (state, action: PayloadAction<ArazzoWorkflow>) => {
      state.workflows[action.payload.id] = action.payload;
    },
    setCurrentWorkflow: (state, action: PayloadAction<string>) => {
      state.currentWorkflowId = action.payload;
    },
    setValidationErrors: (state, action: PayloadAction<ValidationError[]>) => {
      state.validationErrors = action.payload;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(generateService.pending, (state) => {
        state.generationStatus = 'generating';
      })
      .addCase(generateService.fulfilled, (state) => {
        state.generationStatus = 'completed';
      })
      .addCase(generateService.rejected, (state) => {
        state.generationStatus = 'failed';
      });
  },
});

// Async Thunks
export const generateService = createAsyncThunk(
  'workflow/generateService',
  async (workflowId: string) => {
    const response = await apiClient.post(
      `/api/workflows/${workflowId}/generate`,
      {},
      { responseType: 'blob' }
    );
    
    // Trigger download
    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `generated-service-${workflowId}.zip`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    
    return response.data;
  }
);
```

**4. monitorSlice**

```typescript
interface MonitorState {
  progress: Record<string, WorkflowProgress>;
  systemHealth: SystemHealth;
  metrics: PerformanceMetrics;
  refreshInterval: number;
}

const monitorSlice = createSlice({
  name: 'monitor',
  initialState,
  reducers: {
    updateProgress: (state, action: PayloadAction<WorkflowProgress>) => {
      state.progress[action.payload.workflowId] = action.payload;
    },
    updateSystemHealth: (state, action: PayloadAction<SystemHealth>) => {
      state.systemHealth = action.payload;
    },
  },
});

// Async Thunks with polling
export const startProgressMonitoring = createAsyncThunk(
  'monitor/startProgress',
  async (workflowId: string, { dispatch, getState }) => {
    const intervalId = setInterval(async () => {
      const response = await apiClient.get(`/api/workflows/${workflowId}/progress`);
      dispatch(monitorSlice.actions.updateProgress(response.data));
      
      if (response.data.status === 'Completed' || response.data.status === 'Failed') {
        clearInterval(intervalId);
      }
    }, 5000);
    
    return intervalId;
  }
);
```

### API Client Design

**apiClient.ts**

```typescript
import axios, { AxiosInstance, AxiosRequestConfig } from 'axios';
import axiosRetry from 'axios-retry';

// Create axios instance
const apiClient: AxiosInstance = axios.create({
  baseURL: process.env.REACT_APP_API_URL || 'http://localhost:5000',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Configure retry policy
axiosRetry(apiClient, {
  retries: 3,
  retryDelay: axiosRetry.exponentialDelay,
  retryCondition: (error) => {
    return axiosRetry.isNetworkOrIdempotentRequestError(error) 
      || error.response?.status === 429 
      || error.response?.status === 503;
  },
});

// Request interceptor - Add API key
apiClient.interceptors.request.use(
  (config) => {
    const apiKey = process.env.REACT_APP_API_KEY;
    if (apiKey) {
      config.headers['X-API-Key'] = apiKey;
    }
    
    // Add trace ID for distributed tracing
    const traceId = crypto.randomUUID();
    config.headers['X-Trace-Id'] = traceId;
    
    // Start performance tracking
    config.metadata = { startTime: new Date() };
    
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor - Handle errors
apiClient.interceptors.response.use(
  (response) => {
    // Log performance metrics
    const duration = new Date().getTime() - response.config.metadata.startTime.getTime();
    console.log(`API call to ${response.config.url} took ${duration}ms`);
    
    return response;
  },
  (error) => {
    // Handle specific error cases
    if (error.response) {
      switch (error.response.status) {
        case 401:
          // Unauthorized - invalid API key
          console.error('Invalid API key');
          break;
        case 429:
          // Too many requests - rate limited
          console.error('Rate limit exceeded');
          break;
        case 500:
          // Server error
          console.error('Server error', error.response.data);
          break;
      }
    } else if (error.request) {
      // Network error
      console.error('Network error - service unavailable');
    }
    
    return Promise.reject(error);
  }
);

// Circuit breaker implementation
class CircuitBreaker {
  private failureCount = 0;
  private readonly threshold = 5;
  private readonly timeout = 30000;
  private state: 'CLOSED' | 'OPEN' | 'HALF_OPEN' = 'CLOSED';
  private nextAttempt = Date.now();

  async execute<T>(fn: () => Promise<T>): Promise<T> {
    if (this.state === 'OPEN') {
      if (Date.now() < this.nextAttempt) {
        throw new Error('Circuit breaker is OPEN');
      }
      this.state = 'HALF_OPEN';
    }

    try {
      const result = await fn();
      this.onSuccess();
      return result;
    } catch (error) {
      this.onFailure();
      throw error;
    }
  }

  private onSuccess() {
    this.failureCount = 0;
    this.state = 'CLOSED';
  }

  private onFailure() {
    this.failureCount++;
    if (this.failureCount >= this.threshold) {
      this.state = 'OPEN';
      this.nextAttempt = Date.now() + this.timeout;
    }
  }
}

export const circuitBreaker = new CircuitBreaker();

export default apiClient;
```

---

## Backend API Service

### Project Structure (Clean Architecture)

```
src/backend/
├── Domain/                              # Core business entities
│   ├── Entities/
│   │   ├── Workflow.cs
│   │   ├── WorkflowStep.cs
│   │   ├── InteractionHistory.cs
│   │   └── UploadedFile.cs
│   ├── ValueObjects/
│   │   ├── ArazzoWorkflow.cs
│   │   ├── WorkflowStatus.cs
│   │   └── StepType.cs
│   ├── Interfaces/
│   │   ├── IWorkflowRepository.cs
│   │   ├── IFileRepository.cs
│   │   └── IInteractionRepository.cs
│   └── Exceptions/
│       ├── WorkflowNotFoundException.cs
│       └── InvalidWorkflowException.cs
│
├── Application/                         # Business logic & orchestration
│   ├── Common/
│   │   ├── Interfaces/
│   │   │   ├── ISemanticKernelService.cs
│   │   │   ├── IIngestService.cs
│   │   │   ├── IContextRetriever.cs
│   │   │   └── IServiceGenerator.cs
│   │   └── Models/
│   │       ├── ChatRequest.cs
│   │       ├── ChatResponse.cs
│   │       └── ContextResult.cs
│   │
│   ├── Features/
│   │   ├── FileUpload/
│   │   │   ├── Commands/
│   │   │   │   └── UploadFileCommand.cs
│   │   │   └── Handlers/
│   │   │       └── UploadFileCommandHandler.cs
│   │   │
│   │   ├── Chat/
│   │   │   ├── Commands/
│   │   │   │   └── SendMessageCommand.cs
│   │   │   └── Handlers/
│   │   │       └── SendMessageCommandHandler.cs
│   │   │
│   │   ├── Workflow/
│   │   │   ├── Queries/
│   │   │   │   ├── GetWorkflowQuery.cs
│   │   │   │   └── GetWorkflowProgressQuery.cs
│   │   │   ├── Commands/
│   │   │   │   ├── CreateWorkflowCommand.cs
│   │   │   │   ├── UpdateWorkflowCommand.cs
│   │   │   │   └── GenerateServiceCommand.cs
│   │   │   └── Handlers/
│   │   │       ├── GetWorkflowQueryHandler.cs
│   │   │       ├── CreateWorkflowCommandHandler.cs
│   │   │       └── GenerateServiceCommandHandler.cs
│   │   │
│   │   └── Monitor/
│   │       ├── Queries/
│   │       │   └── GetSystemHealthQuery.cs
│   │       └── Handlers/
│   │           └── GetSystemHealthQueryHandler.cs
│   │
│   └── Orchestration/                   # Semantic Kernel orchestration
│       ├── Flows/
│       │   ├── QuestionRefiningFlow.cs
│       │   ├── ContextRetrievingFlow.cs
│       │   ├── TaskBreakdownFlow.cs
│       │   └── ResultSummarizingFlow.cs
│       ├── Plugins/
│       │   ├── WorkflowGenerationPlugin.cs
│       │   └── ContextRetrievalPlugin.cs
│       └── Prompts/
│           ├── refine-question.txt
│           ├── breakdown-tasks.txt
│           └── summarize-workflow.txt
│
├── Infrastructure/                      # External concerns
│   ├── Persistence/
│   │   ├── ApplicationDbContext.cs
│   │   ├── Repositories/
│   │   │   ├── WorkflowRepository.cs
│   │   │   ├── FileRepository.cs
│   │   │   └── InteractionRepository.cs
│   │   └── Configurations/
│   │       ├── WorkflowConfiguration.cs
│   │       └── InteractionConfiguration.cs
│   │
│   ├── Services/
│   │   ├── SemanticKernelService.cs
│   │   ├── HttpClients/
│   │   │   ├── IngestServiceClient.cs
│   │   │   ├── ContextRetrieverClient.cs
│   │   │   └── ServiceGeneratorClient.cs
│   │   └── FileStorageService.cs
│   │
│   ├── Resiliency/
│   │   ├── PollyPolicies.cs
│   │   └── CircuitBreakerRegistry.cs
│   │
│   └── Telemetry/
│       ├── OpenTelemetryConfiguration.cs
│       └── CustomMetrics.cs
│
└── WebAPI/                              # Entry point
    ├── Controllers/
    │   ├── FileController.cs
    │   ├── ChatController.cs
    │   ├── WorkflowController.cs
    │   └── MonitorController.cs
    │
    ├── Middleware/
    │   ├── ApiKeyAuthenticationMiddleware.cs
    │   ├── ExceptionHandlingMiddleware.cs
    │   └── RequestLoggingMiddleware.cs
    │
    ├── Filters/
    │   └── ValidateModelStateFilter.cs
    │
    ├── Extensions/
    │   └── ServiceCollectionExtensions.cs
    │
    ├── appsettings.json
    ├── appsettings.Development.json
    ├── Program.cs
    └── Dockerfile
```

### API Controllers & Endpoints

#### FileController.cs

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ApiKeyPolicy")]
public class FileController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FileController> _logger;

    [HttpPost("upload")]
    [RequestSizeLimit(10_485_760)] // 10MB
    [ProducesResponseType(typeof(UploadFileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFile(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        using var activity = Activity.Current?.Source.StartActivity("UploadFile");
        activity?.SetTag("file.name", file.FileName);
        activity?.SetTag("file.size", file.Length);

        var command = new UploadFileCommand
        {
            File = file,
            UploadedBy = User.Identity?.Name ?? "anonymous"
        };

        var result = await _mediator.Send(command, cancellationToken);
        
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UploadedFileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFiles(CancellationToken cancellationToken)
    {
        var query = new GetFilesQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{fileId}")]
    [ProducesResponseType(typeof(UploadedFileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(
        [FromRoute] Guid fileId,
        CancellationToken cancellationToken)
    {
        var query = new GetFileQuery { FileId = fileId };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{fileId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFile(
        [FromRoute] Guid fileId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteFileCommand { FileId = fileId };
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
```

#### ChatController.cs

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ApiKeyPolicy")]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ChatController> _logger;

    [HttpPost("message")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendMessage(
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        using var activity = Activity.Current?.Source.StartActivity("SendChatMessage");
        activity?.SetTag("session.id", request.SessionId);
        activity?.SetTag("workflow.id", request.WorkflowId);

        var command = new SendMessageCommand
        {
            Message = request.Message,
            SessionId = request.SessionId,
            WorkflowId = request.WorkflowId
        };

        var result = await _mediator.Send(command, cancellationToken);
        
        return Ok(result);
    }

    [HttpGet("history/{sessionId}")]
    [ProducesResponseType(typeof(IEnumerable<ChatMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        [FromRoute] Guid sessionId,
        CancellationToken cancellationToken)
    {
        var query = new GetChatHistoryQuery { SessionId = sessionId };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
```

#### WorkflowController.cs

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ApiKeyPolicy")]
public class WorkflowController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost]
    [ProducesResponseType(typeof(WorkflowDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateWorkflow(
        [FromBody] CreateWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateWorkflowCommand
        {
            Name = request.Name,
            Description = request.Description,
            FileId = request.FileId
        };

        var result = await _mediator.Send(command, cancellationToken);
        
        return CreatedAtAction(
            nameof(GetWorkflow),
            new { workflowId = result.Id },
            result);
    }

    [HttpGet("{workflowId}")]
    [ProducesResponseType(typeof(WorkflowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkflow(
        [FromRoute] Guid workflowId,
        CancellationToken cancellationToken)
    {
        var query = new GetWorkflowQuery { WorkflowId = workflowId };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{workflowId}/progress")]
    [ProducesResponseType(typeof(WorkflowProgressDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProgress(
        [FromRoute] Guid workflowId,
        CancellationToken cancellationToken)
    {
        var query = new GetWorkflowProgressQuery { WorkflowId = workflowId };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{workflowId}/generate")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateService(
        [FromRoute] Guid workflowId,
        CancellationToken cancellationToken)
    {
        var command = new GenerateServiceCommand { WorkflowId = workflowId };
        var result = await _mediator.Send(command, cancellationToken);
        
        return File(
            result.FileContent,
            "application/zip",
            $"generated-service-{workflowId}.zip");
    }

    [HttpPut("{workflowId}")]
    [ProducesResponseType(typeof(WorkflowDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateWorkflow(
        [FromRoute] Guid workflowId,
        [FromBody] UpdateWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateWorkflowCommand
        {
            WorkflowId = workflowId,
            ArazzoWorkflow = request.ArazzoWorkflow
        };

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
```

### (Continued in next message due to length...)
