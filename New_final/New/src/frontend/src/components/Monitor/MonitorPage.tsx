import React, { useEffect } from "react";
import { useDispatch, useSelector } from "react-redux";
import { AppDispatch, RootState } from "../../store";
import { fetchWorkflows } from "../../store/workflowSlice";
import "./MonitorPage.css";

const MonitorPage: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { workflows, loading, error } = useSelector(
    (state: RootState) => state.workflow
  );

  useEffect(() => {
    dispatch(fetchWorkflows());
  }, [dispatch]);

  const getStatusColor = (status: string) => {
    switch (status) {
      case "Completed":
        return "#48bb78";
      case "InProgress":
        return "#4299e1";
      case "Failed":
        return "#f56565";
      default:
        return "#a0aec0";
    }
  };

  return (
    <div className="monitor-page">
      <div className="card">
        <h2>Workflow Monitor</h2>
        <p className="subtitle">
          Track the status and details of all generated workflows.
        </p>
      </div>

      {loading && (
        <div className="loading">
          <div className="spinner"></div>
        </div>
      )}

      {!loading && error && (
        <div className="card error-card">
          <p>Error: {error}</p>
        </div>
      )}

      {!loading && Object.keys(workflows).length === 0 && (
        <div className="card">
          <div className="empty-state">
            <p>No workflows found. Start by generating a new workflow.</p>
          </div>
        </div>
      )}

      {!loading && Object.keys(workflows).length > 0 && (
        <div className="workflow-list">
          {Object.values(workflows).map((workflow) => (
            <div key={workflow.id} className="card workflow-item">
              <h3>{workflow.name}</h3>
              <p className="workflow-id">ID: {workflow.id}</p>
              <p className="workflow-description">{workflow.description}</p>
              <div className="workflow-details">
                <span
                  className="status-badge"
                  style={{ backgroundColor: getStatusColor(workflow.status) }}
                >
                  {workflow.status}
                </span>
                <span className="timestamp">
                  Created: {new Date(workflow.createdAt).toLocaleString()}
                </span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default MonitorPage;
