# Build WiX installer for Anything
# Requires: WiX Toolset installed

param(
    [string]$SourceDir = ".\dist\win-x64",
    [string]$OutputDir = ".\dist\installer"
)

# Ensure output directory exists
New-Item -ItemType Directory -Force -Path $OutputDir

# Compile WiX source
candle.exe -dSourceDir=$SourceDir Product.wxs

# Link to create MSI
light.exe -out "$OutputDir\Anything-Setup.msi" Product.wixobj

Write-Host "MSI created at: $OutputDir\Anything-Setup.msi"
