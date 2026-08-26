# Arazzo Workflow Schema

## Overview

The Arazzo Workflow is the central data artifact of the platform. It represents a business process as a series of executable steps that reference OpenAPI operations.

## JSON Schema Definition

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["arazzo", "info", "sourceDescriptions", "workflows"],
  "properties": {
    "arazzo": {
      "type": "string",
      "const": "1.0.0",
      "description": "Arazzo specification version"
    },
    "info": {
      "type": "object",
      "required": ["title", "version"],
      "properties": {
        "title": {
          "type": "string",
          "description": "The title of the workflow"
        },
        "summary": {
          "type": "string",
          "description": "A short summary of the workflow"
        },
        "description": {
          "type": "string",
          "description": "A detailed description of the workflow purpose"
        },
        "version": {
          "type": "string",
          "description": "The version of the workflow"
        }
      }
    },
    "sourceDescriptions": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["name", "url", "type"],
        "properties": {
          "name": {
            "type": "string",
            "description": "Reference name for the API description"
          },
          "url": {
            "type": "string",
            "format": "uri",
            "description": "URL or file path to the OpenAPI document"
          },
          "type": {
            "type": "string",
            "enum": ["openapi", "arazzo"],
            "description": "The type of source description"
          }
        }
      },
      "description": "List of API descriptions used by the workflow"
    },
    "workflows": {
      "type": "array",
      "items": {
        "$ref": "#/definitions/Workflow"
      },
      "description": "The workflow definitions"
    }
  },
  "definitions": {
    "Workflow": {
      "type": "object",
      "required": ["workflowId", "steps"],
      "properties": {
        "workflowId": {
          "type": "string",
          "description": "Unique identifier for the workflow"
        },
        "summary": {
          "type": "string",
          "description": "Brief description of what the workflow does"
        },
        "description": {
          "type": "string",
          "description": "Detailed explanation of the workflow"
        },
        "parameters": {
          "type": "array",
          "items": {
            "$ref": "#/definitions/Parameter"
          },
          "description": "Input parameters for the workflow"
        },
        "steps": {
          "type": "array",
          "items": {
            "$ref": "#/definitions/Step"
          },
          "description": "Ordered list of workflow steps"
        },
        "successActions": {
          "type": "array",
          "items": {
            "$ref": "#/definitions/Action"
          },
          "description": "Actions to perform on successful completion"
        },
        "failureActions": {
          "type": "array",
          "items": {
            "$ref": "#/definitions/Action"
          },
          "description": "Actions to perform on failure"
        },
        "outputs": {
          "type": "object",
          "additionalProperties": {
            "type": "string"
          },
          "description": "Output values from the workflow"
        }
      }
    },
    "Step": {
      "type": "object",
      "required": ["stepId", "operationId"],
      "properties": {
        "stepId": {
          "type": "string",
          "description": "Unique identifier for the step"
        },
        "description": {
          "type": "string",
          "description": "Description of what the step does"
        },
        "operationId": {
          "type": "string",
          "description": "Reference to the OpenAPI operation"
        },
        "operationPath": {
          "type": "string",
          "description": "The API path for the operation"
        },
        "parameters": {
          "type": "array",
          "items": {
            "$ref": "#/definitions/StepParameter"
          },
          "description": "Parameters to pass to the operation"
        },
        "requestBody": {
          "type": "object",
          "description": "Request body for the operation"
        },
        "successCriteria": {
          "type": "array",
          "items": {
            "$ref": "#/definitions/Criterion"
          },
          "description": "Conditions that define success"
        },
        "onSuccess": {
          "type": "array",
          "items": {
            "$ref": "#/definitions/Action"
          },
          "description": "Actions to take on success"
        },
        "onFailure": {
          "type": "array",
          "items": {
            "$ref": "#/definitions/Action"
          },
          "description": "Actions to take on failure"
        },
        "outputs": {
          "type": "object",
          "additionalProperties": {
            "type": "string"
          },
          "description": "Named outputs from this step"
        }
      }
    },
    "Parameter": {
      "type": "object",
      "required": ["name", "in"],
      "properties": {
        "name": {
          "type": "string"
        },
        "in": {
          "type": "string",
          "enum": ["query", "header", "path", "cookie", "body"]
        },
        "value": {
          "description": "The value or expression for the parameter"
        }
      }
    },
    "StepParameter": {
      "type": "object",
      "required": ["name", "value"],
      "properties": {
        "name": {
          "type": "string"
        },
        "in": {
          "type": "string",
          "enum": ["query", "header", "path", "cookie", "body"]
        },
        "value": {
          "description": "Value, reference, or runtime expression"
        }
      }
    },
    "Criterion": {
      "type": "object",
      "required": ["condition"],
      "properties": {
        "condition": {
          "type": "string",
          "description": "Boolean expression to evaluate"
        },
        "context": {
          "type": "string",
          "description": "Context for the condition (e.g., statusCode, response)"
        }
      }
    },
    "Action": {
      "type": "object",
      "properties": {
        "type": {
          "type": "string",
          "enum": ["goto", "end", "retry"],
          "description": "Type of action to perform"
        },
        "stepId": {
          "type": "string",
          "description": "Target step ID for goto actions"
        },
        "retryAfter": {
          "type": "number",
          "description": "Seconds to wait before retry"
        },
        "retryLimit": {
          "type": "integer",
          "description": "Maximum number of retries"
        }
      }
    }
  }
}
```

## C# Domain Model

```csharp
namespace Domain.ValueObjects;

