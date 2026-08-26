-- =============================================================================
-- Arazzo Workflow Platform Database Schema
-- PostgreSQL 16+
-- =============================================================================

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =============================================================================
-- UPLOADED FILES TABLE
-- =============================================================================

CREATE TABLE uploaded_files (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    original_name VARCHAR(255) NOT NULL,
    file_path VARCHAR(500) NOT NULL,
    file_size BIGINT NOT NULL,
    content_type VARCHAR(100) NOT NULL,
    uploaded_by VARCHAR(255),
    uploaded_at TIMESTAMP NOT NULL DEFAULT NOW(),
    status VARCHAR(50) NOT NULL DEFAULT 'Uploaded',
    checksum VARCHAR(64),
    metadata JSONB,
    CONSTRAINT chk_status CHECK (status IN ('Uploaded', 'Processing', 'Ready', 'Error', 'Deleted'))
);

CREATE INDEX idx_uploaded_files_status ON uploaded_files(status);
CREATE INDEX idx_uploaded_files_uploaded_at ON uploaded_files(uploaded_at DESC);
CREATE INDEX idx_uploaded_files_uploaded_by ON uploaded_files(uploaded_by);

COMMENT ON TABLE uploaded_files IS 'Stores information about uploaded OpenAPI specification files';
COMMENT ON COLUMN uploaded_files.status IS 'Upload status: Uploaded, Processing, Ready, Error, Deleted';
COMMENT ON COLUMN uploaded_files.checksum IS 'SHA256 checksum for file integrity verification';

-- =============================================================================
-- WORKFLOWS TABLE
-- =============================================================================

CREATE TABLE workflows (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    file_id UUID REFERENCES uploaded_files(id) ON DELETE SET NULL,
    arazzo_json JSONB NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Draft',
    version INTEGER NOT NULL DEFAULT 1,
    created_by VARCHAR(255),
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMP,
    error_message TEXT,
    metadata JSONB,
    CONSTRAINT chk_workflow_status CHECK (status IN ('Draft', 'InProgress', 'Completed', 'Failed', 'Archived'))
);

CREATE INDEX idx_workflows_status ON workflows(status);
CREATE INDEX idx_workflows_file_id ON workflows(file_id);
CREATE INDEX idx_workflows_created_at ON workflows(created_at DESC);
CREATE INDEX idx_workflows_arazzo_json ON workflows USING GIN (arazzo_json);

COMMENT ON TABLE workflows IS 'Stores Arazzo workflow definitions and their status';
COMMENT ON COLUMN workflows.arazzo_json IS 'Full Arazzo workflow JSON conforming to Arazzo 1.0.0 specification';
COMMENT ON COLUMN workflows.status IS 'Workflow status: Draft, InProgress, Completed, Failed, Archived';
COMMENT ON COLUMN workflows.version IS 'Version number for tracking workflow iterations';

-- =============================================================================
-- INTERACTION HISTORY TABLE
-- =============================================================================

CREATE TABLE interaction_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workflow_id UUID NOT NULL REFERENCES workflows(id) ON DELETE CASCADE,
    session_id UUID NOT NULL,
    message_type VARCHAR(50) NOT NULL,
    role VARCHAR(50) NOT NULL,
    content TEXT NOT NULL,
    tokens_used INTEGER,
    processing_time_ms INTEGER,
    context_used JSONB,
    metadata JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_message_type CHECK (message_type IN ('UserMessage', 'AssistantMessage', 'SystemMessage', 'ErrorMessage')),
    CONSTRAINT chk_role CHECK (role IN ('user', 'assistant', 'system'))
);

CREATE INDEX idx_interaction_history_workflow_id ON interaction_history(workflow_id);
CREATE INDEX idx_interaction_history_session_id ON interaction_history(session_id);
CREATE INDEX idx_interaction_history_created_at ON interaction_history(created_at DESC);
CREATE INDEX idx_interaction_history_message_type ON interaction_history(message_type);

