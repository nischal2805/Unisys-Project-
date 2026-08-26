import React, { useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import { updateWorkflow, generateService } from "../../store/workflowSlice";
import { AppDispatch, RootState } from "../../store";
import "./ResponseRefiningPage.css";

const ResponseRefiningPage: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { workflows, currentWorkflowId, generationStatus, loading, error } = useSelector(
    (state: RootState) => state.workflow
  );

  const currentWorkflow = currentWorkflowId
    ? workflows[currentWorkflowId]
    : null;
  const [jsonEditor, setJsonEditor] = useState(
    currentWorkflow ? JSON.stringify(currentWorkflow.arazzoJson, null, 2) : ""
  );
  const [saveSuccess, setSaveSuccess] = useState(false);

  React.useEffect(() => {
    if (currentWorkflow) {
      setJsonEditor(JSON.stringify(currentWorkflow.arazzoJson, null, 2));
    }
  }, [currentWorkflow]);

  const handleSave = async () => {
    if (!currentWorkflowId) return;

    try {
      // Validate JSON first
      JSON.parse(jsonEditor);
      
      await dispatch(
        updateWorkflow({ workflowId: currentWorkflowId, arazzoWorkflow: jsonEditor })
      ).unwrap();
      
      setSaveSuccess(true);
      setTimeout(() => setSaveSuccess(false), 3000);
    } catch (err: any) {
      if (err instanceof SyntaxError) {
        alert("Invalid JSON format. Please check your syntax.");
      } else {
        alert(`Failed to save: ${err.message || 'Unknown error'}`);
      }
    }
  };

  const handleGenerate = async () => {
    if (!currentWorkflowId) return;
    await dispatch(generateService(currentWorkflowId));
  };

  const handleFormat = () => {
    try {
      const parsed = JSON.parse(jsonEditor);
      setJsonEditor(JSON.stringify(parsed, null, 2));
    } catch (err) {
      alert("Invalid JSON format. Cannot format.");
    }
  };

  return (
    <div className="refining-page">
      <div className="card">
        <h2>Refine Arazzo Workflow</h2>
        <p className="subtitle">
          Review and edit the generated Arazzo workflow before generating the C#
          service code.
        </p>
      </div>

      {error && <div className="error-message">{error}</div>}

      {!currentWorkflow ? (
        <div className="card">
          <div className="empty-state">
            <p>
              No workflow available. Generate a workflow first using the chat
              interface.
            </p>
          </div>
        </div>
      ) : (
        <>
          <div className="card">
            <div className="editor-header">
              <h3>Arazzo JSON Editor</h3>
              <div className="editor-actions">
                <button
                  className="button button-secondary"
                  onClick={handleFormat}
                >
                  Format JSON
                </button>
                <button
                  className="button button-primary"
                  onClick={handleSave}
                  disabled={loading}
                >
                  {loading ? 'Saving...' : 'Save Changes'}
                </button>
              </div>
            </div>
            {saveSuccess && (
              <div className="success-message">
                ✓ Changes saved successfully!
              </div>
            )}
            <textarea
              className="json-editor"
              value={jsonEditor}
              onChange={(e) => setJsonEditor(e.target.value)}
              spellCheck={false}
            />
          </div>

          <div className="card">
            <h3>Generate C# Service</h3>
            <p className="generate-description">
              Once you're satisfied with the workflow, generate a
              production-ready C# service implementation.
            </p>
            <div className="generate-actions">
              <button
                className="button button-primary generate-button"
                onClick={handleGenerate}
                disabled={generationStatus === "generating"}
              >
                {generationStatus === "generating" ? (
                  <>
                    <div className="spinner-small"></div>
                    Generating...
                  </>
                ) : (
                  "Generate Service Code"
                )}
              </button>
            </div>
            {generationStatus === "completed" && (
              <div className="success-message">
                ✓ Service code generated successfully! Check your downloads
                folder.
              </div>
            )}
            {generationStatus === "failed" && (
              <div className="error-message">
                ✗ Failed to generate service code. Please try again.
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
};

export default ResponseRefiningPage;
