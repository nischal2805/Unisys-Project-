import React, { useState, useRef, useEffect } from "react";
import { useDispatch, useSelector } from "react-redux";
import { sendMessage, createSession, addMessage } from "../../store/chatSlice";
import { uploadFile } from "../../store/fileSlice";
import { generateWorkflow, generateService } from "../../store/workflowSlice";
import { AppDispatch, RootState } from "../../store";
import { CodePreviewPanel } from "./CodePreviewPanel";
import "./ChatPage.css";

const ChatPage: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { sessions, currentSessionId, isTyping, error } = useSelector(
    (state: RootState) => state.chat
  );
  const {
    currentWorkflowId,
    workflows,
    loading: workflowLoading,
  } = useSelector((state: RootState) => state.workflow);
  const { currentFile, uploads } = useSelector(
    (state: RootState) => state.file
  );

  const [message, setMessage] = useState("");
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [showFilePreview, setShowFilePreview] = useState(false);
  const [showPreviewPanel, setShowPreviewPanel] = useState(false);
  const [previewContent, setPreviewContent] = useState("");
  const [previewType, setPreviewType] = useState<
    "json" | "csharp" | "typescript"
  >("json");
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const currentSession = currentSessionId ? sessions[currentSessionId] : null;
  const currentWorkflow = currentWorkflowId
    ? workflows[currentWorkflowId]
    : null;

  // Auto-create session when we have a workflow
  useEffect(() => {
    if (!currentSessionId && currentWorkflowId) {
      dispatch(createSession({ workflowId: currentWorkflowId }));
    }
  }, [dispatch, currentSessionId, currentWorkflowId]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [currentSession?.messages]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Handle file upload first if a file is selected
    if (selectedFile) {
      try {
        // Add user message showing file upload intent
        const userMessage =
          message.trim() ||
          `Please analyze this OpenAPI spec and create an Arazzo workflow`;

        // Add the user's message to chat
        dispatch(
          addMessage({
            id: crypto.randomUUID(),
            role: "user",
            content: `**Uploading:** ${selectedFile.name}\n\n${userMessage}`,
            timestamp: new Date().toISOString(),
          })
        );

        const uploadResult = await dispatch(uploadFile(selectedFile)).unwrap();

        // Auto-create workflow from uploaded file
        if (uploadResult.id) {
          const workflowName = `Workflow from ${selectedFile.name}`;

          // Create workflow (this calls Phi3!)
          const workflowResult = await dispatch(
            generateWorkflow(uploadResult.id)
          ).unwrap();

          // Show preview panel with generated Arazzo
          if (workflowResult.arazzoJson) {
            const formatted = JSON.stringify(
              JSON.parse(workflowResult.arazzoJson),
              null,
              2
            );
            setPreviewContent(formatted);
            setPreviewType("json");
            setShowPreviewPanel(true);
          }

          // Create session if doesn't exist
          if (!currentSessionId) {
            await dispatch(createSession({ workflowId: workflowResult.id }));
          }

          // Add AI-generated workflow summary to chat
          setTimeout(() => {
            const arazzoPreview = workflowResult.arazzoJson
              ? JSON.stringify(
                  JSON.parse(workflowResult.arazzoJson),
                  null,
                  2
                ).substring(0, 500)
              : "Workflow generated";

            dispatch(
              addMessage({
                id: crypto.randomUUID(),
                role: "assistant",
                content: `**Workflow Created Successfully!**\n\n**File:** ${selectedFile.name}\n**Workflow:** ${workflowName}\n\nI've analyzed your OpenAPI specification and generated an Arazzo workflow.\n\n**Generated Workflow Preview:**\n\`\`\`json\n${arazzoPreview}...\n\`\`\`\n\n**What you can do next:**\n- Ask me to "Explain the workflow steps"\n- Request "Generate C# service code"\n- Say "Add authentication"\n- Ask "Show me the complete Arazzo JSON"\n\nWhat would you like to do?`,
                timestamp: new Date().toISOString(),
              })
            );
          }, 500);
        }

        setSelectedFile(null);
        setShowFilePreview(false);
        setMessage("");
      } catch (error: any) {
        console.error("File upload or workflow creation failed:", error);
        alert(
          `Error: ${
            error.message ||
            "Failed to upload file and create workflow. Check console for details."
          }`
        );
        setSelectedFile(null);
        setShowFilePreview(false);
      }
      return;
    }

    // Handle regular chat message
    if (!message.trim()) return;

    // If no workflow exists yet, prompt user to upload a file first
    if (!currentWorkflowId && !currentFile) {
      alert(
        "Please upload an OpenAPI specification file first to start creating workflows."
      );
      return;
    }

    // Create session if it doesn't exist
    let sessionId = currentSessionId;
    if (!sessionId && currentWorkflowId) {
      await dispatch(createSession({ workflowId: currentWorkflowId }));
      // The session will be created in the store, use the new currentSessionId
      sessionId = currentSessionId;
    }

    if (currentWorkflowId) {
      await dispatch(
        sendMessage({
          sessionId: sessionId || "temp",
          message: message.trim(),
          workflowId: currentWorkflowId,
        })
      );
      setMessage("");
    }
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (
      file &&
      (file.name.endsWith(".json") ||
        file.name.endsWith(".yaml") ||
        file.name.endsWith(".yml"))
    ) {
      setSelectedFile(file);
      setShowFilePreview(true);
    } else {
      alert(
        "Please select a valid OpenAPI specification file (.json, .yaml, or .yml)"
      );
    }
    // Reset input
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  const handleRemoveFile = () => {
    setSelectedFile(null);
    setShowFilePreview(false);
  };

  const handleGenerateService = async () => {
    if (!currentWorkflowId) {
      alert("Please create a workflow first before generating a service.");
      return;
    }

    try {
      await dispatch(generateService(currentWorkflowId)).unwrap();
      alert("Service generated and downloaded successfully!");
    } catch (error) {
      console.error("Service generation failed:", error);
      alert("Failed to generate service. Please try again.");
    }
  };

  return (
    <div className="chat-page split-screen-layout">
      {/* Left side: Chat interface */}
      <div
        className={`chat-container ${showPreviewPanel ? "with-preview" : ""}`}
      >
        <div className="chat-inner">
          <div className="chat-header">
            <div className="header-content">
              <h2>Arazzo Workflow AI Assistant</h2>
              <p className="chat-subtitle">
                Upload your OpenAPI spec and chat with AI to create Arazzo
                workflows and generate business services.
              </p>
            </div>
            <button
              className="preview-toggle-button"
              onClick={() => setShowPreviewPanel(!showPreviewPanel)}
              title={showPreviewPanel ? "Hide preview" : "Show preview"}
            >
              {showPreviewPanel ? "Hide Preview" : "Show Preview"}
            </button>
            {currentFile && (
              <div className="current-file-badge">
                Working with: <strong>{currentFile.originalName}</strong>
              </div>
            )}
            {currentWorkflow && (
              <div className="workflow-status-badge">
                Workflow: <strong>{currentWorkflow.name}</strong>
                <span
                  className={`status-indicator status-${currentWorkflow.status.toLowerCase()}`}
                >
                  {currentWorkflow.status}
                </span>
                <button
                  className="button button-small button-success"
                  onClick={handleGenerateService}
                  disabled={
                    workflowLoading || currentWorkflow.status !== "Completed"
                  }
                >
                  Generate & Download Service
                </button>
              </div>
            )}
          </div>

          {error && <div className="error-message">{error}</div>}

          <div className="messages-container">
            {!currentSession || currentSession.messages.length === 0 ? (
              <div className="empty-chat">
                <div className="welcome-icon">🚀</div>
                <h3>Welcome to Arazzo Workflow Generator!</h3>
                <p className="welcome-description">
                  This AI-powered assistant helps you transform OpenAPI
                  specifications into executable Arazzo workflows.
                </p>

                <div className="getting-started">
                  <h4>Getting Started:</h4>
                  <ol className="steps-list">
                    <li>Upload your OpenAPI specification (JSON/YAML)</li>
                    <li>Describe the workflow you want to create</li>
                    <li>Let AI generate and refine your Arazzo workflow</li>
                    <li>Download the generated business service</li>
                  </ol>
                </div>

                <div className="upload-prompt">
                  <p>
                    <strong>
                      Click the attachment icon below to upload your OpenAPI
                      spec and get started!
                    </strong>
                  </p>
                </div>

                {uploads.length > 0 && (
                  <div className="recent-files">
                    <h4>📂 Recent Files:</h4>
                    <div className="recent-files-list">
                      {uploads.slice(0, 3).map((file) => (
                        <button
                          key={file.id}
                          className="recent-file-button"
                          onClick={async () => {
                            await dispatch(generateWorkflow(file.id));
                          }}
                        >
                          {file.originalName}
                        </button>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            ) : (
              <>
                {currentSession.messages.map((msg, idx) => (
                  <div key={idx} className={`message message-${msg.role}`}>
                    <div className="message-avatar">
                      {msg.role === "user"
                        ? "U"
                        : msg.role === "system"
                        ? "S"
                        : "AI"}
                    </div>
                    <div className="message-content">
                      <div className="message-text">
                        {/* Format message with simple markdown-like rendering */}
                        {(() => {
                          const content = msg.content;
                          // Check for code blocks
                          if (content.includes("```")) {
                            const parts = content.split("```");
                            return parts.map((part, idx) => {
                              if (idx % 2 === 1) {
                                // This is a code block
                                const lines = part.split("\n");
                                const code = lines.slice(1).join("\n").trim();
                                return (
                                  <pre key={idx} className="code-block">
                                    <code>{code}</code>
                                  </pre>
                                );
                              }
                              // Regular text
                              return (
                                <div key={idx}>
                                  {part.split("\n").map((line, i) => (
                                    <div key={i}>
                                      {line.startsWith("**") &&
                                      line.endsWith("**") ? (
                                        <strong>
                                          {line.replace(/\*\*/g, "")}
                                        </strong>
                                      ) : line.startsWith("- ") ? (
                                        <li style={{ marginLeft: "20px" }}>
                                          {line.substring(2)}
                                        </li>
                                      ) : (
                                        <span>{line}</span>
                                      )}
                                    </div>
                                  ))}
                                </div>
                              );
                            });
                          }
                          // No code blocks, render normally
                          return msg.content
                            .split("\n")
                            .map((line, i) => (
                              <div key={i}>
                                {line.startsWith("**") &&
                                line.endsWith("**") ? (
                                  <strong>{line.replace(/\*\*/g, "")}</strong>
                                ) : line.startsWith("- ") ? (
                                  <li style={{ marginLeft: "20px" }}>
                                    {line.substring(2)}
                                  </li>
                                ) : (
                                  <span>{line}</span>
                                )}
                              </div>
                            ));
                        })()}
                      </div>
                      <div className="message-time">
                        {new Date(msg.timestamp).toLocaleTimeString()}
                      </div>
                    </div>
                  </div>
                ))}
                {isTyping && (
                  <div className="message message-assistant">
                    <div className="message-avatar">AI</div>
                    <div className="message-content">
                      <div className="typing-indicator">
                        <span></span>
                        <span></span>
                        <span></span>
                      </div>
                      <div className="ai-thinking-text">
                        Processing...
                      </div>
                    </div>
                  </div>
                )}
                {workflowLoading && (
                  <div className="message message-assistant">
                    <div className="message-avatar">AI</div>
                    <div className="message-content">
                      <div className="workflow-generating">
                        <div className="spinner"></div>
                        <span>
                          Analyzing OpenAPI spec and generating Arazzo
                          workflow...
                        </span>
                      </div>
                    </div>
                  </div>
                )}
                <div ref={messagesEndRef} />
              </>
            )}
          </div>

          {showFilePreview && selectedFile && (
            <div className="file-preview-bar">
              <span className="file-preview-icon">📎</span>
              <span className="file-preview-name">{selectedFile.name}</span>
              <span className="file-preview-size">
                ({(selectedFile.size / 1024).toFixed(2)} KB)
              </span>
              <button
                className="file-preview-remove"
                onClick={handleRemoveFile}
              >
                ✕
              </button>
            </div>
          )}

          <form className="chat-input-form" onSubmit={handleSubmit}>
            <input
              type="file"
              ref={fileInputRef}
              accept=".json,.yaml,.yml"
              onChange={handleFileSelect}
              style={{ display: "none" }}
            />
            <button
              type="button"
              className="button button-icon"
              onClick={() => fileInputRef.current?.click()}
              title="Upload OpenAPI Spec"
            >
              📎
            </button>
            <input
              type="text"
              className="chat-input"
              placeholder={
                selectedFile
                  ? "Add a description or instructions for the workflow..."
                  : currentWorkflowId
                  ? "Describe how to modify the workflow..."
                  : "Upload an OpenAPI spec to start..."
              }
              value={message}
              onChange={(e) => setMessage(e.target.value)}
              disabled={isTyping}
            />
            <button
              type="submit"
              className="button button-primary"
              disabled={(!selectedFile && !message.trim()) || isTyping}
            >
              {selectedFile ? "Upload & Create" : "Send"}
            </button>
          </form>
        </div>
      </div>

      {/* Right side: Code Preview Panel (Bolt.new/Claude Artifacts style) */}
      {showPreviewPanel && (
        <CodePreviewPanel
          title="Generated Arazzo Workflow"
          content={previewContent}
          type={previewType}
          isGenerating={workflowLoading}
        />
      )}
    </div>
  );
};

export default ChatPage;
