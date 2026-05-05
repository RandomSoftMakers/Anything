# Anything - Installation Guide

## Quick Install

### Linux
```bash
curl -fsSL https://raw.githubusercontent.com/RandomSoftMakers/Anything/main/setup-linux.sh | bash
```

Or download the setup script and run:
```bash
chmod +x setup-linux.sh
./setup-linux.sh
```

### Windows
Download the installer from [Releases](https://github.com/RandomSoftMakers/Anything/releases) and run `Anything-Setup.msi`.

### Build from source
```bash
git clone https://github.com/RandomSoftMakers/Anything.git
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
- Theme (Dark/Light)
- Titlebar preference (Native/Custom)
- Language

## Settings
Access settings by clicking the gear icon (⚙) in the titlebar, where you can configure:
- Theme
- Native titlebar
- Max search results
- Language
