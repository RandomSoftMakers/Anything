# Build WiX installer for Anything
# Requires: WiX Toolset installed

param(
    [string]$SourceDir = ".\dist\win-x64",
    [string]$OutputDir = ".\dist\installer"
)

# Ensure output directory exists
New-Item -ItemType Directory -Force -Path $OutputDir

# Resolve absolute paths
$sourceFull = Resolve-Path $SourceDir
$wxsFile = Join-Path $PSScriptRoot "Product.wxs"

# Copy icon files to source directory for MSI packaging
Copy-Item -Path ".\icon.png" -Destination "$sourceFull\" -Force -ErrorAction SilentlyContinue
Copy-Item -Path ".\icon.ico" -Destination "$sourceFull\" -Force -ErrorAction SilentlyContinue

# Compile WiX source
candle.exe -dSourceDir="$sourceFull" "$wxsFile"

# Link to create MSI
light.exe -out "$OutputDir\Anything-Setup.msi" Product.wixobj

Write-Host "MSI created at: $OutputDir\Anything-Setup.msi"
