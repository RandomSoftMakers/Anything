# This branch of Anything is deprecated and will be removed soon. 
# ![icon](https://github.com/AnythingDevelopmentTeam/Anything/blob/legacy/icon.png) Anything

Lightning-fast local file search for Windows, Linux, and macOS.  
Minimalist interface, instant results, open source.

![License](https://img.shields.io/badge/License-GPL--3.0-blue.svg)
![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS-4CAF50)
![Tech](https://img.shields.io/badge/.NET-10.0-512BD4)
![UI](https://img.shields.io/badge/UI-Avalonia)

---
## Why?
- Everything on Windows is great, but it's not open source and not cross-platform.
- Anything is a free, open-source, cross-platform alternative.
---
## Features

- **Fast name search** (no index required)
- **10 built-in themes** (Dark, Light, Catppuccin, Solarized, VS Code, GNOME, Breeze)
- **Smart result filtering** (regex, whole word, path, type, size, date)
- **Plugin system** with dynamic `.dll` loading
- **Clean MVVM architecture** (Avalonia UI)
- **First-run experience** with setup wizard
- **Settings system** with persistent configuration
- **Multiple package formats** (DEB, RPM, MSI)
- **Cross-platform CLI** with unified argument parsing
- **Easy to extend and modify**

---

## Quick Install

**Linux:**
```bash
curl -fsSL https://raw.githubusercontent.com/AnythingDevelopmentTeam/Anything/main/setup-linux.sh | bash
```

**Windows:**
Download and run the MSI installer from [Releases](https://github.com/AnythingDevelopmentTeam/Anything/releases).

See [INSTALL.md](INSTALL.md) for detailed installation instructions.

---

## Comparison: Anything vs Everything vs fsearch vs ripgrep

| Feature | **Anything** | **Everything** | **fsearch** | **ripgrep** |
|---------|---------------|----------------|-------------|-------------|
| **Platforms** | Windows, Linux, macOS | Windows | Linux, Windows (WSL), macOS | Windows, Linux, macOS |
| **Search Type** | By name (no index) | By name | By name | Full-text (CLI) |
| **Search Speed** | Fast, disk-dependent | Instant (index) | Instant (index) | Very high (scan) |
| **FS Indexing** | No | Yes | Yes | No |
| **Content Search** | No | No | No | Yes |
| **UI** | Avalonia | Win32 | GTK | CLI |
| **Dark Theme** | Yes | Partial | Yes | CLI |
| **Extensibility** | High | Low | Medium | High |
| **Open Source** | GPL-3.0 | Closed | GPL-2.0 | MIT |
| **Resource Usage** | Medium | Very low | Low | Project-dependent |
| **Large Disk Support** | Disk-speed dependent | Yes | Yes | Yes |
| **RegEx Support** | Planned | Yes | Yes | Yes |
| **Hotkeys** | Yes | Yes | Yes | CLI |
| **Plugins** | Planned | No | No | Via shell |
| **Installation** | Binary / Package / Build | EXE | Linux packages | CLI |
| **Target Audience** | Users + Developers | Windows mass users | Linux users | Developers |

---

## Screenshots

![Dark Theme](https://github.com/AnythingDevelopmentTeam/Anything/blob/main/ScreenShot_Dark)
![Light Theme](https://github.com/AnythingDevelopmentTeam/Anything/blob/main/Screenshot_Light)

---

## Technologies

- .NET 10.0
- Avalonia UI 11.2
- C# 12

---

## Building from Source

```bash
git clone https://github.com/AnythingDevelopmentTeam/anything.git
cd anything
./build.sh  # or build.ps1 on Windows
```

### Running Avalonia (Cross-platform)
```bash
dotnet run --project Anything.UI.Avalonia/Anything.UI.Avalonia.csproj
```

### Running Android
```bash
dotnet publish Anything.UI.Avalonia.Android/Anything.UI.Avalonia.Android.csproj -c Release -r android-arm64 -o dist/android
```

---

## First Run

On first launch, Anything shows a setup wizard to configure:
- Theme
- Language

Access settings anytime by clicking the gear icon () in the titlebar.

---

## Package Downloads

- **DEB** (Debian/Ubuntu): `anything.deb`
- **RPM** (Fedora/RHEL): `anything.rpm`
- **MSI** (Windows): `Anything-Setup.msi`
- **Portable**: Single-file executables for Windows, Linux, and macOS

See [GitHub Releases](https://github.com/AnythingDevelopmentTeam/Anything/releases) for all downloads.

---

## License

GPL-3.0 - See [LICENSE](LICENSE) file for details.