COMMENT ON TABLE interaction_history IS 'Tracks all chat interactions and LLM responses for workflow development';
COMMENT ON COLUMN interaction_history.session_id IS 'Groups related messages in a conversation session';
COMMENT ON COLUMN interaction_history.tokens_used IS 'Number of LLM tokens consumed for this interaction';
COMMENT ON COLUMN interaction_history.processing_time_ms IS 'Time taken to process the message in milliseconds';
COMMENT ON COLUMN interaction_history.context_used IS 'Context information used for this interaction (sources, embeddings, etc.)';

-- =============================================================================
-- WORKFLOW STEPS TABLE (Denormalized for quick access)
-- =============================================================================

CREATE TABLE workflow_steps (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workflow_id UUID NOT NULL REFERENCES workflows(id) ON DELETE CASCADE,
    step_id VARCHAR(100) NOT NULL,
    step_order INTEGER NOT NULL,
    operation_id VARCHAR(255) NOT NULL,
    operation_path VARCHAR(500),
    description TEXT,
    status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    step_json JSONB NOT NULL,
    execution_count INTEGER DEFAULT 0,
    last_executed_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_step_status CHECK (status IN ('Pending', 'Completed', 'Failed', 'Skipped')),
    CONSTRAINT uq_workflow_step_id UNIQUE (workflow_id, step_id)
);

CREATE INDEX idx_workflow_steps_workflow_id ON workflow_steps(workflow_id);
CREATE INDEX idx_workflow_steps_status ON workflow_steps(status);
CREATE INDEX idx_workflow_steps_operation_id ON workflow_steps(operation_id);

COMMENT ON TABLE workflow_steps IS 'Denormalized workflow steps for performance and tracking';
COMMENT ON COLUMN workflow_steps.step_order IS 'Order of step execution in the workflow';
COMMENT ON COLUMN workflow_steps.execution_count IS 'Number of times this step has been executed';

-- =============================================================================
-- TELEMETRY EVENTS TABLE
-- =============================================================================

CREATE TABLE telemetry_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    trace_id VARCHAR(32) NOT NULL,
    span_id VARCHAR(16) NOT NULL,
    parent_span_id VARCHAR(16),
    service_name VARCHAR(100) NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    event_name VARCHAR(255) NOT NULL,
    workflow_id UUID REFERENCES workflows(id) ON DELETE SET NULL,
    user_id VARCHAR(255),
    duration_ms INTEGER,
    status VARCHAR(50) NOT NULL,
    error_message TEXT,
    attributes JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_telemetry_status CHECK (status IN ('Success', 'Error', 'Warning'))
);

CREATE INDEX idx_telemetry_events_trace_id ON telemetry_events(trace_id);
CREATE INDEX idx_telemetry_events_workflow_id ON telemetry_events(workflow_id);
CREATE INDEX idx_telemetry_events_service_name ON telemetry_events(service_name);
CREATE INDEX idx_telemetry_events_event_type ON telemetry_events(event_type);
CREATE INDEX idx_telemetry_events_created_at ON telemetry_events(created_at DESC);

COMMENT ON TABLE telemetry_events IS 'Stores business events from OpenTelemetry traces for monitoring';
COMMENT ON COLUMN telemetry_events.trace_id IS 'OpenTelemetry trace ID for correlation';
COMMENT ON COLUMN telemetry_events.event_type IS 'Type of event (workflow_created, step_generated, service_exported, etc.)';

-- =============================================================================
-- GENERATED SERVICES TABLE
-- =============================================================================

CREATE TABLE generated_services (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workflow_id UUID NOT NULL REFERENCES workflows(id) ON DELETE CASCADE,
    service_name VARCHAR(255) NOT NULL,
    file_path VARCHAR(500) NOT NULL,
    file_size BIGINT NOT NULL,
    generated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    downloaded_count INTEGER DEFAULT 0,
    last_downloaded_at TIMESTAMP,
    metadata JSONB,
    CONSTRAINT uq_workflow_generated_service UNIQUE (workflow_id)
);

