# Anything - Installation Guide

## Quick Install

### Linux
```bash
curl -fsSL https://raw.githubusercontent.com/AnythingDevelopmentTeam/Anything/main/setup-linux.sh | bash
```

Or download the setup script and run:
```bash
chmod +x setup-linux.sh
./setup-linux.sh
```

### Windows
Download and run the setup script:
```powershell
# Using PowerShell
.\setup-windows.ps1
```

Or use the batch file (right-click and "Run as Administrator"):
```
setup-windows.bat
```

Or download the installer from [Releases](https://github.com/AnythingDevelopmentTeam/Anything/releases) and run `Anything-Setup.msi`.

### Build from source
```bash
git clone https://github.com/AnythingDevelopmentTeam/Anything.git
cd Anything
./build.sh  # or build.ps1 on Windows
```

## Package Managers

### Debian/Ubuntu
```bash
sudo dpkg -i anything.deb
```

### Fedora/RHEL
```bash
sudo rpm -i anything.rpm
```

## First Run
On first launch, Anything will show a setup wizard to configure:
- Theme
- Language

## Settings
Access settings by clicking the gear icon (⚙) in the titlebar, where you can configure:
- Theme
- Max search results
- Language
- Plugins
