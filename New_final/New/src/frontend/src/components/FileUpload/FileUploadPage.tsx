import React, { useState, useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { uploadFile, fetchFiles } from '../../store/fileSlice';
import { AppDispatch, RootState } from '../../store';
import './FileUploadPage.css';

const FileUploadPage: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { uploads, uploadProgress, loading, error } = useSelector(
    (state: RootState) => state.file
  );

  const [isDragging, setIsDragging] = useState(false);

  const handleDragEnter = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(true);
  }, []);

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);
  }, []);

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
  }, []);

  const handleDrop = useCallback(
    async (e: React.DragEvent) => {
      e.preventDefault();
      e.stopPropagation();
      setIsDragging(false);

      const files = Array.from(e.dataTransfer.files);
      const file = files[0];

      if (file && (file.name.endsWith('.json') || file.name.endsWith('.yaml') || file.name.endsWith('.yml'))) {
        await dispatch(uploadFile(file));
      } else {
        alert('Please upload a valid OpenAPI specification file (.json, .yaml, or .yml)');
      }
    },
    [dispatch]
  );

  const handleFileInput = useCallback(
    async (e: React.ChangeEvent<HTMLInputElement>) => {
      const file = e.target.files?.[0];
      if (file) {
        await dispatch(uploadFile(file));
      }
    },
    [dispatch]
  );

  React.useEffect(() => {
    dispatch(fetchFiles());
  }, [dispatch]);

  return (
    <div className="file-upload-page">
      <div className="card">
        <h2>Upload OpenAPI Specification</h2>
        <p className="subtitle">Upload your OpenAPI 3.x specification file to begin generating Arazzo workflows.</p>

        {error && <div className="error-message">{error}</div>}

        <div
          className={`dropzone ${isDragging ? 'dropzone-active' : ''}`}
          onDragEnter={handleDragEnter}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDrop={handleDrop}
        >
          <div className="dropzone-content">
            <svg className="upload-icon" viewBox="0 0 24 24" width="48" height="48">
              <path
                fill="currentColor"
                d="M9,16V10H5L12,3L19,10H15V16H9M5,20V18H19V20H5Z"
              />
            </svg>
            <p className="dropzone-text">
              {isDragging ? 'Drop your file here' : 'Drag and drop your OpenAPI spec here'}
            </p>
            <p className="dropzone-subtext">or</p>
            <label className="file-input-label">
              <input
                type="file"
                accept=".json,.yaml,.yml"
                onChange={handleFileInput}
                className="file-input"
              />
              <span className="button button-primary">Browse Files</span>
            </label>
            <p className="dropzone-hint">Supported formats: JSON, YAML</p>
          </div>
        </div>

        {uploadProgress > 0 && uploadProgress < 100 && (
          <div className="progress-bar">
            <div className="progress-fill" style={{ width: `${uploadProgress}%` }}></div>
            <span className="progress-text">{uploadProgress}%</span>
          </div>
        )}
      </div>

      <div className="card">
        <h3>Uploaded Files</h3>
        {loading && (
          <div className="loading">
            <div className="spinner"></div>
          </div>
        )}

        {!loading && uploads.length === 0 && (
          <p className="empty-state">No files uploaded yet.</p>
        )}

        {!loading && uploads.length > 0 && (
          <div className="file-list">
            {uploads.map((file) => (
              <div key={file.id} className="file-item">
                <div className="file-info">
                  <span className="file-name">{file.originalName || file.name}</span>
                  {file.status && (
                    <span className={`file-status status-${file.status.toLowerCase()}`}>
                      {file.status}
                    </span>
                  )}
                </div>
                <div className="file-meta">
                  <span className="file-date">
                    Uploaded: {file.uploadedAt ? new Date(file.uploadedAt).toLocaleDateString() : 'N/A'}
                  </span>
                  {file.fileSize && (
                    <span className="file-size">
                      Size: {(file.fileSize / 1024).toFixed(2)} KB
                    </span>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default FileUploadPage;
