using System.Text.Json.Serialization;

namespace ArazzoWorkflowPlatform.Domain.ValueObjects;

/// <summary>
/// Represents a complete Arazzo workflow document (Arazzo 1.0.0 specification)
/// </summary>
public record ArazzoWorkflow
{
    [JsonPropertyName("arazzo")]
    public string Arazzo { get; init; } = "1.0.0";

    [JsonPropertyName("info")]
    public required WorkflowInfo Info { get; init; }

    [JsonPropertyName("sourceDescriptions")]
    public required List<SourceDescription> SourceDescriptions { get; init; }

    [JsonPropertyName("workflows")]
    public required List<Workflow> Workflows { get; init; }
}

/// <summary>
/// Metadata about the workflow document
/// </summary>
public record WorkflowInfo
{
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }
}

/// <summary>
/// Reference to an OpenAPI or Arazzo document
/// </summary>
public record SourceDescription
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; } // "openapi" or "arazzo"
}

/// <summary>
/// A workflow definition containing multiple steps
/// </summary>
public record Workflow
{
    [JsonPropertyName("workflowId")]
    public required string WorkflowId { get; init; }

    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("parameters")]
    public List<WorkflowParameter>? Parameters { get; init; }

    [JsonPropertyName("steps")]
    public required List<WorkflowStep> Steps { get; init; }

    [JsonPropertyName("successActions")]
    public List<WorkflowAction>? SuccessActions { get; init; }

    [JsonPropertyName("failureActions")]
    public List<WorkflowAction>? FailureActions { get; init; }

    [JsonPropertyName("outputs")]
    public Dictionary<string, string>? Outputs { get; init; }
}

/// <summary>
/// A workflow input parameter
/// </summary>
public record WorkflowParameter
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("in")]
    public required string In { get; init; } // query, header, path, cookie, body

    [JsonPropertyName("value")]
    public object? Value { get; init; }

    [JsonPropertyName("required")]
    public bool? Required { get; init; }
}

/// <summary>
/// A single step in a workflow
/// </summary>
public record WorkflowStep
{
    [JsonPropertyName("stepId")]
    public required string StepId { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("operationId")]
    public required string OperationId { get; init; }

    [JsonPropertyName("operationPath")]
    public string? OperationPath { get; init; }

    [JsonPropertyName("parameters")]
    public List<StepParameter>? Parameters { get; init; }

    [JsonPropertyName("requestBody")]
    public object? RequestBody { get; init; }

    [JsonPropertyName("successCriteria")]
    public List<SuccessCriterion>? SuccessCriteria { get; init; }

    [JsonPropertyName("onSuccess")]
    public List<WorkflowAction>? OnSuccess { get; init; }

    [JsonPropertyName("onFailure")]
    public List<WorkflowAction>? OnFailure { get; init; }

    [JsonPropertyName("outputs")]
    public Dictionary<string, string>? Outputs { get; init; }
}

/// <summary>
/// A parameter for a workflow step
/// </summary>
public record StepParameter
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("in")]
    public required string In { get; init; }

    [JsonPropertyName("value")]
    public required object Value { get; init; } // Can be literal or runtime expression
}

/// <summary>
/// Defines when a step is considered successful
/// </summary>
public record SuccessCriterion
{
    [JsonPropertyName("condition")]
    public required string Condition { get; init; }

    [JsonPropertyName("context")]
    public string? Context { get; init; }
}

/// <summary>
/// Action to take after step execution
/// </summary>
public record WorkflowAction
{
    [JsonPropertyName("type")]
    public required string Type { get; init; } // goto, end, retry

    [JsonPropertyName("stepId")]
    public string? StepId { get; init; }

    [JsonPropertyName("retryAfter")]
    public int? RetryAfter { get; init; }

    [JsonPropertyName("retryLimit")]
    public int? RetryLimit { get; init; }
}