public record ArazzoWorkflow
{
    public string Arazzo { get; init; } = "1.0.0";
    public required WorkflowInfo Info { get; init; }
    public required List<SourceDescription> SourceDescriptions { get; init; }
    public required List<Workflow> Workflows { get; init; }
}

public record WorkflowInfo
{
    public required string Title { get; init; }
    public string? Summary { get; init; }
    public string? Description { get; init; }
    public required string Version { get; init; }
}

public record SourceDescription
{
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string Type { get; init; } // "openapi" or "arazzo"
}

public record Workflow
{
    public required string WorkflowId { get; init; }
    public string? Summary { get; init; }
    public string? Description { get; init; }
    public List<WorkflowParameter>? Parameters { get; init; }
    public required List<WorkflowStep> Steps { get; init; }
    public List<WorkflowAction>? SuccessActions { get; init; }
    public List<WorkflowAction>? FailureActions { get; init; }
    public Dictionary<string, string>? Outputs { get; init; }
}

public record WorkflowStep
{
    public required string StepId { get; init; }
    public string? Description { get; init; }
    public required string OperationId { get; init; }
    public string? OperationPath { get; init; }
    public List<StepParameter>? Parameters { get; init; }
    public object? RequestBody { get; init; }
    public List<SuccessCriterion>? SuccessCriteria { get; init; }
    public List<WorkflowAction>? OnSuccess { get; init; }
    public List<WorkflowAction>? OnFailure { get; init; }
    public Dictionary<string, string>? Outputs { get; init; }
}

public record WorkflowParameter
{
    public required string Name { get; init; }
    public required string In { get; init; } // query, header, path, cookie, body
    public object? Value { get; init; }
}

public record StepParameter
{
    public required string Name { get; init; }
    public required string In { get; init; }
    public required object Value { get; init; } // Can be literal or runtime expression
}

public record SuccessCriterion
{
    public required string Condition { get; init; }
    public string? Context { get; init; }
}

