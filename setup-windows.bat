@echo off
echo Anything - Local File Search Tool
echo Installing Anything...

REM Check if PowerShell execution policy allows scripts
powershell -Command "Get-ExecutionPolicy" | findstr /i "RemoteSigned Unrestricted Bypass" >nul 2>&1
if %errorlevel% neq 0 (
    echo Setting PowerShell execution policy...
    powershell -Command "Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force"
)

REM Run PowerShell setup script
powershell -ExecutionPolicy Bypass -File "%~dp0setup-windows.ps1"

if %errorlevel% neq 0 (
    echo.
    echo Installation failed. Please try running as Administrator.
    pause
    exit /b 1
)

pause
