import React from "react";
import {
  BrowserRouter as Router,
  Route,
  Routes,
  NavLink,
} from "react-router-dom";
import { Provider } from "react-redux";
import { store } from "./store";
import ChatPage from "./components/Chat/ChatPage";
import FileUploadPage from "./components/FileUpload/FileUploadPage";
import MonitorPage from "./components/Monitor/MonitorPage";
import ResponseRefiningPage from "./components/ResponseRefining/ResponseRefiningPage";
import ErrorBoundary from "./components/ErrorBoundary/ErrorBoundary";
import "./App.css";

const App: React.FC = () => {
  return (
    <Provider store={store}>
      <ErrorBoundary>
        <Router>
          <div className="app-container">
            <header className="app-header">
              <h1>Arazzo Workflow Platform</h1>
              <nav>
                <NavLink
                  to="/upload"
                  className={({ isActive }) => (isActive ? "active" : "")}
                >
                  Upload
                </NavLink>
                <NavLink
                  to="/chat"
                  className={({ isActive }) => (isActive ? "active" : "")}
                >
                  Chat
                </NavLink>
                <NavLink
                  to="/monitor"
                  className={({ isActive }) => (isActive ? "active" : "")}
                >
                  Monitor
                </NavLink>
                <NavLink
                  to="/refine"
                  className={({ isActive }) => (isActive ? "active" : "")}
                >
                  Refine
                </NavLink>
              </nav>
            </header>
            <main className="app-main">
              <Routes>
                <Route path="/upload" element={<FileUploadPage />} />
                <Route path="/chat" element={<ChatPage />} />
                <Route path="/monitor" element={<MonitorPage />} />
                <Route path="/refine" element={<ResponseRefiningPage />} />
                <Route path="/" element={<FileUploadPage />} />
              </Routes>
            </main>
          </div>
        </Router>
      </ErrorBoundary>
    </Provider>
  );
};

export default App;
