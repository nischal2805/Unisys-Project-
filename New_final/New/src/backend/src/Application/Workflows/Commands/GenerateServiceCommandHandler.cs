using ArazzoWorkflowPlatform.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Application.Workflows.Commands;

public class GenerateServiceCommandHandler : IRequestHandler<GenerateServiceCommand, GenerateServiceResponse>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly ILogger<GenerateServiceCommandHandler> _logger;

    public GenerateServiceCommandHandler(
        IWorkflowRepository workflowRepository,
        ILogger<GenerateServiceCommandHandler> logger)
    {
        _workflowRepository = workflowRepository;
        _logger = logger;
    }

    public async Task<GenerateServiceResponse> Handle(GenerateServiceCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdAsync(request.WorkflowId, cancellationToken);
        if (workflow == null)
            throw new ArgumentException($"Workflow with ID {request.WorkflowId} not found");

        _logger.LogInformation("[CODEGEN] Generating service code for workflow: {WorkflowName}", workflow.Name);

        // Parse the Arazzo JSON to extract workflow details
        var arazzoDoc = JsonDocument.Parse(workflow.ArazzoJson);
        var workflowInfo = ExtractWorkflowInfo(arazzoDoc);

        // Generate the C# service code
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // Generate solution file
            AddFileToArchive(archive, $"{workflowInfo.ServiceName}/{workflowInfo.ServiceName}.sln", 
                GenerateSolutionFile(workflowInfo));

            // Generate project file
            AddFileToArchive(archive, $"{workflowInfo.ServiceName}/{workflowInfo.ServiceName}.csproj", 
                GenerateProjectFile(workflowInfo));

            // Generate Program.cs
            AddFileToArchive(archive, $"{workflowInfo.ServiceName}/Program.cs", 
                GenerateProgramCs(workflowInfo));

            // Generate appsettings.json
            AddFileToArchive(archive, $"{workflowInfo.ServiceName}/appsettings.json", 
                GenerateAppSettings(workflowInfo));

            // Generate Workflow Service
            AddFileToArchive(archive, $"{workflowInfo.ServiceName}/Services/{workflowInfo.ServiceName}Service.cs", 
                GenerateWorkflowService(workflowInfo));

            // Generate DTOs
            AddFileToArchive(archive, $"{workflowInfo.ServiceName}/Models/WorkflowModels.cs", 
                GenerateModels(workflowInfo));

            // Generate Controller
            AddFileToArchive(archive, $"{workflowInfo.ServiceName}/Controllers/{workflowInfo.ServiceName}Controller.cs", 
                GenerateController(workflowInfo));

            // Generate Dockerfile
            AddFileToArchive(archive, $"{workflowInfo.ServiceName}/Dockerfile", 
                GenerateDockerfile(workflowInfo));

            // Include the original Arazzo JSON
            AddFileToArchive(archive, $"{workflowInfo.ServiceName}/arazzo-workflow.json", 
                workflow.ArazzoJson);

            // Generate README
            AddFileToArchive(archive, $"{workflowInfo.ServiceName}/README.md", 
                GenerateReadme(workflowInfo));
        }

        memoryStream.Position = 0;
        var zipBytes = memoryStream.ToArray();

        _logger.LogInformation("[CODEGEN] Generated {ByteCount} bytes for service: {ServiceName}", zipBytes.Length, workflowInfo.ServiceName);

        return new GenerateServiceResponse
        {
            ZipContent = zipBytes,
            FileName = $"{workflowInfo.ServiceName}-generated-service.zip"
        };
    }

    private void AddFileToArchive(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }

    private WorkflowInfo ExtractWorkflowInfo(JsonDocument arazzoDoc)
    {
        var info = new WorkflowInfo();
        
        if (arazzoDoc.RootElement.TryGetProperty("info", out var infoElement))
        {
            info.Title = infoElement.TryGetProperty("title", out var title) ? title.GetString() ?? "GeneratedService" : "GeneratedService";
            info.Version = infoElement.TryGetProperty("version", out var version) ? version.GetString() ?? "1.0.0" : "1.0.0";
            info.Description = infoElement.TryGetProperty("description", out var desc) ? desc.GetString() : null;
        }

        info.ServiceName = SanitizeClassName(info.Title);

        if (arazzoDoc.RootElement.TryGetProperty("workflows", out var workflows) && workflows.GetArrayLength() > 0)
        {
            var firstWorkflow = workflows[0];
            if (firstWorkflow.TryGetProperty("steps", out var steps))
            {
                foreach (var step in steps.EnumerateArray())
                {
                    var stepInfo = new StepInfo
                    {
                        StepId = step.TryGetProperty("stepId", out var stepId) ? stepId.GetString() ?? "step" : "step",
                        OperationId = step.TryGetProperty("operationId", out var opId) ? opId.GetString() ?? "operation" : "operation",
                        Description = step.TryGetProperty("description", out var stepDesc) ? stepDesc.GetString() : null
                    };
                    info.Steps.Add(stepInfo);
                }
            }
        }

        return info;
    }

    private string SanitizeClassName(string input)
    {
        var sanitized = new StringBuilder();
        bool capitalizeNext = true;
        
        foreach (char c in input)
        {
            if (char.IsLetterOrDigit(c))
            {
                sanitized.Append(capitalizeNext ? char.ToUpper(c) : c);
                capitalizeNext = false;
            }
            else
            {
                capitalizeNext = true;
            }
        }
        
        var result = sanitized.ToString();
        if (result.Length == 0 || char.IsDigit(result[0]))
            result = "Generated" + result;
            
        return result;
    }

    private string GenerateSolutionFile(WorkflowInfo info) => $@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{info.ServiceName}"", ""{info.ServiceName}.csproj"", ""{{A1234567-1234-1234-1234-123456789ABC}}""
