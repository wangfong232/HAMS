Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "   HAMS - HOSPITAL APPOINTMENT MANAGEMENT SYSTEM (SWD392 DEMO)" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host ""

$rootDir = $PSScriptRoot

Write-Host " [1/3] Khoi dong HAMSPatient (Port 5140)..." -ForegroundColor Yellow
Start-Process cmd.exe -ArgumentList "/k title HAMSPatient (5140) && dotnet run --project `"$rootDir\HAMSPatient`" --launch-profile http"

Write-Host " [2/3] Khoi dong HAMSOperations (Port 5115)..." -ForegroundColor Yellow
Start-Process cmd.exe -ArgumentList "/k title HAMSOperations (5115) && dotnet run --project `"$rootDir\HAMSOperations`" --launch-profile http"

Write-Host " [3/3] Khoi dong HAMSClinical (Port 5165)..." -ForegroundColor Yellow
Start-Process cmd.exe -ArgumentList "/k title HAMSClinical (5165) && dotnet run --project `"$rootDir\HAMSClinical`" --launch-profile http"

Write-Host ""
Write-Host " Vui long doi 5 giay de cac ung dung khoi dong..." -ForegroundColor Gray
Start-Sleep -Seconds 5

Start-Process "http://localhost:5140"
Start-Process "http://localhost:5115"
Start-Process "http://localhost:5165"

Write-Host ""
Write-Host "======================================================================" -ForegroundColor Green
Write-Host "   HE THONG DA SAN SANG CHO BUOI DEMO!" -ForegroundColor Green
Write-Host "======================================================================" -ForegroundColor Green
Write-Host " 1. Benh nhan : http://localhost:5140"
Write-Host " 2. Le tan   : http://localhost:5115"
Write-Host " 3. Bac si   : http://localhost:5165"
