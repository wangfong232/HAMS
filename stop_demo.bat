@echo off
setlocal
title HAMS - Stop All Demo Services
color 0c

echo ======================================================================
echo    DANG TAT CAC DICH VU HAMS (PORT 5140, 5115, 5165)...
echo ======================================================================
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command "$ports = @(5140, 5115, 5165); foreach ($p in $ports) { Get-NetTCPConnection -LocalPort $p -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue } }; Stop-Process -Name 'VBCSCompiler' -Force -ErrorAction SilentlyContinue; exit 0"

echo.
echo ======================================================================
echo    TAT CA DICH VU HAMS DA DUOC DUNG VA GIAI PHONG PORT THANH CONG!
echo ======================================================================
ping 127.0.0.1 -n 3 >nul
