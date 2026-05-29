@echo off
REM ============================================================
REM  Сборка Anything UI (Qt версия)
REM  Требования: CMake 3.22+, MinGW (из Chocolatey), Qt 6.4.2
REM ============================================================

setlocal enabledelayedexpansion

REM Путь к Qt (установлен через aqt / chocolatey)
set QT_DIR=C:\Qt\6.4.2\mingw_64

REM Путь к MinGW (ставится вместе с qt6-base-dev)
set MINGW_DIR=C:\ProgramData\mingw64\mingw64\bin

REM Путь к Rust DLL
set SEARCHENGINE_SRC=..\SearchEngine\target\release\searchengine.dll

REM Добавляем в PATH
set PATH=%MINGW_DIR%;%QT_DIR%\bin;%PATH%

REM Сборка Rust (SearchEngine + LibAnything)
echo.
echo [1/3] Building Rust SearchEngine...
cd /d "%~dp0..\SearchEngine"
call cargo build --release >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo Failed to build Rust SearchEngine
    exit /b 1
)

REM Конфигурация CMake
echo [2/3] Configuring CMake...
cd /d "%~dp0"
if not exist build mkdir build
cd build
cmake .. -G "MinGW Makefiles" -DCMAKE_PREFIX_PATH="%QT_DIR%" -DCMAKE_BUILD_TYPE=Release
if %ERRORLEVEL% neq 0 (
    echo CMake configuration failed
    exit /b 1
)

REM Сборка
echo [3/3] Building Qt application...
mingw32-make -j %NUMBER_OF_PROCESSORS%
if %ERRORLEVEL% neq 0 (
    echo Build failed
    exit /b 1
)

echo.
echo Build successful! Binary: build\AnythingUIQt.exe
echo To run: cd build ^&& AnythingUIQt.exe