CREATE INDEX idx_generated_services_workflow_id ON generated_services(workflow_id);
CREATE INDEX idx_generated_services_generated_at ON generated_services(generated_at DESC);

COMMENT ON TABLE generated_services IS 'Tracks generated C# service artifacts';
COMMENT ON COLUMN generated_services.downloaded_count IS 'Number of times the service has been downloaded';

-- =============================================================================
-- SYSTEM CONFIGURATION TABLE
-- =============================================================================

CREATE TABLE system_configuration (
    key VARCHAR(100) PRIMARY KEY,
    value TEXT NOT NULL,
    description TEXT,
    value_type VARCHAR(50) NOT NULL,
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_by VARCHAR(255),
    CONSTRAINT chk_value_type CHECK (value_type IN ('String', 'Integer', 'Boolean', 'JSON'))
);

CREATE INDEX idx_system_configuration_updated_at ON system_configuration(updated_at DESC);

COMMENT ON TABLE system_configuration IS 'System-wide configuration settings';

-- =============================================================================
-- AUDIT LOG TABLE
-- =============================================================================

CREATE TABLE audit_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_type VARCHAR(100) NOT NULL,
    entity_id UUID NOT NULL,
    action VARCHAR(100) NOT NULL,
    user_id VARCHAR(255),
    changes JSONB,
    ip_address INET,
    user_agent TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_audit_log_entity ON audit_log(entity_type, entity_id);
CREATE INDEX idx_audit_log_user_id ON audit_log(user_id);
CREATE INDEX idx_audit_log_created_at ON audit_log(created_at DESC);

COMMENT ON TABLE audit_log IS 'Audit trail for all entity changes';
COMMENT ON COLUMN audit_log.changes IS 'JSON representation of what changed (old and new values)';

-- =============================================================================
-- VIEWS
-- =============================================================================

-- View for workflow progress summary
CREATE VIEW vw_workflow_progress AS
SELECT 
    w.id AS workflow_id,
    w.name,
    w.status,
    w.created_at,
    w.updated_at,
    COUNT(ws.id) AS total_steps,
    COUNT(ws.id) FILTER (WHERE ws.status = 'Completed') AS completed_steps,
    COUNT(ws.id) FILTER (WHERE ws.status = 'Failed') AS failed_steps,
    COUNT(ih.id) AS total_interactions,
    SUM(ih.tokens_used) AS total_tokens_used,
    gs.generated_at AS service_generated_at,
    gs.downloaded_count AS service_download_count
FROM workflows w
LEFT JOIN workflow_steps ws ON w.id = ws.workflow_id
LEFT JOIN interaction_history ih ON w.id = ih.workflow_id
LEFT JOIN generated_services gs ON w.id = gs.workflow_id
GROUP BY w.id, w.name, w.status, w.created_at, w.updated_at, gs.generated_at, gs.downloaded_count;

COMMENT ON VIEW vw_workflow_progress IS 'Consolidated view of workflow progress and statistics';

-- View for recent activity
CREATE VIEW vw_recent_activity AS
SELECT 
    'Workflow' AS entity_type,
    id AS entity_id,
    name AS entity_name,
    status AS activity_status,
    created_at AS activity_time
FROM workflows
UNION ALL
SELECT 
    'Upload' AS entity_type,
    id AS entity_id,
    original_name AS entity_name,
    status AS activity_status,
    uploaded_at AS activity_time
FROM uploaded_files
UNION ALL
SELECT 
    'Service' AS entity_type,
    id AS entity_id,
    service_name AS entity_name,
    'Generated' AS activity_status,
    generated_at AS activity_time
FROM generated_services
ORDER BY activity_time DESC
LIMIT 100;

COMMENT ON VIEW vw_recent_activity IS 'Recent system activity across all entity types';

-- =============================================================================
-- FUNCTIONS
-- =============================================================================

-- Function to update workflow updated_at timestamp
CREATE OR REPLACE FUNCTION update_workflow_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger for workflows table
CREATE TRIGGER trg_workflows_update_timestamp
BEFORE UPDATE ON workflows
FOR EACH ROW
EXECUTE FUNCTION update_workflow_timestamp();

