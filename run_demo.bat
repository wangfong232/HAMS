@echo off
setlocal
title HAMS - Hospital Appointment Management System
color 0b

echo ======================================================================
echo    HAMS - HOSPITAL APPOINTMENT MANAGEMENT SYSTEM (SWD392 DEMO)
echo ======================================================================
echo.

set "ROOT_DIR=%~dp0"

echo  [0/3] Dang bien dich toan bo Solution HAMS.slnx...
dotnet build "%ROOT_DIR%HAMS.slnx"
if %ERRORLEVEL% neq 0 (
    echo.
    echo ======================================================================
    echo  [LOI] Bien dich that bai! Vui long kiem tra lai code truoc khi chay.
    echo ======================================================================
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo  [1/3] Khoi dong HAMSPatient  (Port 5140)...
start "HAMSPatient (5140)" cmd /k dotnet run --project "%ROOT_DIR%HAMSPatient" --no-build --launch-profile http

echo  [2/3] Khoi dong HAMSOperations (Port 5115)...
start "HAMSOperations (5115)" cmd /k dotnet run --project "%ROOT_DIR%HAMSOperations" --no-build --launch-profile http

echo  [3/3] Khoi dong HAMSClinical  (Port 5165)...
start "HAMSClinical (5165)" cmd /k dotnet run --project "%ROOT_DIR%HAMSClinical" --no-build --launch-profile http

echo.
echo  Vui long doi 3 giay de cac ung dung khoi tao...
ping 127.0.0.1 -n 4 >nul

echo.
echo  Dang mo 3 phan he tren trinh duyet...
start http://localhost:5140
start http://localhost:5115
start http://localhost:5165

echo.
echo ======================================================================
echo    HE THONG DA SAN SANG CHO BUOI DEMO!
echo ======================================================================
echo  1. Benh nhan (PhongTQ) : http://localhost:5140
echo  2. Le tan   (Minh Duc) : http://localhost:5115
echo  3. Bac si   (Duc Anh)  : http://localhost:5165
echo.
echo  * Meo:
echo    - Nut [Reset Demo] mau do nam o goc phai Menu de lam moi DB.
echo    - Chay file 'stop_demo.bat' de tat tat ca cac ung dung.
echo ======================================================================
pause