EndProject
Global
    GlobalSection(SolutionConfigurationPlatforms) = preSolution
        Debug|Any CPU = Debug|Any CPU
        Release|Any CPU = Release|Any CPU
    EndGlobalSection
    GlobalSection(ProjectConfigurationPlatforms) = postSolution
        {{A1234567-1234-1234-1234-123456789ABC}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {{A1234567-1234-1234-1234-123456789ABC}}.Debug|Any CPU.Build.0 = Debug|Any CPU
        {{A1234567-1234-1234-1234-123456789ABC}}.Release|Any CPU.ActiveCfg = Release|Any CPU
        {{A1234567-1234-1234-1234-123456789ABC}}.Release|Any CPU.Build.0 = Release|Any CPU
    EndGlobalSection
EndGlobal
".Trim();

    private string GenerateProjectFile(WorkflowInfo info) => $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.OpenApi"" Version=""8.0.0"" />
    <PackageReference Include=""Swashbuckle.AspNetCore"" Version=""6.5.0"" />
    <PackageReference Include=""Polly"" Version=""8.2.0"" />
    <PackageReference Include=""Serilog.AspNetCore"" Version=""8.0.0"" />
  </ItemGroup>

</Project>
";

    private string GenerateProgramCs(WorkflowInfo info) => $@"using Serilog;
using {info.ServiceName}.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{{
    c.SwaggerDoc(""v1"", new() {{ Title = ""{info.Title} API"", Version = ""{info.Version}"" }});
}});

// Register workflow service
builder.Services.AddScoped<{info.ServiceName}Service>();
builder.Services.AddHttpClient();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseSerilogRequestLogging();
app.UseAuthorization();
app.MapControllers();

// Health check
app.MapGet(""/health"", () => Results.Ok(new {{ status = ""healthy"", timestamp = DateTime.UtcNow }}));

