Act as a Principal Software Architect with deep expertise in building LLM-powered, cloud-native, and resilient microservices. Your task is to generate a comprehensive technical design and project plan for a new platform based on the provided architecture.

The final output must be a production-ready blueprint that a senior development team can use for implementation, covering functional and non-functional requirements.

<project_objective>
The primary goal is to create a platform that enables a user to upload an OpenAPI specification and, through an interactive chat interface, generate an "Arazzo workflow." This workflow must then be convertible into a fully functional, documented C# business service with API endpoints and code export capabilities. The entire system must be designed for a seamless local-first deployment via a single Docker Compose command.
</project_objective>

<core_requirements>
1.  **Input:** The system must accept an OpenAPI specification file via a dedicated `File Upload and Management Page`.
2.  **Chat Interface:** A web-based `Chat pane` for interactive LLM-driven development of the Arazzo workflow.
3.  **LLM-Powered Generation:** Use the **Phi** LLM, orchestrated by **Microsoft Semantic Kernel**, to guide the workflow creation.
4.  **Workflow to Service:** The generated `Arazzo Workflow` will be transformed into a deployable C# business service by a `Service Generator`.
5.  **Outputs:** The final service must include auto-generated documentation, exposed API endpoints, and a code export option.
6.  **Monitoring:** A `Progress Monitor` system must track all interactions, storing this data in a `Main DB`.
7.  **Local First Deployment:** The entire stack must be orchestrated with a single `docker-compose.yml`.
</core_requirements>

<technical_stack_and_constraints>
- **Frontend:** **React** with **Redux**.
- **Backend:** **C#** (.NET 8+), following **Clean Architecture**.
- **LLM Orchestration:** **Microsoft Semantic Kernel**.
- **LLM:** **Phi** (containerized).
- **Vector Database:** **Qdrant** (containerized).
- **Data Persistence:** A main relational database (e.g., PostgreSQL) for tracking progress and workflow state.
- **Containerization:** All services must be Dockerized.
</technical_stack_and_constraints>

<!-- NEW: Non-Functional Requirements (NFRs) -->
<non_functional_requirements>
1.  **Observability:** The system must be fully observable. All services must emit structured logs, metrics, and traces using the **OpenTelemetry** standard. The `Progress Monitor` should be the consumer of these traces for its tracking purposes.
2.  **Resiliency:** Service-to-service communication must be resilient. Implement retry policies and circuit breakers (e.g., using **Polly**) for all cross-service API calls.
3.  **Configuration Management:** All service configurations (connection strings, API keys, service URLs) must be externalized from the Docker images and managed via `.env` files for the local `docker-compose` setup.
4.  **Security:** The backend API endpoints must be secured. Propose a simple but effective strategy for the local environment (e.g., an API key passed in the header).
</non_functional_requirements>

Please structure your response using the following detailed template, referencing the components from the architecture diagram:

**1. High-Level Architecture Diagram**
   - Provide a MermaidJS `graph TD` diagram that digitally recreates the provided architecture, clearly showing all services and the flow of data between them.

