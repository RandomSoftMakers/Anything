@echo off
REM ============================================================
REM  Package a portable Windows release of Anything
REM  Usage:   package_release.bat
REM  Prereq:  build_qt.bat must have succeeded first
REM  Output:  AnythingUIQt-<version>-win64.zip
REM ============================================================
setlocal enabledelayedexpansion

set VERSION=0.1.0
set QT_DIR=C:\Qt\6.4.2\mingw_64
set MINGW_DIR=C:\ProgramData\mingw64\mingw64\bin
set BUILD_DIR=%~dp0build
set STAGE_DIR=%~dp0_stage
set PKG_NAME=AnythingUIQt-%VERSION%-win64

echo Packaging Anything v%VERSION%...

if not exist "%BUILD_DIR%\AnythingUIQt.exe" (
    echo Error: build\AnythingUIQt.exe not found. Run build_qt.bat first.
    exit /b 1
)

rmdir /s /q "%STAGE_DIR%" 2>nul
mkdir "%STAGE_DIR%\%PKG_NAME%"

REM 1. Binary
echo [1/5] Copying binary...
copy "%BUILD_DIR%\AnythingUIQt.exe" "%STAGE_DIR%\%PKG_NAME%\" >nul
copy "%BUILD_DIR%\searchengine.dll" "%STAGE_DIR%\%PKG_NAME%\" >nul

REM 2. Qt runtime DLLs via windeployqt
echo [2/5] Deploying Qt runtime DLLs...
"%QT_DIR%\bin\windeployqt.exe" --no-compiler-runtime --no-system-d3d-compiler "%STAGE_DIR%\%PKG_NAME%\AnythingUIQt.exe" >nul 2>&1

REM 3. MinGW runtime DLLs (not bundled by windeployqt)
echo [3/5] Copying MinGW runtime DLLs...
copy "%MINGW_DIR%\libgcc_s_seh-1.dll"   "%STAGE_DIR%\%PKG_NAME%\" >nul 2>&1
copy "%MINGW_DIR%\libstdc++-6.dll"      "%STAGE_DIR%\%PKG_NAME%\" >nul 2>&1
copy "%MINGW_DIR%\libwinpthread-1.dll"  "%STAGE_DIR%\%PKG_NAME%\" >nul 2>&1

REM 4. Metadata
echo [4/5] Copying metadata...
copy "%~dp0LICENSE"   "%STAGE_DIR%\%PKG_NAME%\" >nul
copy "%~dp0README.md" "%STAGE_DIR%\%PKG_NAME%\" >nul

REM 5. Zip
echo [5/5] Creating archive...
cd /d "%STAGE_DIR%"
powershell -NoLogo -NoProfile -Command "Compress-Archive -Path '%PKG_NAME%\*' -DestinationPath '%~dp0%PKG_NAME%.zip' -Force"
cd /d "%~dp0"

rmdir /s /q "%STAGE_DIR%" 2>nul

echo Done: %~dp0%PKG_NAME%.zip
echo.
echo To run, extract the zip and launch AnythingUIQt.exe.
