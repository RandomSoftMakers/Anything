@echo off
echo Building Anything MSI Installer...

REM Check if WiX is installed
where candle.exe >nul 2>&1
if %errorlevel% neq 0 (
    echo WiX Toolset not found!
    echo Please install WiX Toolset from: https://wixtoolset.org/
    echo Or use: choco install wixtoolset
    pause
    exit /b 1
)

REM Build the project first if needed
if not exist "..\..\dist\win-x64\Anything.UI.Avalonia.exe" (
    echo Building project...
    cd ..\..
    call build.ps1
    cd packaging\windows
)

REM Create output directory
if not exist "..\..\dist\installer" mkdir "..\..\dist\installer"

REM Compile WiX source
echo Compiling WiX source...
candle.exe -dSourceDir="..\..\dist\win-x64" Product.wxs

REM Link to create MSI
echo Creating MSI...
light.exe -out "..\..\dist\installer\Anything-Setup.msi" Product.wixobj

REM Cleanup
del Product.wixobj 2>nul

if exist "..\..\dist\installer\Anything-Setup.msi" (
    echo.
    echo MSI created successfully: ..\..\dist\installer\Anything-Setup.msi
) else (
    echo.
    echo MSI creation failed!
)

pause
