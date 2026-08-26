import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";
import apiClient from "../api/apiClient";

// Types
export interface ArazzoWorkflow {
  arazzo: string;
  info: {
    title: string;
    version: string;
    summary?: string;
    description?: string;
  };
  sourceDescriptions: Array<{
    name: string;
    url: string;
    type: string;
  }>;
  workflows: Array<{
    workflowId: string;
    summary?: string;
    description?: string;
    steps: Array<any>;
  }>;
}

export interface Workflow {
  id: string;
  name: string;
  description?: string;
  fileId?: string;
  arazzoJson: ArazzoWorkflow;
  status: "Draft" | "InProgress" | "Completed" | "Failed";
  createdAt: string;
  updatedAt: string;
}

interface WorkflowState {
  workflows: Record<string, Workflow>;
  currentWorkflowId: string | null;
  generationStatus: "idle" | "generating" | "completed" | "failed";
  loading: boolean;
  error: string | null;
}

const initialState: WorkflowState = {
  workflows: {},
  currentWorkflowId: null,
  generationStatus: "idle",
  loading: false,
  error: null,
};

// Async Thunks
export const generateWorkflow = createAsyncThunk(
  "workflow/generateWorkflow",
  async (fileId: string, { rejectWithValue }) => {
    try {
      const response = await apiClient.post("/api/workflows", {
        FileId: fileId,
        WorkflowName: `Workflow for ${fileId.substring(0, 8)}`,
        Description: "Auto-generated workflow from uploaded file",
      });
      return response.data;
    } catch (error: any) {
      return rejectWithValue(
        error.response?.data?.message || "Failed to generate workflow"
      );
    }
  }
);

export const fetchWorkflows = createAsyncThunk(
  "workflow/fetchWorkflows",
  async (_, { rejectWithValue }) => {
    try {
      const response = await apiClient.get("/api/workflows");
      return response.data;
    } catch (error: any) {
      return rejectWithValue(
        error.response?.data?.message || "Failed to fetch workflows"
      );
    }
  }
);

export const refineWorkflow = createAsyncThunk(
  "workflow/refineWorkflow",
  async (
    { workflowId, suggestions }: { workflowId: string; suggestions: string },
    { rejectWithValue }
  ) => {
    try {
      const response = await apiClient.post(
        `/api/workflows/${workflowId}/refine`,
        {
          suggestions,
        }
      );
      return response.data;
    } catch (error: any) {
      return rejectWithValue(
        error.response?.data?.message || "Failed to refine workflow"
      );
    }
  }
);

export const updateWorkflow = createAsyncThunk(
  "workflow/updateWorkflow",
  async (
    { workflowId, arazzoWorkflow }: { workflowId: string; arazzoWorkflow: string },
    { rejectWithValue }
  ) => {
    try {
      const response = await apiClient.put(
        `/api/workflows/${workflowId}`,
        {
          arazzoWorkflow,
        }
      );
      return response.data;
    } catch (error: any) {
      return rejectWithValue(
        error.response?.data?.message || "Failed to update workflow"
      );
    }
  }
);

export const generateService = createAsyncThunk(
  "workflow/generateService",
  async (workflowId: string, { rejectWithValue }) => {
    try {
      const response = await apiClient.get(
        `/api/workflows/${workflowId}/generate`,
        {
          responseType: "blob",
        }
      );
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement("a");
      link.href = url;
      link.setAttribute("download", `generated-service-${workflowId}.zip`);
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
      return workflowId;
    } catch (error: any) {
      return rejectWithValue(
        error.response?.data?.message || "Failed to generate service"
      );
    }
  }
);

// Slice
const workflowSlice = createSlice({
  name: "workflow",
  initialState,
  reducers: {
    setCurrentWorkflowId: (state, action: PayloadAction<string>) => {
      state.currentWorkflowId = action.payload;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(generateWorkflow.pending, (state) => {
        state.loading = true;
        state.error = null;
        state.generationStatus = "generating";
      })
      .addCase(
        generateWorkflow.fulfilled,
        (state, action: PayloadAction<any>) => {
          state.loading = false;
          // Parse arazzoJson string to object
          const workflow: Workflow = {
            ...action.payload,
            arazzoJson:
              typeof action.payload.arazzoJson === "string"
                ? JSON.parse(action.payload.arazzoJson)
                : action.payload.arazzoJson,
          };
          state.workflows[workflow.id] = workflow;
          state.currentWorkflowId = workflow.id;
          state.generationStatus = "completed";
        }
      )
      .addCase(generateWorkflow.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
        state.generationStatus = "failed";
      })
      .addCase(fetchWorkflows.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(
        fetchWorkflows.fulfilled,
        (state, action: PayloadAction<{ workflows: any[] }>) => {
          state.loading = false;
          if (
            action.payload.workflows &&
            Array.isArray(action.payload.workflows)
          ) {
            action.payload.workflows.forEach((workflowData) => {
              // Parse arazzoJson string to object
              const workflow: Workflow = {
                ...workflowData,
                arazzoJson:
                  typeof workflowData.arazzoJson === "string"
                    ? JSON.parse(workflowData.arazzoJson)
                    : workflowData.arazzoJson,
              };
              state.workflows[workflow.id] = workflow;
            });
          }
        }
      )
      .addCase(fetchWorkflows.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      })
      .addCase(refineWorkflow.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(
        refineWorkflow.fulfilled,
        (state, action: PayloadAction<any>) => {
          state.loading = false;
          // Parse arazzoJson string to object
          const workflow: Workflow = {
            ...action.payload,
            arazzoJson:
              typeof action.payload.arazzoJson === "string"
                ? JSON.parse(action.payload.arazzoJson)
                : action.payload.arazzoJson,
          };
          state.workflows[workflow.id] = workflow;
          state.currentWorkflowId = workflow.id;
        }
      )
      .addCase(refineWorkflow.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      })
      .addCase(updateWorkflow.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(
        updateWorkflow.fulfilled,
        (state, action: PayloadAction<any>) => {
          state.loading = false;
          const workflow: Workflow = {
            ...action.payload,
            arazzoJson:
              typeof action.payload.arazzoJson === "string"
                ? JSON.parse(action.payload.arazzoJson)
                : action.payload.arazzoJson,
          };
          state.workflows[workflow.id] = workflow;
        }
      )
      .addCase(updateWorkflow.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      })
      .addCase(generateService.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(generateService.fulfilled, (state) => {
        state.loading = false;
      })
      .addCase(generateService.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      });
  },
});

export const { setCurrentWorkflowId } = workflowSlice.actions;
export default workflowSlice.reducer;
