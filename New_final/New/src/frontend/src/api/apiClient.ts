import axios, { AxiosInstance, AxiosError, InternalAxiosRequestConfig } from 'axios';
import axiosRetry from 'axios-retry';

// API Configuration
const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000';
const API_KEY = process.env.REACT_APP_API_KEY || 'dev-api-key-change-in-production';

// Create axios instance
const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  timeout: 120000, // 120 seconds for AI generation
  headers: {
    'Content-Type': 'application/json',
  },
});

// Configure retry logic
axiosRetry(apiClient, {
  retries: 3,
  retryDelay: axiosRetry.exponentialDelay,
  retryCondition: (error: AxiosError) => {
    return axiosRetry.isNetworkOrIdempotentRequestError(error) ||
      error.response?.status === 429 ||
      error.response?.status === 503;
  },
});

// Request interceptor - Add API key
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // Add API key to headers
    config.headers['X-API-Key'] = API_KEY;

    // Add trace ID for distributed tracing
    const traceId = crypto.randomUUID();
    config.headers['X-Trace-Id'] = traceId;

    // Store start time for performance tracking
    (config as any).metadata = { startTime: new Date() };

    console.log(`[API] ${config.method?.toUpperCase()} ${config.url}`);

    return config;
  },
  (error) => {
    console.error('[API] Request error:', error);
    return Promise.reject(error);
  }
);

// Response interceptor - Handle errors
apiClient.interceptors.response.use(
  (response) => {
    // Log performance metrics
    const duration = new Date().getTime() - (response.config as any).metadata.startTime.getTime();
    console.log(`[API] ${response.config.url} completed in ${duration}ms`);

    return response;
  },
  (error: AxiosError) => {
    // Handle specific error cases
    if (error.response) {
      switch (error.response.status) {
        case 401:
          console.error('[API] Unauthorized - Invalid API key');
          break;
        case 429:
          console.error('[API] Rate limit exceeded');
          break;
        case 500:
          console.error('[API] Server error:', error.response.data);
          break;
        default:
          console.error(`[API] Error ${error.response.status}:`, error.response.data);
      }
    } else if (error.request) {
      console.error('[API] Network error - service unavailable');
    } else {
      console.error('[API] Error:', error.message);
    }

    return Promise.reject(error);
  }
);

// Circuit breaker implementation
class CircuitBreaker {
  private failureCount = 0;
  private readonly threshold = 5;
  private readonly timeout = 30000;
  private state: 'CLOSED' | 'OPEN' | 'HALF_OPEN' = 'CLOSED';
  private nextAttempt = Date.now();

  async execute<T>(fn: () => Promise<T>): Promise<T> {
    if (this.state === 'OPEN') {
      if (Date.now() < this.nextAttempt) {
        throw new Error('Circuit breaker is OPEN');
      }
      this.state = 'HALF_OPEN';
    }

    try {
      const result = await fn();
      this.onSuccess();
      return result;
    } catch (error) {
      this.onFailure();
      throw error;
    }
  }

  private onSuccess() {
    this.failureCount = 0;
    this.state = 'CLOSED';
  }

  private onFailure() {
    this.failureCount++;
    if (this.failureCount >= this.threshold) {
      this.state = 'OPEN';
      this.nextAttempt = Date.now() + this.timeout;
      console.warn('[Circuit Breaker] OPEN - too many failures');
    }
  }
}

export const circuitBreaker = new CircuitBreaker();

// API Methods
export const api = {
  // File Upload
  uploadFile: async (file: File) => {
    const formData = new FormData();
    formData.append('file', file);

    const response = await apiClient.post('/api/files/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    
    // Transform backend response to frontend format
    const backendFile = response.data;
    return {
      id: backendFile.fileId || backendFile.id,
      name: backendFile.fileName || file.name,
      originalName: backendFile.fileName || file.name,
      fileSize: backendFile.fileSize || file.size,
      contentType: backendFile.contentType || file.type,
      uploadedAt: backendFile.uploadedAt || new Date().toISOString(),
      status: 'Ready' as const,
    };
  },

  // Get all files
  getFiles: async () => {
    const response = await apiClient.get('/api/files');
    // Backend returns { files: [...] }, extract and transform the array
    const backendFiles = response.data.files || [];
    return backendFiles.map((file: any) => ({
      id: file.id,
      name: file.fileName,
      originalName: file.fileName,
      fileSize: file.fileSize,
      contentType: file.contentType,
      uploadedAt: file.uploadedAt,
      status: 'Ready' as const, // Default status since backend doesn't provide it
    }));
  },

  // Get file by ID
  getFile: async (fileId: string) => {
    const response = await apiClient.get(`/api/files/${fileId}`);
    return response.data;
  },

  // Delete file
  deleteFile: async (fileId: string) => {
    await apiClient.delete(`/api/files/${fileId}`);
  },

  // Create workflow
  createWorkflow: async (data: { name: string; description?: string; fileId: string }) => {
    // Transform frontend format to backend format
    const requestBody = {
      workflowName: data.name,
      description: data.description,
      fileId: data.fileId
    };
    const response = await apiClient.post('/api/workflows', requestBody);
    return response.data;
  },

  // Get workflow
  getWorkflow: async (workflowId: string) => {
    const response = await apiClient.get(`/api/workflows/${workflowId}`);
    return response.data;
  },

  // Update workflow
  updateWorkflow: async (workflowId: string, arazzoWorkflow: any) => {
    const response = await apiClient.put(`/api/workflows/${workflowId}`, {
      arazzoWorkflow,
    });
    return response.data;
  },

  // Get workflow progress
  getWorkflowProgress: async (workflowId: string) => {
    const response = await apiClient.get(`/api/workflows/${workflowId}/progress`);
    return response.data;
  },

  // Generate service from workflow
  generateService: async (workflowId: string) => {
    const response = await apiClient.post(
      `/api/workflows/${workflowId}/generate`,
      {},
      {
        responseType: 'blob',
      }
    );
    return response.data;
  },

  // Send chat message
  sendChatMessage: async (data: {
    message: string;
    sessionId: string;
    workflowId: string;
  }) => {
    // Backend expects proper casing and Guid format
    const requestBody = {
      message: data.message,
      sessionId: data.sessionId,
      workflowId: data.workflowId
    };
    const response = await apiClient.post('/api/chat/message', requestBody);
    
    // Transform backend response to frontend format
    return {
      content: response.data.response,
      messageId: response.data.interactionId,
      metadata: {}
    };
  },

  // Get chat history
  getChatHistory: async (sessionId: string) => {
    const response = await apiClient.get(`/api/chat/history/${sessionId}`);
    return response.data;
  },

  // Health check
  healthCheck: async () => {
    const response = await apiClient.get('/health');
    return response.data;
  },
};

export default apiClient;
