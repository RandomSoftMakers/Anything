# Anything - Windows Installation

## Quick Setup

### Option 1: Automated Setup (Recommended)
1. Download `setup-windows.bat` from the repository
2. Right-click on `setup-windows.bat` and select "Run as Administrator"
3. Follow the prompts

Or using PowerShell:
```powershell
.\setup-windows.ps1
```

### Option 2: Build from Source
```powershell
# Build the project
.\build.ps1

# The output will be in dist/win-x64/
```

### Option 3: MSI Installer
If you have WiX Toolset installed:
```powershell
.\packaging\windows\build-msi.ps1
```

## Prerequisites

- Windows 10/11
- .NET 10.0 SDK (will be installed automatically if missing)
- WiX Toolset (optional, for MSI creation)

## What the Setup Script Does

1. Checks for .NET 10.0 and installs it if missing
2. Builds the Anything application
3. Installs to `C:\Program Files\Anything\`
4. Creates Start Menu shortcut
5. Optionally creates MSI installer if WiX is available

## Manual Installation

If you prefer to install manually:
1. Run `.\build.ps1` to build the project
2. Copy `dist/win-x64/*` to your preferred directory
3. Add that directory to your PATH
4. Create shortcuts as needed

## Troubleshooting

**"Execution Policy" error:**
```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
```

**Build fails:**
- Ensure .NET 10.0 SDK is installed: `dotnet --version`
- Run PowerShell as Administrator

**MSI creation fails:**
- Install WiX Toolset from https://wixtoolset.org/
