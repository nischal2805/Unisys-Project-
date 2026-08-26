using ArazzoWorkflowPlatform.Domain.Entities;
using ArazzoWorkflowPlatform.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Text.Json;
using System.Text;

namespace Application.Workflows.Commands;

public class CreateWorkflowCommandHandler : IRequestHandler<CreateWorkflowCommand, CreateWorkflowResponse>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IFileRepository _fileRepository;
    private readonly Kernel? _kernel;
    private readonly ILogger<CreateWorkflowCommandHandler> _logger;
    private readonly IVectorStore? _vectorStore;
    private readonly IEmbeddingService? _embeddingService;
    private readonly IChunkingService? _chunkingService;

    // Approximate tokens threshold for using RAG
    private const int LargeFileTokenThreshold = 2000;
    private const double CharsPerToken = 4.0;

    public CreateWorkflowCommandHandler(
        IWorkflowRepository workflowRepository, 
        IFileRepository fileRepository,
        ILogger<CreateWorkflowCommandHandler> logger,
        Kernel? kernel = null,
        IVectorStore? vectorStore = null,
        IEmbeddingService? embeddingService = null,
        IChunkingService? chunkingService = null)
    {
        _workflowRepository = workflowRepository;
        _fileRepository = fileRepository;
        _logger = logger;
        _kernel = kernel;
        _vectorStore = vectorStore;
        _embeddingService = embeddingService;
        _chunkingService = chunkingService;
    }

    public async Task<CreateWorkflowResponse> Handle(CreateWorkflowCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate file exists
            var file = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken);
            if (file == null) 
                throw new ArgumentException($"File with ID {request.FileId} not found");
            
            // Generate Arazzo workflow
            var arazzoJson = await GenerateArazzoWorkflowAsync(file.Metadata, request.WorkflowName, request.Description, cancellationToken);
            
            // Create workflow entity
            var workflow = new WorkflowEntity
            {
                Id = Guid.NewGuid(),
                FileId = request.FileId,
                Name = request.WorkflowName,
                Description = request.Description,
                ArazzoJson = arazzoJson,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = WorkflowStatus.Draft
            };
            
            // Save workflow
            await _workflowRepository.CreateAsync(workflow, cancellationToken);
            
            return new CreateWorkflowResponse 
            { 
                Id = workflow.Id.ToString(), 
                Name = workflow.Name,
                Description = workflow.Description,
                FileId = workflow.FileId.ToString(),
                ArazzoJson = arazzoJson, // Keep as string for frontend to parse
                Status = workflow.Status.ToString(),
                CreatedAt = workflow.CreatedAt,
                UpdatedAt = workflow.UpdatedAt
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create workflow: {ex.Message}", ex);
        }
    }

    private async Task<string> GenerateArazzoWorkflowAsync(string? fileContent, string workflowName, string? description, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ARAZZO] Starting workflow generation for: {WorkflowName}", workflowName);
        _logger.LogDebug("[ARAZZO] File content length: {ContentLength} chars", fileContent?.Length ?? 0);
        _logger.LogDebug("[ARAZZO] AI Kernel available: {KernelAvailable}", _kernel != null);
        _logger.LogDebug("[ARAZZO] RAG services available: VectorStore={VectorStore}, Embedding={Embedding}", 
            _vectorStore != null, _embeddingService != null);
        
        // Test Ollama connectivity before attempting AI generation
        if (_kernel != null)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(8);
                var response = await httpClient.GetAsync("http://localhost:11434");
                _logger.LogDebug("[ARAZZO] Ollama connectivity test: {StatusCode}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ARAZZO] Ollama connectivity test failed");
            }
        }
        
        // Always try to generate an intelligent workflow based on OpenAPI content
        var intelligentWorkflow = GenerateIntelligentArazzoWorkflow(fileContent, workflowName, description);
        
        // Try AI generation first, fallback to intelligent workflow if needed
        if (_kernel == null || string.IsNullOrEmpty(fileContent))
        {
            _logger.LogWarning("[ARAZZO] Using intelligent fallback workflow (no AI kernel or content)");
            return intelligentWorkflow;
        }

        // Determine if we should use RAG for large files
        string contextForPrompt;
        var estimatedTokens = (int)Math.Ceiling((fileContent?.Length ?? 0) / CharsPerToken);
        
        if (estimatedTokens > LargeFileTokenThreshold && _vectorStore != null && _embeddingService != null)
        {
            _logger.LogInformation("[ARAZZO] Large file detected ({Tokens} tokens), using RAG for context retrieval", estimatedTokens);
            contextForPrompt = await GetRagContextAsync(workflowName, description, fileContent, cancellationToken);
        }
        else
        {
            // For smaller files, use direct content (truncated if needed)
            contextForPrompt = fileContent?.Substring(0, Math.Min(6000, fileContent?.Length ?? 0)) ?? "";
            _logger.LogDebug("[ARAZZO] Using direct content ({Length} chars)", contextForPrompt.Length);
        }

        // Enhanced prompt using RAG context
        var prompt = $@"Create a valid Arazzo 1.0.0 workflow JSON based on this OpenAPI specification context:

{contextForPrompt}

Requirements:
- Valid Arazzo 1.0.0 format
- Workflow name: {workflowName}
- Description: {description ?? "Automated API workflow"}
- Include 2-4 workflow steps that demonstrate a logical sequence
- Use operationIds from the spec when available
- Include proper inputs, outputs, and success criteria
- Output only valid JSON, no explanations or markdown";

        _logger.LogInformation("[ARAZZO] Sending enhanced prompt to AI (context: {ContextLength} chars)...", contextForPrompt.Length);

        try
        {
            // Use separate timeout for AI generation only - don't link to main cancellation token
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            
            _logger.LogDebug("[ARAZZO] Invoking AI with 10-minute timeout (separate from request timeout)...");
            var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cts.Token);
            var response = result.ToString();
            
            _logger.LogInformation("[ARAZZO] AI response received ({ResponseLength} chars)", response.Length);
            _logger.LogDebug("[ARAZZO] First 200 chars: {Preview}...", response.Substring(0, Math.Min(200, response.Length)));
            
            // Clean and validate response
            response = response.Replace("```json", "").Replace("```", "").Trim();
            
            try
            {
                var doc = JsonDocument.Parse(response);
                // Verify it has the required Arazzo structure
                if (doc.RootElement.TryGetProperty("arazzo", out _) && 
                    doc.RootElement.TryGetProperty("workflows", out var workflows) &&
                    workflows.GetArrayLength() > 0)
                {
                    _logger.LogInformation("[ARAZZO] Valid AI-generated workflow created!");
                    return response;
                }
                else
                {
                    _logger.LogWarning("[ARAZZO] AI response missing required Arazzo structure");
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[ARAZZO] Invalid JSON from AI");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ARAZZO] AI generation failed");
        }
        
        _logger.LogInformation("[ARAZZO] Using intelligent fallback workflow");
        return intelligentWorkflow;
    }

    /// <summary>
    /// Retrieves relevant context from the vector store using RAG for large OpenAPI specs
    /// </summary>
    private async Task<string> GetRagContextAsync(string workflowName, string? description, string? fileContent, CancellationToken cancellationToken)
    {
        var contextBuilder = new StringBuilder();
        
        try
        {
            // Build a search query based on workflow name and description
            var searchQuery = $"API operations for {workflowName}. {description ?? ""}";
            
            // Generate embedding for the search query
            var queryEmbedding = await _embeddingService!.GenerateEmbeddingAsync(searchQuery, cancellationToken);
            
            // Search for relevant chunks
            var searchResults = await _vectorStore!.SearchAsync(
                queryEmbedding,
                limit: 8, // Get top 8 most relevant chunks
                filterByFileId: null, // Search across all files
                cancellationToken: cancellationToken);

            var results = searchResults.ToList();
            
            _logger.LogInformation("[RAG] Retrieved {ChunkCount} relevant chunks for context", results.Count);

            if (results.Count == 0)
            {
                // Fallback to truncated content if no RAG results
                _logger.LogWarning("[RAG] No relevant chunks found, using truncated content");
                return fileContent?.Substring(0, Math.Min(4000, fileContent?.Length ?? 0)) ?? "";
            }

            // Build context from relevant chunks, prioritizing operations
            var operationChunks = results.Where(r => r.Metadata.ChunkType == "operation").ToList();
            var pathChunks = results.Where(r => r.Metadata.ChunkType == "path").ToList();
            var schemaChunks = results.Where(r => r.Metadata.ChunkType == "schema").ToList();

            contextBuilder.AppendLine("=== RELEVANT API OPERATIONS ===");
            foreach (var chunk in operationChunks.Take(4))
            {
                contextBuilder.AppendLine(chunk.Content);
                contextBuilder.AppendLine();
            }

            if (pathChunks.Any())
            {
                contextBuilder.AppendLine("=== RELEVANT API PATHS ===");
                foreach (var chunk in pathChunks.Take(2))
                {
                    contextBuilder.AppendLine(chunk.Content);
                    contextBuilder.AppendLine();
                }
            }

            if (schemaChunks.Any())
            {
                contextBuilder.AppendLine("=== RELEVANT SCHEMAS ===");
                foreach (var chunk in schemaChunks.Take(2))
                {
                    contextBuilder.AppendLine(chunk.Content);
                    contextBuilder.AppendLine();
                }
            }

            var context = contextBuilder.ToString();
            _logger.LogDebug("[RAG] Built context of {Length} chars from {ChunkCount} chunks", context.Length, results.Count);
            
            return context;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[RAG] Failed to retrieve RAG context, falling back to truncated content");
            return fileContent?.Substring(0, Math.Min(4000, fileContent?.Length ?? 0)) ?? "";
        }
    }

    private string GenerateIntelligentArazzoWorkflow(string? fileContent, string workflowName, string? description)
    {
        _logger.LogDebug("[ARAZZO] Generating intelligent workflow from OpenAPI content");
        
        var operations = ExtractOperationsFromOpenApi(fileContent);
        var steps = CreateStepsFromOperations(operations);
        
        _logger.LogInformation("[ARAZZO] Found {OperationCount} operations, created {StepCount} workflow steps", operations.Count, steps.Length);
        
        var workflow = new
        {
            arazzo = "1.0.0",
            info = new 
            { 
                title = workflowName, 
                version = "1.0.0", 
                description = description ?? $"Generated workflow from {operations.Count} API operations",
                summary = $"Automated workflow with {steps.Length} steps"
            },
            sourceDescriptions = new[] 
            { 
                new { name = "main-api", type = "openapi", url = "/api/openapi.json" } 
            },
            workflows = new[] 
            { 
                new 
                { 
                    workflowId = SanitizeId(workflowName),
                    summary = $"Main workflow for {workflowName}",
                    description = description ?? "Automated API operations workflow", 
                    steps = steps
                } 
            }
        };
        
        var json = JsonSerializer.Serialize(workflow, new JsonSerializerOptions { WriteIndented = true });
        _logger.LogInformation("[ARAZZO] Intelligent workflow generated ({JsonLength} chars)", json.Length);
        return json;
    }
    
    private List<(string operationId, string method, string path, string summary)> ExtractOperationsFromOpenApi(string? content)
    {
        var operations = new List<(string operationId, string method, string path, string summary)>();
        
        if (string.IsNullOrEmpty(content)) 
        {
            _logger.LogWarning("[ARAZZO] No content to parse, using default operations");
            return new List<(string, string, string, string)>
            {
                ("getItems", "GET", "/items", "Retrieve all items"),
                ("createItem", "POST", "/items", "Create a new item"),
                ("getItemById", "GET", "/items/{id}", "Get item by ID")
            };
        }
        
        try
        {
            JsonDocument doc;
            // Handle both JSON and YAML content
            if (content.TrimStart().StartsWith("{"))
            {
                // JSON format
                doc = JsonDocument.Parse(content);
            }
            else
            {
                // Likely YAML - convert to JSON first (simple conversion)
                _logger.LogDebug("[ARAZZO] Detecting YAML content, attempting to parse...");
                var jsonContent = ConvertYamlToJsonSimple(content);
                if (jsonContent != null)
                {
                    doc = JsonDocument.Parse(jsonContent);
                }
                else
                {
                    // Fall back to text parsing
                    _logger.LogDebug("[ARAZZO] YAML conversion failed, using text extraction");
                    return ExtractOperationsFromText(content);
                }
            }
            
            if (doc.RootElement.TryGetProperty("paths", out var paths))
            {
                foreach (var pathProperty in paths.EnumerateObject())
                {
                    var path = pathProperty.Name;
                    foreach (var methodProperty in pathProperty.Value.EnumerateObject())
                    {
                        var method = methodProperty.Name.ToUpper();
                        var operation = methodProperty.Value;
                        
                        var operationId = operation.TryGetProperty("operationId", out var opId) 
                            ? opId.GetString() ?? $"{method.ToLower()}{SanitizeId(path)}"
                            : $"{method.ToLower()}{SanitizeId(path)}";
                            
                        var summary = operation.TryGetProperty("summary", out var sum)
                            ? sum.GetString() ?? $"{method} {path}"
                            : $"{method} operation on {path}";
                        
                        operations.Add((operationId, method, path, summary));
                        
                        if (operations.Count >= 6) break; // Limit to avoid too many steps
                    }
                    if (operations.Count >= 6) break;
                }
            }
        }
        catch (JsonException)
        {
            // If JSON parsing fails, try basic YAML/text parsing
            _logger.LogDebug("[ARAZZO] JSON parsing failed, trying text extraction");
            operations = ExtractOperationsFromText(content);
        }
        
        // If no operations found, provide some defaults
        if (operations.Count == 0)
        {
            _logger.LogWarning("[ARAZZO] No operations extracted, using generic defaults");
            operations.Add(("listResources", "GET", "/api/resources", "List all resources"));
            operations.Add(("createResource", "POST", "/api/resources", "Create new resource"));
            operations.Add(("getResource", "GET", "/api/resources/{id}", "Get resource by ID"));
        }
        
        return operations;
    }
    
    private List<(string, string, string, string)> ExtractOperationsFromText(string content)
    {
        var operations = new List<(string, string, string, string)>();
        var lines = content.Split('\n');
        
        // Look for common OpenAPI patterns in YAML or text
        foreach (var line in lines.Take(100)) // Check first 100 lines
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("/") && trimmed.Contains(":"))
            {
                var path = trimmed.Split(':')[0].Trim();
                // Simple heuristic to generate operations
                operations.Add(($"get{SanitizeId(path)}", "GET", path, $"Get {path}"));
                if (operations.Count >= 4) break;
            }
        }
        
        return operations;
    }
    
    private object[] CreateStepsFromOperations(List<(string operationId, string method, string path, string summary)> operations)
    {
        var steps = new List<object>();
        
        for (int i = 0; i < Math.Min(operations.Count, 4); i++)
        {
            var op = operations[i];
            var stepId = $"step{i + 1}_{op.operationId}";
            
            object step;
            
            if (i == 0)
            {
                // First step has no dependencies
                step = new
                {
                    stepId = stepId,
                    description = op.summary,
                    operationId = op.operationId,
                    parameters = new object[] { },
                    successCriteria = new[]
                    {
                        new { condition = "$statusCode == 200" }
                    },
                    outputs = new
                    {
                        result = "$response.body"
                    }
                };
            }
            else
            {
                // Subsequent steps depend on previous step
                step = new
                {
                    stepId = stepId,
                    description = op.summary,
                    operationId = op.operationId,
                    dependsOn = $"step{i}_{operations[i-1].operationId}",
                    parameters = new object[] { },
                    successCriteria = new[]
                    {
                        new { condition = "$statusCode == 200" }
                    },
                    outputs = new
                    {
                        result = "$response.body"
                    }
                };
            }
            
            steps.Add(step);
        }
        
        return steps.ToArray();
    }
    
    private string SanitizeId(string input)
    {
        return string.Join("", input.Where(c => char.IsLetterOrDigit(c) || c == '_'))
               .Trim('_')
               .Replace("__", "_");
    }
    
    private string? ConvertYamlToJsonSimple(string yamlContent)
    {
        try
        {
            // Simple YAML to JSON conversion for basic OpenAPI structure
            var lines = yamlContent.Split('\n');
            var json = new StringBuilder();
            json.AppendLine("{");
            
            bool inPaths = false;
            var pathsJson = new List<string>();
            
            foreach (var line in lines.Take(200)) // Process first 200 lines
            {
                var trimmed = line.Trim();
                
                // Look for paths section
                if (trimmed.StartsWith("paths:"))
                {
                    inPaths = true;
                    continue;
                }
                
                if (inPaths && trimmed.StartsWith("/") && trimmed.Contains(":"))
                {
                    // Extract path
                    var path = trimmed.Split(':')[0].Trim();
                    var pathJson = $"\"{path}\": {{";
                    
                    // Look for HTTP methods in following lines
                    var methods = new List<string>();
                    for (int i = Array.IndexOf(lines, line) + 1; i < Math.Min(lines.Length, Array.IndexOf(lines, line) + 10); i++)
                    {
                        var methodLine = lines[i].Trim();
                        if (methodLine.StartsWith("get:") || methodLine.StartsWith("post:") || 
                            methodLine.StartsWith("put:") || methodLine.StartsWith("delete:"))
                        {
                            var method = methodLine.Split(':')[0].Trim();
                            methods.Add($"\"{method}\": {{\"operationId\": \"{method}{SanitizeId(path)}\"}}");
                        }
                    }
                    
                    if (methods.Count > 0)
                    {
                        pathJson += string.Join(",", methods) + "}";
                        pathsJson.Add(pathJson);
                    }
                }
                
                if (inPaths && (trimmed.StartsWith("components:") || trimmed.StartsWith("info:")))
                {
                    break; // End of paths section
                }
            }
            
            json.AppendLine("\"paths\": {");
            json.AppendLine(string.Join(",\n", pathsJson));
            json.AppendLine("}");
            json.AppendLine("}");
            
            var result = json.ToString();
            _logger.LogDebug("[ARAZZO] Converted YAML to JSON ({ResultLength} chars)", result.Length);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ARAZZO] YAML conversion error");
            return null;
        }
    }
}
