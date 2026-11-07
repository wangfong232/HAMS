Write-Host "======================================================================" -ForegroundColor Yellow
Write-Host "   DANG TAT CAC DICH VU HAMS (PORT 5140, 5115, 5165)..." -ForegroundColor Yellow
Write-Host "======================================================================" -ForegroundColor Yellow

$ports = @(5140, 5115, 5165)
foreach ($p in $ports) {
    Get-NetTCPConnection -LocalPort $p -ErrorAction SilentlyContinue | ForEach-Object {
        Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue
    }
}
Stop-Process -Name "VBCSCompiler" -Force -ErrorAction SilentlyContinue

Write-Host "Da giai phong toan bo cac port va tien trinh bien dich thanh cong!" -ForegroundColor Green