-- Function to create audit log entry
CREATE OR REPLACE FUNCTION create_audit_log_entry()
RETURNS TRIGGER AS $$
BEGIN
    IF (TG_OP = 'INSERT') THEN
        INSERT INTO audit_log (entity_type, entity_id, action, changes)
        VALUES (TG_TABLE_NAME, NEW.id, 'INSERT', row_to_json(NEW));
        RETURN NEW;
    ELSIF (TG_OP = 'UPDATE') THEN
        INSERT INTO audit_log (entity_type, entity_id, action, changes)
        VALUES (TG_TABLE_NAME, NEW.id, 'UPDATE', 
                jsonb_build_object('old', row_to_json(OLD), 'new', row_to_json(NEW)));
        RETURN NEW;
    ELSIF (TG_OP = 'DELETE') THEN
        INSERT INTO audit_log (entity_type, entity_id, action, changes)
        VALUES (TG_TABLE_NAME, OLD.id, 'DELETE', row_to_json(OLD));
        RETURN OLD;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Apply audit triggers to key tables
CREATE TRIGGER trg_workflows_audit
AFTER INSERT OR UPDATE OR DELETE ON workflows
FOR EACH ROW
EXECUTE FUNCTION create_audit_log_entry();

CREATE TRIGGER trg_uploaded_files_audit
AFTER INSERT OR UPDATE OR DELETE ON uploaded_files
FOR EACH ROW
EXECUTE FUNCTION create_audit_log_entry();

-- =============================================================================
-- INITIAL DATA
-- =============================================================================

-- Insert default system configuration
INSERT INTO system_configuration (key, value, description, value_type) VALUES
('max_file_size_mb', '10', 'Maximum file upload size in megabytes', 'Integer'),
('max_workflow_steps', '50', 'Maximum number of steps allowed in a workflow', 'Integer'),
('enable_telemetry', 'true', 'Enable OpenTelemetry event tracking', 'Boolean'),
('default_chunk_size', '1000', 'Default chunk size for text chunking', 'Integer'),
('llm_timeout_seconds', '30', 'Timeout for LLM requests in seconds', 'Integer'),
('vector_search_limit', '5', 'Default number of vector search results', 'Integer');

-- =============================================================================
-- GRANTS (Adjust as needed for your security model)
-- =============================================================================

-- Grant permissions to application user
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO arazzo_user;
GRANT SELECT ON ALL SEQUENCES IN SCHEMA public TO arazzo_user;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA public TO arazzo_user;

-- =============================================================================
-- SAMPLE DATA FOR TESTING (Optional - Remove in production)
-- =============================================================================

-- Sample uploaded file
INSERT INTO uploaded_files (id, name, original_name, file_path, file_size, content_type, uploaded_by, status)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'petstore-openapi.yaml',
    'petstore-openapi.yaml',
    '/uploads/petstore-openapi.yaml',
    15234,
    'application/yaml',
    'system',
    'Ready'
);

-- Sample workflow
INSERT INTO workflows (id, name, description, file_id, arazzo_json, status, created_by)
VALUES (
    '00000000-0000-0000-0000-000000000002',
    'Pet Store User Registration',
    'Complete user registration workflow for Pet Store API',
    '00000000-0000-0000-0000-000000000001',
    '{"arazzo": "1.0.0", "info": {"title": "Pet Store Registration", "version": "1.0.0"}, "sourceDescriptions": [], "workflows": []}',
    'Draft',
    'system'
);

-- =============================================================================
-- DATABASE STATISTICS AND MAINTENANCE
-- =============================================================================

-- Analyze tables for query optimization
ANALYZE uploaded_files;
ANALYZE workflows;
ANALYZE interaction_history;
ANALYZE workflow_steps;
ANALYZE telemetry_events;

-- =============================================================================
-- END OF SCHEMA
-- =============================================================================