Log.Information(""Starting {info.ServiceName} on http://localhost:5000..."");
app.Run();
";

    private string GenerateAppSettings(WorkflowInfo info) => $@"{{
  ""Logging"": {{
    ""LogLevel"": {{
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }}
  }},
  ""AllowedHosts"": ""*"",
  ""ServiceSettings"": {{
    ""BaseUrl"": ""https://api.example.com"",
    ""Timeout"": 30
  }}
}}
";

    private string GenerateWorkflowService(WorkflowInfo info)
    {
        var stepMethods = new StringBuilder();
        foreach (var step in info.Steps)
        {
            stepMethods.AppendLine($@"
    /// <summary>
    /// {step.Description ?? $"Execute {step.OperationId}"}
    /// </summary>
    public async Task<WorkflowStepResult> Execute{SanitizeClassName(step.OperationId)}Async(WorkflowContext context)
    {{
        _logger.LogInformation(""Executing step: {step.StepId}"");
        
        try
        {{
            // TODO: Implement actual API call for {step.OperationId}
            await Task.Delay(100); // Simulated work
            
            return new WorkflowStepResult
            {{
                StepId = ""{step.StepId}"",
                Success = true,
                Message = ""Step completed successfully""
            }};
        }}
        catch (Exception ex)
        {{
            _logger.LogError(ex, ""Error executing step: {step.StepId}"");
            return new WorkflowStepResult
            {{
                StepId = ""{step.StepId}"",
                Success = false,
                Message = ex.Message
            }};
        }}
    }}
");
        }

        return $@"using {info.ServiceName}.Models;

namespace {info.ServiceName}.Services;

/// <summary>
/// Service implementing the {info.Title} workflow
/// Generated from Arazzo specification
/// </summary>
public class {info.ServiceName}Service
{{
    private readonly ILogger<{info.ServiceName}Service> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public {info.ServiceName}Service(
        ILogger<{info.ServiceName}Service> logger,
        IHttpClientFactory httpClientFactory)
    {{
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }}

    /// <summary>
    /// Execute the complete workflow
    /// </summary>
    public async Task<WorkflowExecutionResult> ExecuteWorkflowAsync(WorkflowContext context)
    {{
        _logger.LogInformation(""Starting workflow execution: {info.Title}"");
        
        var result = new WorkflowExecutionResult
        {{
            WorkflowId = Guid.NewGuid().ToString(),
            StartedAt = DateTime.UtcNow
        }};

        try
        {{
            // Execute workflow steps in sequence
{string.Join("\n", info.Steps.Select((s, i) => $"            result.StepResults.Add(await Execute{SanitizeClassName(s.OperationId)}Async(context));"))}

            result.Success = result.StepResults.All(s => s.Success);
            result.CompletedAt = DateTime.UtcNow;
            
            _logger.LogInformation(""Workflow completed. Success: {{Success}}"", result.Success);
        }}
        catch (Exception ex)
        {{
            _logger.LogError(ex, ""Workflow execution failed"");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.CompletedAt = DateTime.UtcNow;
        }}

        return result;
    }}
{stepMethods}
}}
";
    }

    private string GenerateModels(WorkflowInfo info) => $@"namespace {info.ServiceName}.Models;

/// <summary>
/// Context for workflow execution
/// </summary>
public class WorkflowContext
{{
    public string? CorrelationId {{ get; set; }}
    public Dictionary<string, object> Parameters {{ get; set; }} = new();
    public Dictionary<string, object> State {{ get; set; }} = new();
}}

/// <summary>
/// Result of a single workflow step
/// </summary>
public class WorkflowStepResult
{{
    public required string StepId {{ get; set; }}
    public bool Success {{ get; set; }}
    public string? Message {{ get; set; }}
    public object? Data {{ get; set; }}
}}

/// <summary>
/// Result of complete workflow execution
/// </summary>
public class WorkflowExecutionResult
{{
    public required string WorkflowId {{ get; set; }}
    public bool Success {{ get; set; }}
    public string? ErrorMessage {{ get; set; }}
    public DateTime StartedAt {{ get; set; }}
    public DateTime? CompletedAt {{ get; set; }}
    public List<WorkflowStepResult> StepResults {{ get; set; }} = new();
}}

/// <summary>
/// Request to execute the workflow
/// </summary>
public class ExecuteWorkflowRequest
{{
    public Dictionary<string, object>? Parameters {{ get; set; }}
}}
";

    private string GenerateController(WorkflowInfo info) => $@"using Microsoft.AspNetCore.Mvc;
using {info.ServiceName}.Models;
using {info.ServiceName}.Services;

namespace {info.ServiceName}.Controllers;

/// <summary>
/// Controller for {info.Title} workflow
/// </summary>
[ApiController]
[Route(""api/[controller]"")]
public class {info.ServiceName}Controller : ControllerBase
{{
    private readonly {info.ServiceName}Service _workflowService;
    private readonly ILogger<{info.ServiceName}Controller> _logger;

    public {info.ServiceName}Controller(
        {info.ServiceName}Service workflowService,
        ILogger<{info.ServiceName}Controller> logger)
    {{
        _workflowService = workflowService;
        _logger = logger;
    }}

    /// <summary>
    /// Execute the {info.Title} workflow
    /// </summary>
    [HttpPost(""execute"")]
    [ProducesResponseType(typeof(WorkflowExecutionResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<WorkflowExecutionResult>> ExecuteWorkflow(
        [FromBody] ExecuteWorkflowRequest? request,
        CancellationToken cancellationToken)
    {{
        try
        {{
            _logger.LogInformation(""Received workflow execution request"");

            var context = new WorkflowContext
            {{
                CorrelationId = Guid.NewGuid().ToString(),
                Parameters = request?.Parameters ?? new Dictionary<string, object>()
            }};

            var result = await _workflowService.ExecuteWorkflowAsync(context);

            if (result.Success)
                return Ok(result);
            else
                return BadRequest(result);
        }}
        catch (Exception ex)
        {{
            _logger.LogError(ex, ""Error executing workflow"");
            return StatusCode(500, new {{ error = ""Internal server error"", message = ex.Message }});
        }}
    }}

    /// <summary>
    /// Get workflow information
    /// </summary>
    [HttpGet(""info"")]
    public ActionResult<object> GetWorkflowInfo()
    {{
        return Ok(new
        {{
            name = ""{info.Title}"",
            version = ""{info.Version}"",
            description = ""{info.Description ?? "Generated workflow service"}"",
            steps = new[] {{ {string.Join(", ", info.Steps.Select(s => $@"new {{ id = ""{s.StepId}"", operation = ""{s.OperationId}"" }}"))} }}
        }});
    }}
}}
";

    private string GenerateDockerfile(WorkflowInfo info) => $@"# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000

EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT [""dotnet"", ""{info.ServiceName}.dll""]
";

    private string GenerateReadme(WorkflowInfo info) => $@"# {info.Title}

{info.Description ?? "Auto-generated service from Arazzo workflow specification."}

## Version
{info.Version}

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Docker (optional)

### Running Locally

```bash
dotnet restore
dotnet run
```

The API will be available at `http://localhost:5000`

### Running with Docker

```bash
docker build -t {info.ServiceName.ToLower()} .
docker run -p 5000:5000 {info.ServiceName.ToLower()}
```

## API Endpoints

### Execute Workflow
```
POST /api/{info.ServiceName}/execute
Content-Type: application/json

{{
  ""parameters"": {{}}
}}
```

### Get Workflow Info
```
GET /api/{info.ServiceName}/info
```

### Health Check
```
GET /health
```

## Workflow Steps

{string.Join("\n", info.Steps.Select((s, i) => $"{i + 1}. **{s.StepId}** - {s.Description ?? s.OperationId}"))}

## Generated from Arazzo Specification

This service was automatically generated from an Arazzo workflow specification.
See `arazzo-workflow.json` for the original specification.
";

    private class WorkflowInfo
    {
        public string Title { get; set; } = "GeneratedService";
        public string Version { get; set; } = "1.0.0";
        public string? Description { get; set; }
        public string ServiceName { get; set; } = "GeneratedService";
        public List<StepInfo> Steps { get; set; } = new();
    }

    private class StepInfo
    {
        public string StepId { get; set; } = "";
        public string OperationId { get; set; } = "";
        public string? Description { get; set; }
    }
}
