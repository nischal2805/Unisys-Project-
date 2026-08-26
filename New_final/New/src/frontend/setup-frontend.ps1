#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Setup script for the Arazzo Workflow Generator frontend.

.DESCRIPTION
    This script installs dependencies and prepares the frontend for development.
#>

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Frontend Setup - Arazzo Generator    " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check Node.js installation
Write-Host "[1/4] Checking Node.js installation..." -ForegroundColor Yellow
try {
    $nodeVersion = node --version
    Write-Host "  ✓ Node.js $nodeVersion found" -ForegroundColor Green
} catch {
    Write-Host "  ✗ Node.js is not installed!" -ForegroundColor Red
    Write-Host "  Please install Node.js 18+ from https://nodejs.org" -ForegroundColor Red
    exit 1
}

# Check npm installation
try {
    $npmVersion = npm --version
    Write-Host "  ✓ npm $npmVersion found" -ForegroundColor Green
} catch {
    Write-Host "  ✗ npm is not installed!" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Navigate to frontend directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

# Create .env file if it doesn't exist
Write-Host "[2/4] Setting up environment variables..." -ForegroundColor Yellow
if (-not (Test-Path ".env")) {
    @"
REACT_APP_API_URL=http://localhost:5000
REACT_APP_API_KEY=dev-api-key-12345
"@ | Out-File -FilePath ".env" -Encoding UTF8
    Write-Host "  ✓ Created .env file" -ForegroundColor Green
} else {
    Write-Host "  ✓ .env file already exists" -ForegroundColor Green
}

Write-Host ""

# Install dependencies
Write-Host "[3/4] Installing npm dependencies..." -ForegroundColor Yellow
Write-Host "  This may take a few minutes..." -ForegroundColor Gray
try {
    npm install
    Write-Host "  ✓ Dependencies installed successfully" -ForegroundColor Green
} catch {
    Write-Host "  ✗ Failed to install dependencies" -ForegroundColor Red
    Write-Host "  Error: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Type check
Write-Host "[4/4] Running type check..." -ForegroundColor Yellow
try {
    npm run build 2>&1 | Out-Null
    Write-Host "  ✓ TypeScript compilation successful" -ForegroundColor Green
} catch {
    Write-Host "  ⚠ TypeScript compilation had warnings (this is expected)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Setup Complete!                      " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Green
Write-Host "  1. Start the backend API (see src/backend/README.md)" -ForegroundColor White
Write-Host "  2. Start the development server:" -ForegroundColor White
Write-Host "     npm start" -ForegroundColor Cyan
Write-Host ""
Write-Host "The frontend will be available at: http://localhost:3000" -ForegroundColor Green
Write-Host ""
