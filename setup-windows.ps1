# Anything Windows Setup Script
# This script installs Anything on Windows systems

$ErrorActionPreference = "Stop"

# Colors
function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    } else {
        $input | Write-Output
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

Write-ColorOutput Green "Anything - Local File Search Tool"
Write-Output "Installing Anything..."

# Check if .NET 10.0 is installed
$dotnetInstalled = $false
try {
    $dotnetVersion = dotnet --version 2>$null
    if ($dotnetVersion -like "10.*") {
        $dotnetInstalled = $true
    }
} catch {}

if (-not $dotnetInstalled) {
    Write-ColorOutput Yellow "Installing .NET 10.0 SDK..."
    $dotnetInstaller = "https://dot.net/v1/dotnet-install.ps1"
    $installScript = "$env:TEMP\dotnet-install.ps1"
    
    Invoke-WebRequest -Uri $dotnetInstaller -OutFile $installScript
    & $installScript -Channel 10.0 -InstallDir "$env:ProgramFiles\dotnet"
    
    $env:PATH += ";$env:ProgramFiles\dotnet"
    Write-ColorOutput Green ".NET 10.0 installed successfully"
} else {
    Write-ColorOutput Green ".NET is already installed: $dotnetVersion"
}

# Build the Avalonia project
Write-ColorOutput Yellow "Building Anything..."
dotnet publish Anything.UI.Avalonia/Anything.UI.Avalonia.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -o ./dist/win-x64

# Create installer using WiX if available
$wixAvailable = Get-Command candle.exe -ErrorAction SilentlyContinue

if ($wixAvailable) {
    Write-ColorOutput Yellow "Creating MSI installer..."
    
    if (-not (Test-Path packaging/windows)) {
        New-Item -ItemType Directory -Force -Path packaging/windows
    }
    
    # Build MSI
    & packaging/windows/build-msi.ps1 -SourceDir "./dist/win-x64" -OutputDir "./dist/installer"
    
    Write-ColorOutput Green "MSI installer created at: ./dist/installer/Anything-Setup.msi"
    Write-Output "Run the installer to complete setup."
} else {
    # Install directly
    Write-ColorOutput Yellow "Installing to Program Files..."
    
    $installDir = "$env:ProgramFiles\Anything"
    New-Item -ItemType Directory -Force -Path $installDir
    
    Copy-Item -Path ./dist/win-x64/* -Destination $installDir -Recurse -Force
    
    # Add to PATH
    $currentPath = [Environment]::GetEnvironmentVariable("PATH", "Machine")
    if ($currentPath -notlike "*$installDir*") {
        [Environment]::SetEnvironmentVariable("PATH", "$currentPath;$installDir", "Machine")
    }
    
    # Create Start Menu shortcut
    $startMenu = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs"
    $shortcutPath = "$startMenu\Anything.lnk"
    $targetPath = "$installDir\Anything.UI.Avalonia.exe"
    
    $WshShell = New-Object -comObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut($shortcutPath)
    $Shortcut.TargetPath = $targetPath
    $Shortcut.Save()
    
    Write-ColorOutput Green "Installation complete!"
    Write-Output "You can now run 'Anything' from the Start Menu."
}
