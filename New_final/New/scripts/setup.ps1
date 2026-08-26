# Arazzo Workflow Platform Setup Script
# Windows PowerShell Version

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Arazzo Workflow Platform Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if Docker is installed
Write-Host "Checking Docker installation..." -ForegroundColor Yellow
try {
    $dockerVersion = docker --version
    Write-Host "✓ Docker found: $dockerVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ Docker is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install Docker Desktop from: https://www.docker.com/products/docker-desktop" -ForegroundColor Yellow
    exit 1
}

# Check if Docker is running
Write-Host "Checking if Docker is running..." -ForegroundColor Yellow
try {
    docker ps | Out-Null
    Write-Host "✓ Docker is running" -ForegroundColor Green
} catch {
    Write-Host "✗ Docker is not running" -ForegroundColor Red
    Write-Host "Please start Docker Desktop and try again" -ForegroundColor Yellow
    exit 1
}

# Check if docker-compose is available
Write-Host "Checking Docker Compose installation..." -ForegroundColor Yellow
try {
    $composeVersion = docker-compose --version
    Write-Host "✓ Docker Compose found: $composeVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ Docker Compose is not installed" -ForegroundColor Red
    exit 1
}

# Create .env file if it doesn't exist
Write-Host ""
Write-Host "Setting up environment configuration..." -ForegroundColor Yellow
if (-Not (Test-Path ".env")) {
    Write-Host "Creating .env file from template..." -ForegroundColor Yellow
    Copy-Item ".env.example" ".env"
    
    # Generate random API key
    $apiKey = [System.Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
    $dbPassword = [System.Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
    
    # Update .env file with generated values
    (Get-Content ".env") -replace 'API_KEY=change-this-to-a-secure-random-key-in-production', "API_KEY=$apiKey" | Set-Content ".env"
    (Get-Content ".env") -replace 'POSTGRES_PASSWORD=change-this-secure-password', "POSTGRES_PASSWORD=$dbPassword" | Set-Content ".env"
    
    Write-Host "✓ .env file created with secure random keys" -ForegroundColor Green
    Write-Host ""
    Write-Host "IMPORTANT: Your API key is: $apiKey" -ForegroundColor Yellow
    Write-Host "Save this key - you'll need it to access the API!" -ForegroundColor Yellow
    Write-Host ""
} else {
    Write-Host "✓ .env file already exists" -ForegroundColor Green
}

# Create necessary directories
Write-Host "Creating directory structure..." -ForegroundColor Yellow
$directories = @(
    "infrastructure/database/init",
    "infrastructure/database/migrations",
    "infrastructure/docker/phi-llm",
    "infrastructure/docker/qdrant",
    "infrastructure/observability/jaeger",
    "src/frontend",
    "src/backend",
    "src/services/ingest-service",
    "src/services/context-retriever",
    "src/services/service-generator"
)

foreach ($dir in $directories) {
    if (-Not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
}
Write-Host "✓ Directory structure created" -ForegroundColor Green

# Pull Docker images
Write-Host ""
Write-Host "Pulling Docker images (this may take a while)..." -ForegroundColor Yellow
Write-Host ""

docker-compose pull

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Docker images pulled successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Failed to pull Docker images" -ForegroundColor Red
    exit 1
}

# Start infrastructure services first
Write-Host ""
Write-Host "Starting infrastructure services..." -ForegroundColor Yellow
Write-Host ""

docker-compose up -d postgres qdrant jaeger

Write-Host ""
Write-Host "Waiting for services to be healthy (this may take 30-60 seconds)..." -ForegroundColor Yellow

# Wait for services to be healthy
$maxAttempts = 30
$attempt = 0
$allHealthy = $false

while (-Not $allHealthy -and $attempt -lt $maxAttempts) {
    $attempt++
    Write-Host "Checking health... (Attempt $attempt/$maxAttempts)" -ForegroundColor Gray
    Start-Sleep -Seconds 2
    
    $postgresHealth = docker inspect --format='{{.State.Health.Status}}' arazzo-postgres 2>$null
    $qdrantHealth = docker inspect --format='{{.State.Health.Status}}' arazzo-qdrant 2>$null
    
    if ($postgresHealth -eq "healthy" -and $qdrantHealth -eq "healthy") {
        $allHealthy = $true
        Write-Host "✓ All infrastructure services are healthy" -ForegroundColor Green
    }
}

if (-Not $allHealthy) {
    Write-Host "✗ Services failed to become healthy in time" -ForegroundColor Red
    Write-Host "Check logs with: docker-compose logs" -ForegroundColor Yellow
    exit 1
}

# Start Phi LLM and wait for model download
Write-Host ""
Write-Host "Starting Phi LLM service and downloading model..." -ForegroundColor Yellow
Write-Host "This may take 5-10 minutes depending on your internet connection..." -ForegroundColor Yellow
Write-Host ""

docker-compose up -d phi-llm

Write-Host "Waiting for Phi model to download..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

$modelReady = $false
$modelAttempt = 0
while (-Not $modelReady -and $modelAttempt -lt 60) {
    $modelAttempt++
    $phiLogs = docker logs arazzo-phi-llm 2>&1
    if ($phiLogs -match "success") {
        $modelReady = $true
        Write-Host "✓ Phi model downloaded successfully" -ForegroundColor Green
    } else {
        Write-Host "." -NoNewline -ForegroundColor Gray
        Start-Sleep -Seconds 10
    }
}

Write-Host ""

# Initialize Qdrant collection
Write-Host ""
Write-Host "Initializing Qdrant collection..." -ForegroundColor Yellow

$qdrantBody = @{
    vectors = @{
        size = 1536
        distance = "Cosine"
    }
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "http://localhost:6333/collections/openapi_embeddings" -Method Put -Body $qdrantBody -ContentType "application/json" | Out-Null
    Write-Host "✓ Qdrant collection created" -ForegroundColor Green
} catch {
    Write-Host "⚠ Qdrant collection may already exist (this is okay)" -ForegroundColor Yellow
}

# Start application services
Write-Host ""
Write-Host "Starting application services..." -ForegroundColor Yellow
Write-Host ""

docker-compose up -d

Write-Host ""
Write-Host "Waiting for all services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 20

# Check service status
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Service Status" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
docker-compose ps

# Display access URLs
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Application URLs:" -ForegroundColor White
Write-Host "  Frontend:              http://localhost:3000" -ForegroundColor Cyan
Write-Host "  Backend API:           http://localhost:5000" -ForegroundColor Cyan
Write-Host "  Swagger Docs:          http://localhost:5000/swagger" -ForegroundColor Cyan
Write-Host "  Jaeger Tracing:        http://localhost:16686" -ForegroundColor Cyan
Write-Host "  Qdrant Dashboard:      http://localhost:6333/dashboard" -ForegroundColor Cyan
Write-Host ""
Write-Host "Useful Commands:" -ForegroundColor White
Write-Host "  View logs:             docker-compose logs -f" -ForegroundColor Gray
Write-Host "  Stop all:              docker-compose down" -ForegroundColor Gray
Write-Host "  Restart service:       docker-compose restart <service>" -ForegroundColor Gray
Write-Host "  View status:           docker-compose ps" -ForegroundColor Gray
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor White
Write-Host "  1. Open http://localhost:3000 in your browser" -ForegroundColor Gray
Write-Host "  2. Upload an OpenAPI specification file" -ForegroundColor Gray
Write-Host "  3. Start chatting to generate your Arazzo workflow" -ForegroundColor Gray
Write-Host ""
Write-Host "For troubleshooting, see: docs/deployment/troubleshooting.md" -ForegroundColor Yellow
Write-Host ""
