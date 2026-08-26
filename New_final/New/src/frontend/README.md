# Frontend - Arazzo Workflow Generator

This is the React frontend for the Arazzo Workflow Generator Platform.

## Technology Stack

- **React 18** with TypeScript
- **Redux Toolkit** for state management
- **React Router** for navigation
- **Axios** with retry logic and circuit breaker for API calls
- **CSS Modules** for styling

## Getting Started

### Prerequisites

- Node.js 18+ and npm
- Backend API running on http://localhost:5000

### Installation

```bash
# Install dependencies
npm install

# Start development server
npm start

# Build for production
npm run build
```

### Environment Variables

Create a `.env` file in the `src/frontend` directory:

```env
REACT_APP_API_URL=http://localhost:5000
REACT_APP_API_KEY=your-api-key-here
```

## Project Structure

```
src/
├── api/
│   └── apiClient.ts          # Axios client with retry logic
├── components/
│   ├── FileUpload/
│   │   ├── FileUploadPage.tsx
│   │   └── FileUploadPage.css
│   ├── Chat/
│   │   ├── ChatPage.tsx
│   │   └── ChatPage.css
│   ├── Monitor/
│   │   ├── MonitorPage.tsx
│   │   └── MonitorPage.css
│   └── ResponseRefining/
│       ├── ResponseRefiningPage.tsx
│       └── ResponseRefiningPage.css
├── store/
│   ├── index.ts              # Redux store configuration
│   ├── fileSlice.ts          # File upload state
│   ├── chatSlice.ts          # Chat state
│   └── workflowSlice.ts      # Workflow state
├── App.tsx                   # Main app component
├── App.css                   # Global styles
└── index.tsx                 # Entry point
```

## Features

### 1. File Upload
- Drag-and-drop interface
- Support for OpenAPI JSON/YAML files
- Upload progress tracking
- File list management

### 2. Chat Interface
- Real-time chat with LLM assistant
- Message history
- Typing indicators
- Sample prompts for quick start

### 3. Workflow Monitor
- View workflow details
- Track generation progress
- Display Arazzo workflow steps
- Status indicators

### 4. Response Refining
- JSON editor for workflow customization
- Format validation
- Save changes
- Generate C# service code

## API Integration

The frontend connects to the backend API at `http://localhost:5000` with the following endpoints:

- `POST /api/files/upload` - Upload OpenAPI spec
- `GET /api/files` - Get uploaded files
- `POST /api/workflows` - Create workflow
- `GET /api/workflows/:id` - Get workflow details
- `PUT /api/workflows/:id` - Update workflow
- `POST /api/chat/messages` - Send chat message
- `GET /api/workflows/:id/generate` - Generate C# service

All requests include:
- `X-API-Key` header for authentication
- `X-Trace-Id` header for distributed tracing

## State Management

Redux Toolkit manages application state:

### File Slice
```typescript
{
  uploads: UploadedFile[],
  currentFile: UploadedFile | null,
  uploadProgress: number,
  loading: boolean,
  error: string | null
}
```

### Chat Slice
```typescript
{
  sessions: Record<string, ChatSession>,
  currentSessionId: string | null,
  isTyping: boolean,
  error: string | null
}
```

### Workflow Slice
```typescript
{
  workflows: Record<string, Workflow>,
  currentWorkflowId: string | null,
  generationStatus: 'idle' | 'generating' | 'completed' | 'failed',
  loading: boolean,
  error: string | null
}
```

## Development

### Running Tests
```bash
npm test
```

### Linting
```bash
npm run lint
```

### Type Checking
```bash
npm run type-check
```

## Deployment

The frontend is containerized with Docker. See the root `docker-compose.yml` for configuration.

### Docker
```bash
# Build and run with Docker Compose
docker-compose up -d frontend
```

The application will be available at http://localhost:3000

## Browser Support

- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)
