# Anything Windows Build Script
# Builds the Avalonia project for Windows

$ErrorActionPreference = "Stop"

Write-Output "Building Anything for Windows..."

# Build for Windows
dotnet publish Anything.UI.Avalonia/Anything.UI.Avalonia.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -o dist/win-x64

Write-Output "Windows build complete: dist/win-x64/Anything.UI.Avalonia.exe"

# Optionally build installer
if (Get-Command candle.exe -ErrorAction SilentlyContinue) {
    Write-Output "Building MSI installer..."
    & packaging/windows/build-msi.ps1 -SourceDir "./dist/win-x64" -OutputDir "./dist/installer"
    Write-Output "MSI installer created at: ./dist/installer/Anything-Setup.msi"
} else {
    Write-Output "WiX Toolset not found. Skipping MSI creation."
    Write-Output "Install WiX Toolset to create MSI installer: https://wixtoolset.org/"
}