public record WorkflowAction
{
    public required string Type { get; init; } // goto, end, retry
    public string? StepId { get; init; }
    public int? RetryAfter { get; init; }
    public int? RetryLimit { get; init; }
}
```

## Sample Arazzo Workflow

```json
{
  "arazzo": "1.0.0",
  "info": {
    "title": "User Registration Workflow",
    "summary": "Complete user registration process with email verification",
    "description": "This workflow handles user registration, sends a verification email, and activates the account upon email confirmation.",
    "version": "1.0.0"
  },
  "sourceDescriptions": [
    {
      "name": "userManagementAPI",
      "url": "https://api.example.com/openapi.json",
      "type": "openapi"
    }
  ],
  "workflows": [
    {
      "workflowId": "user-registration",
      "summary": "Register new user",
      "description": "Complete user registration workflow with email verification",
      "parameters": [
        {
          "name": "email",
          "in": "body",
          "value": null
        },
        {
          "name": "password",
          "in": "body",
          "value": null
        },
        {
          "name": "fullName",
          "in": "body",
          "value": null
        }
      ],
      "steps": [
        {
          "stepId": "create-user",
          "description": "Create user account in the system",
          "operationId": "createUser",
          "operationPath": "/api/users",
          "requestBody": {
            "email": "$inputs.email",
            "password": "$inputs.password",
            "fullName": "$inputs.fullName"
          },
          "successCriteria": [
            {
              "condition": "$statusCode == 201",
              "context": "statusCode"
            }
          ],
          "outputs": {
            "userId": "$response.body.id",
            "verificationToken": "$response.body.verificationToken"
          },
          "onSuccess": [
            {
              "type": "goto",
              "stepId": "send-verification-email"
            }
          ],
          "onFailure": [
            {
              "type": "end"
            }
          ]
        },
        {
          "stepId": "send-verification-email",
          "description": "Send email verification link to user",
          "operationId": "sendVerificationEmail",
          "operationPath": "/api/emails/verification",
          "requestBody": {
            "userId": "$steps.create-user.outputs.userId",
            "email": "$inputs.email",
            "token": "$steps.create-user.outputs.verificationToken"
          },
          "successCriteria": [
            {
              "condition": "$statusCode == 200",
              "context": "statusCode"
            }
          ],
          "outputs": {
            "emailSentAt": "$response.body.sentAt"
          },
          "onSuccess": [
            {
              "type": "goto",
              "stepId": "wait-for-verification"
            }
          ],
          "onFailure": [
            {
              "type": "retry",
              "retryAfter": 60,
              "retryLimit": 3
            }
          ]
        },
        {
          "stepId": "wait-for-verification",
          "description": "Poll for email verification status",
          "operationId": "getUserStatus",
          "operationPath": "/api/users/{userId}/status",
          "parameters": [
            {
              "name": "userId",
              "in": "path",
              "value": "$steps.create-user.outputs.userId"
            }
          ],
          "successCriteria": [
            {
              "condition": "$response.body.verified == true",
              "context": "response"
            }
          ],
          "outputs": {
            "verifiedAt": "$response.body.verifiedAt"
          },
          "onSuccess": [
            {
              "type": "goto",
              "stepId": "activate-account"
            }
          ],
          "onFailure": [
            {
              "type": "retry",
              "retryAfter": 30,
              "retryLimit": 10
            }
          ]
        },
        {
          "stepId": "activate-account",
          "description": "Activate the user account",
          "operationId": "activateUser",
          "operationPath": "/api/users/{userId}/activate",
          "parameters": [
            {
              "name": "userId",
              "in": "path",
              "value": "$steps.create-user.outputs.userId"
            }
          ],
          "successCriteria": [
            {
              "condition": "$statusCode == 200",
              "context": "statusCode"
            }
          ],
          "outputs": {
            "activatedAt": "$response.body.activatedAt",
            "accountStatus": "$response.body.status"
          },
          "onSuccess": [
            {
              "type": "end"
            }
          ]
        }
      ],
      "outputs": {
        "userId": "$steps.create-user.outputs.userId",
        "email": "$inputs.email",
        "registeredAt": "$steps.create-user.outputs.userId",
        "activatedAt": "$steps.activate-account.outputs.activatedAt"
      }
    }
  ]
}
```

## Runtime Expression Language

Arazzo workflows support runtime expressions for dynamic values:

### Expression Syntax

- `$inputs.<parameter_name>` - Access workflow input parameters
- `$steps.<step_id>.outputs.<output_name>` - Access step outputs
- `$response.body.<property>` - Access response body properties
- `$response.headers.<header_name>` - Access response headers
- `$statusCode` - HTTP status code
- `$url` - Request URL

### Example Expressions

```json
{
  "parameters": [
    {
      "name": "userId",
      "in": "path",
      "value": "$steps.create-user.outputs.id"
    },
    {
      "name": "Authorization",
      "in": "header",
      "value": "Bearer $steps.authenticate.outputs.token"
    }
  ],
  "requestBody": {
    "email": "$inputs.email",
    "previousOrderId": "$steps.get-last-order.outputs.orderId"
  }
}
```

## Workflow Validation Rules

### Required Validations

1. **Unique Step IDs**: All `stepId` values must be unique within a workflow
2. **Valid References**: All step references in `goto` actions must exist
3. **Operation Existence**: All `operationId` values must exist in the source OpenAPI spec
4. **Parameter Matching**: Step parameters must match the operation's parameter requirements
5. **No Circular Dependencies**: Steps cannot create circular reference chains
6. **Terminal Steps**: At least one step must have an `end` action

### C# Validation Example

```csharp
public class ArazzoWorkflowValidator : AbstractValidator<ArazzoWorkflow>
{
    public ArazzoWorkflowValidator()
    {
        RuleFor(x => x.Arazzo)
            .Equal("1.0.0")
            .WithMessage("Only Arazzo version 1.0.0 is supported");

        RuleFor(x => x.Info)
            .NotNull()
            .WithMessage("Workflow info is required");

        RuleFor(x => x.SourceDescriptions)
            .NotEmpty()
            .WithMessage("At least one source description is required");

        RuleFor(x => x.Workflows)
            .NotEmpty()
            .WithMessage("At least one workflow is required");

        RuleForEach(x => x.Workflows)
            .SetValidator(new WorkflowValidator());
    }
}

