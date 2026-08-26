import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { api } from '../api/apiClient';

// Types
export interface UploadedFile {
  id: string;
  name: string;
  originalName: string;
  fileSize: number;
  contentType: string;
  uploadedAt: string;
  status: 'Uploaded' | 'Processing' | 'Ready' | 'Error';
  uploadedBy?: string;
}

interface FileState {
  uploads: UploadedFile[];
  currentFile: UploadedFile | null;
  uploadProgress: number;
  loading: boolean;
  error: string | null;
}

// Helper to ensure state is always correct
const getInitialState = (): FileState => ({
  uploads: [],
  currentFile: null,
  uploadProgress: 0,
  loading: false,
  error: null,
});

const initialState: FileState = getInitialState();

// Async Thunks
export const uploadFile = createAsyncThunk(
  'file/upload',
  async (file: File, { dispatch, rejectWithValue }) => {
    try {
      const response = await api.uploadFile(file);
      return response;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Upload failed');
    }
  }
);

export const fetchFiles = createAsyncThunk(
  'file/fetchAll',
  async (_, { rejectWithValue }) => {
    try {
      const response = await api.getFiles();
      return response;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch files');
    }
  }
);

export const deleteFile = createAsyncThunk(
  'file/delete',
  async (fileId: string, { rejectWithValue }) => {
    try {
      await api.deleteFile(fileId);
      return fileId;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to delete file');
    }
  }
);

// Slice
const fileSlice = createSlice({
  name: 'file',
  initialState,
  reducers: {
    resetFileState: () => getInitialState(),
    setUploadProgress: (state, action: PayloadAction<number>) => {
      state.uploadProgress = action.payload;
    },
    setCurrentFile: (state, action: PayloadAction<UploadedFile | null>) => {
      state.currentFile = action.payload;
    },
    clearError: (state) => {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    // Upload file
    builder
      .addCase(uploadFile.pending, (state) => {
        state.loading = true;
        state.error = null;
        state.uploadProgress = 0;
      })
      .addCase(uploadFile.fulfilled, (state, action) => {
        state.loading = false;
        // Safety check: ensure uploads is an array
        if (!Array.isArray(state.uploads)) {
          state.uploads = [];
        }
        state.uploads.unshift(action.payload);
        state.currentFile = action.payload;
        state.uploadProgress = 100;
      })
      .addCase(uploadFile.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
        state.uploadProgress = 0;
      });

    // Fetch files
    builder
      .addCase(fetchFiles.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchFiles.fulfilled, (state, action) => {
        state.loading = false;
        // Safety check: ensure payload is an array
        state.uploads = Array.isArray(action.payload) ? action.payload : [];
      })
      .addCase(fetchFiles.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      });

    // Delete file
    builder
      .addCase(deleteFile.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(deleteFile.fulfilled, (state, action) => {
        state.loading = false;
        // Safety check: ensure uploads is an array
        if (Array.isArray(state.uploads)) {
          state.uploads = state.uploads.filter((file) => file.id !== action.payload);
        }
        if (state.currentFile?.id === action.payload) {
          state.currentFile = null;
        }
      })
      .addCase(deleteFile.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      });
  },
});

export const { resetFileState, setUploadProgress, setCurrentFile, clearError } = fileSlice.actions;
export default fileSlice.reducer;
