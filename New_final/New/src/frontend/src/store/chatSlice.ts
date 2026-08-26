import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { api } from '../api/apiClient';

// Types
export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: string;
  metadata?: {
    workflowStepGenerated?: boolean;
    contextUsed?: string[];
  };
}

export interface ChatSession {
  sessionId: string;
  workflowId: string;
  messages: ChatMessage[];
  createdAt: string;
}

interface ChatState {
  sessions: Record<string, ChatSession>;
  currentSessionId: string | null;
  isTyping: boolean;
  error: string | null;
}

const initialState: ChatState = {
  sessions: {},
  currentSessionId: null,
  isTyping: false,
  error: null,
};

// Async Thunks
export const sendMessage = createAsyncThunk(
  'chat/sendMessage',
  async (
    payload: { message: string; sessionId: string; workflowId: string },
    { dispatch, rejectWithValue }
  ) => {
    try {
      // Add user message immediately
      dispatch(
        addMessage({
          id: crypto.randomUUID(),
          role: 'user',
          content: payload.message,
          timestamp: new Date().toISOString(),
        })
      );

      dispatch(setTyping(true));

      const response = await api.sendChatMessage(payload);

      dispatch(setTyping(false));

      // Add assistant response
      dispatch(
        addMessage({
          id: response.messageId,
          role: 'assistant',
          content: response.content,
          timestamp: new Date().toISOString(),
          metadata: response.metadata,
        })
      );

      return response;
    } catch (error: any) {
      dispatch(setTyping(false));
      return rejectWithValue(error.response?.data?.message || 'Failed to send message');
    }
  }
);

export const fetchChatHistory = createAsyncThunk(
  'chat/fetchHistory',
  async (sessionId: string, { rejectWithValue }) => {
    try {
      const response = await api.getChatHistory(sessionId);
      return { sessionId, messages: response };
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch history');
    }
  }
);

// Slice
const chatSlice = createSlice({
  name: 'chat',
  initialState,
  reducers: {
    addMessage: (state, action: PayloadAction<ChatMessage>) => {
      const session = state.sessions[state.currentSessionId!];
      if (session) {
        session.messages.push(action.payload);
      }
    },
    setTyping: (state, action: PayloadAction<boolean>) => {
      state.isTyping = action.payload;
    },
    createSession: (state, action: PayloadAction<{ workflowId: string }>) => {
      const sessionId = crypto.randomUUID();
      state.sessions[sessionId] = {
        sessionId,
        workflowId: action.payload.workflowId,
        messages: [],
        createdAt: new Date().toISOString(),
      };
      state.currentSessionId = sessionId;
    },
    setCurrentSession: (state, action: PayloadAction<string>) => {
      state.currentSessionId = action.payload;
    },
    clearError: (state) => {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    // Send message
    builder
      .addCase(sendMessage.rejected, (state, action) => {
        state.error = action.payload as string;
      });

    // Fetch history
    builder
      .addCase(fetchChatHistory.fulfilled, (state, action) => {
        const { sessionId, messages } = action.payload;
        if (state.sessions[sessionId]) {
          state.sessions[sessionId].messages = messages;
        }
      })
      .addCase(fetchChatHistory.rejected, (state, action) => {
        state.error = action.payload as string;
      });
  },
});

export const { addMessage, setTyping, createSession, setCurrentSession, clearError } =
  chatSlice.actions;
export default chatSlice.reducer;