public class WorkflowValidator : AbstractValidator<Workflow>
{
    public WorkflowValidator()
    {
        RuleFor(x => x.WorkflowId)
            .NotEmpty()
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Workflow ID must be lowercase alphanumeric with hyphens");

        RuleFor(x => x.Steps)
            .NotEmpty()
            .WithMessage("Workflow must have at least one step");

        RuleFor(x => x.Steps)
            .Must(HaveUniqueStepIds)
            .WithMessage("All step IDs must be unique");

        RuleFor(x => x.Steps)
            .Must(HaveAtLeastOneTerminalStep)
            .WithMessage("Workflow must have at least one step that ends the workflow");

        RuleForEach(x => x.Steps)
            .SetValidator(new WorkflowStepValidator());
    }

    private bool HaveUniqueStepIds(List<WorkflowStep> steps)
    {
        var stepIds = steps.Select(s => s.StepId).ToList();
        return stepIds.Count == stepIds.Distinct().Count();
    }

    private bool HaveAtLeastOneTerminalStep(List<WorkflowStep> steps)
    {
        return steps.Any(s => 
            s.OnSuccess?.Any(a => a.Type == "end") == true ||
            s.OnFailure?.Any(a => a.Type == "end") == true);
    }
}
```

## Workflow Generation Strategy

### LLM Prompt Template

```text
You are a workflow architect. Generate an Arazzo workflow based on the following requirements:

CONTEXT:
OpenAPI Specification: {openapi_context}
User Request: {user_request}
Previous Steps: {previous_steps}

REQUIREMENTS:
1. Create a workflow with a clear sequence of steps
2. Each step must reference a valid OpenAPI operation
3. Use runtime expressions to pass data between steps
4. Define success criteria for each step
5. Handle failures with retry or alternative paths
6. Provide meaningful descriptions

OUTPUT FORMAT:
Return a valid Arazzo workflow JSON that follows the schema defined above.

EXAMPLE:
{example_workflow}

Generate the workflow:
```

### Incremental Generation Process

1. **User Request Analysis**: Extract intent and entities
2. **Operation Mapping**: Match request to OpenAPI operations
3. **Step Generation**: Create steps one at a time with LLM
4. **Dependency Resolution**: Link steps with runtime expressions
5. **Validation**: Validate against schema and business rules
6. **Refinement**: Allow user to modify and regenerate specific steps

## Storage Strategy

### PostgreSQL Schema

```sql
CREATE TABLE workflows (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    arazzo_json JSONB NOT NULL,
    file_id UUID REFERENCES uploaded_files(id),
    status VARCHAR(50) NOT NULL,
    version INTEGER NOT NULL DEFAULT 1,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_workflows_status ON workflows(status);
CREATE INDEX idx_workflows_file_id ON workflows(file_id);
CREATE INDEX idx_workflows_arazzo_json ON workflows USING GIN (arazzo_json);
```

### Querying Workflows

```sql
-- Find workflows by operation ID
SELECT * FROM workflows
WHERE arazzo_json @> '{"workflows": [{"steps": [{"operationId": "createUser"}]}]}';

-- Count steps in a workflow
SELECT 
    id,
    name,
    jsonb_array_length(arazzo_json->'workflows'->0->'steps') as step_count
FROM workflows;
```

---

## Next Steps

See [03-semantic-kernel-orchestration.md](./03-semantic-kernel-orchestration.md) for details on how Semantic Kernel generates these workflows.
