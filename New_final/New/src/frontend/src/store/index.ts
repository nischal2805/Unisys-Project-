import { configureStore } from '@reduxjs/toolkit';
import fileReducer from './fileSlice';
import chatReducer from './chatSlice';
import workflowReducer from './workflowSlice';

// Ensure proper initial state
const preloadedState = undefined;

export const store = configureStore({
  reducer: {
    file: fileReducer,
    chat: chatReducer,
    workflow: workflowReducer,
  },
  preloadedState,
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: {
        // Ignore these action types for file uploads
        ignoredActions: ['file/uploadFile/pending'],
        // Ignore these paths in the state
        ignoredPaths: ['file.currentFile'],
      },
    }),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
