# =============================================================================
# NIS2-AuditGuard – Egykattintásos Demó Indító Szkript (start-demo.ps1)
# Készítette: Károlyi Béla (FULABV) – Milton Friedman Egyetem
# =============================================================================
$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $RepoRoot

Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host "  NIS2-AuditGuard (.NET 10 LTS) - Ketzonas + Lokalis AI Demo Indito  " -ForegroundColor Cyan
Write-Host "=====================================================================" -ForegroundColor Cyan

# 1. Lokális Ollama (http://localhost:11434) ellenőrzése
Write-Host "`n[1/4] Lokalis Ollama AI-motor ellenorzese (http://localhost:11434)..." -ForegroundColor Yellow
try {
    $tags = Invoke-RestMethod -Uri "http://localhost:11434/api/tags" -Method Get -TimeoutSec 3
    $modelNames = ($tags.models | ForEach-Object { $_.name }) -join ", "
    Write-Host "  [OK] Ollama szerver elerheto! Telepitett modellek: $modelNames" -ForegroundColor Green
} catch {
    Write-Host "  [FIGYELEM] Az Ollama nem valaszolt a 11434-es porton. A C# rendszer automatikus determinisztikus fallback modban is mukodik." -ForegroundColor DarkYellow
}

# 2. Solution fordítás ellenőrzése
Write-Host "`n[2/4] Solution ellenorzese (dotnet build)..." -ForegroundColor Yellow
dotnet build Nis2AuditGuard.sln --nologo -v q
if ($LASTEXITCODE -ne 0) {
    Write-Error "Hiba a forditas soran!"
    exit 1
}
Write-Host "  [OK] Nis2AuditGuard.sln sikeresen lefordult (0 hiba, 0 figyelmeztetes)." -ForegroundColor Green

# 3. Publikus Előszűrő Zóna (PublicWeb - Port 5070) indítása külön ablakban
Write-Host "`n[3/4] Publikus Eloszuro Zona inditasa (http://localhost:5070)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$RepoRoot'; Write-Host '--- NIS2-AuditGuard PublicWeb (5070) ---' -ForegroundColor Cyan; dotnet run --project src/Nis2AuditGuard.PublicWeb --no-build --urls http://localhost:5070"

# 4. Zárt Menedzseri & Auditori Munkaállomás (ManagerWeb - 127.0.0.1:5080) indítása külön ablakban
Write-Host "[4/4] Zart Menedzseri & Auditori Konzol inditasa (http://127.0.0.1:5080)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$RepoRoot'; Write-Host '--- NIS2-AuditGuard ManagerWeb (127.0.0.1:5080 - Loopback Only) ---' -ForegroundColor Green; dotnet run --project src/Nis2AuditGuard.ManagerWeb --no-build --urls http://127.0.0.1:5080"

Start-Sleep -Seconds 3
Start-Process "http://localhost:5070"
Start-Process "http://127.0.0.1:5080"

Write-Host "`n=====================================================================" -ForegroundColor Green
Write-Host "  MINDKET ZONA ELINDULT ES MEGNYILT A BONGESZOBEN:" -ForegroundColor Green
Write-Host "  1) Publikus Eloszuro & Arajanlat-kalkulator: http://localhost:5070" -ForegroundColor White
Write-Host "  2) Zart Menedzseri & Auditori Konzol:        http://127.0.0.1:5080" -ForegroundColor White
Write-Host "=====================================================================" -ForegroundColor Green

