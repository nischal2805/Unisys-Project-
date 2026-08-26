# Arazzo Workflow Platform Cleanup Script
# Windows PowerShell Version

Write-Host "========================================" -ForegroundColor Red
Write-Host "Arazzo Workflow Platform Cleanup" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Red
Write-Host ""

Write-Host "⚠ WARNING: This will remove all containers, volumes, and data!" -ForegroundColor Yellow
Write-Host ""

$confirmation = Read-Host "Are you sure you want to continue? (yes/no)"

if ($confirmation -ne "yes") {
    Write-Host "Cleanup cancelled." -ForegroundColor Green
    exit 0
}

Write-Host ""
Write-Host "Stopping all services..." -ForegroundColor Yellow
docker-compose down

Write-Host ""
Write-Host "Removing volumes..." -ForegroundColor Yellow
docker-compose down -v

Write-Host ""
Write-Host "Removing orphaned containers..." -ForegroundColor Yellow
docker-compose down --remove-orphans

Write-Host ""
Write-Host "Pruning Docker system..." -ForegroundColor Yellow
docker system prune -f

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Cleanup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "All containers, volumes, and networks have been removed." -ForegroundColor White
Write-Host "Your .env file has been preserved." -ForegroundColor White
Write-Host ""
Write-Host "To start fresh, run: .\scripts\setup.ps1" -ForegroundColor Cyan
Write-Host ""