**2. Detailed Service & Component Breakdown**

   - **Frontend (React & Redux):**
     - **Component Structure:** Detail the purpose and props for `FileUploadPage`, `ChatPane`, `ResponseRefiningPage`, and `MonitorPage`.
     - **State Management (Redux):** Propose a Redux Toolkit slice structure (e.g., `chatSlice`, `fileSlice`, `workflowSlice`).
     - **API Client:** Design a centralized API client using `axios`, including interceptors to handle the API key and resiliency policies.

   - **ASP.NET Backend Service (C#):**
     - **Project Structure:** Define the Clean Architecture projects (`Domain`, `Application`, `Infrastructure`, `WebAPI`).
     - **API Controllers:** Define the endpoints for `FileUploaderController` and `PromptController`.
     - **Semantic Kernel Orchestration:**
       - **Arazzo Workflow Schema:** <!-- NEW --> First, define the JSON schema for the `Arazzo Workflow`. This is the central data artifact. What fields are necessary to define the business logic? (e.g., `name`, `description`, `steps: [{type, functionId, inputs, outputs}]`).
       - **Orchestration Flows:** Detail the implementation plan for the following flows using Semantic Kernel, explaining how you will use **function calling/tooling** to generate the `Arazzo Workflow` JSON schema:
         1.  `Question Refining Flow`: Provide a sample prompt and the expected LLM interaction.
         2.  `Context Retrieving Flow`: How it calls the `Context Retriever` service.
         3.  `Task Breakdown Flow`: How it uses the LLM and retrieved context to generate the `steps` array for the `Arazzo Workflow`.
         4.  `Result Summarizing Flow`: How it synthesizes the final, complete `Arazzo Workflow` object.
     - **Resiliency:** Show where and how Polly would be configured for calls to other services.

   - **Ingest Service (C#):**
     - **Purpose & API:** A service for processing the OpenAPI spec. Define its single endpoint (e.g., `POST /api/ingest`).
     - **Chunking Strategies:** Detail how to implement configurable chunking strategies (e.g., by path, by schema) using Semantic Kernel's text chunkers.
     - **Workflow:** `Injester` receives file content -> uses a `Chunking Strategy` -> generates embeddings -> stores in **Qdrant**.

   - **Context Retriever Service (C#):**
     - **Purpose & API:** To fetch context for the LLM. Define its primary endpoint (e.g., `POST /api/retrieve`).
     - **Retrievers:** Detail the logic for the three retriever types:
       1.  `User Input Retriever`: Fetches from chat history.
       2.  `Private Knowledge Retriever`: Queries **Qdrant** using vector search.
       3.  `Internet Knowledge Retriever (plugin)`: An optional Semantic Kernel plugin.
     - **Output:** Aggregates context into a structured object for the main backend service.

   - **Service Generator (C#):**
     - **Input:** A finalized `Arazzo Workflow` JSON object.
     - **Code Generation Strategy:** Detail the plan to convert the workflow data into C# code. Strongly recommend and detail the use of the **Scriban** templating library. Provide a small example of a Scriban template for generating a C# method.
     - **Output:** A `.zip` file containing a complete, compilable C# service project.

**3. Observability and Monitoring Strategy** <!-- NEW -->
   - **OpenTelemetry Setup:** How would you configure OpenTelemetry in the C# services and the React frontend to ensure traces flow end-to-end?
   - **Progress Monitor:** How does the `Progress Monitor` hook into this data? Explain its role as a specialized consumer of trace and log data, storing relevant business events (e.g., "workflow_generated", "user_prompted") in the `Main DB`.

**4. Dockerization & Data Strategy**
   - **`docker-compose.yml`:** Provide a complete YAML file for all services, including `frontend`, `backend-api`, `ingest-service`, `context-retriever`, `qdrant`, `phi-llm`, and `main-db` (PostgreSQL). Include port mappings, health checks, and the use of an `.env` file.
   - **`Main DB` Schema:** Define the SQL `CREATE TABLE` statements for `Workflows` and `InteractionHistory`.
   - **`Qdrant` Schema:** Define the API call to create the Qdrant collection, specifying the vector size and metadata payload index.

**5. Project Plan & Phased Roadmap**
   - **Phase 0: Foundation & Tooling:** Setup the repo, CI/CD pipeline, and the core `docker-compose` file. Implement the **Observability** stack (e.g., Jaeger/Zipkin for local tracing).
   - **Phase 1: Ingestion & Retrieval:** Build the `Ingest Service` and `Context Retriever`. The goal is to successfully query the OpenAPI spec.
   - **Phase 2: Core Orchestration:** Implement the backend's AI flows to generate the `Arazzo Workflow` JSON.
   - **Phase 3: Service Generation & UI:** Build the `Service Generator` and the main `Chat pane` in the UI.
   - **Phase 4: Polish & Monitoring:** Implement the `Monitor Page`, refine error handling, and add comprehensive tests.