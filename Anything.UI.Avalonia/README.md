# Anything UI (Avalonia)

Cross-platform UI for Anything built with Avalonia UI framework.

## Features

- **Cross-platform**: Runs on Windows, Linux, and macOS
- **First-run experience**: Onboarding wizard for new users
- **Settings system**: Configure theme, titlebar, language, and more
- **Theme support**: Dark and Light themes with runtime switching
- **Native titlebar**: Optional native titlebar support
- **Settings window**: Easy access to all configuration options

## Building

```bash
dotnet build Anything.UI.Avalonia.csproj
```

## Running

```bash
dotnet run --project Anything.UI.Avalonia/Anything.UI.Avalonia.csproj
```

## Publishing for different platforms

### Windows
```bash
dotnet publish Anything.UI.Avalonia.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/win-x64
```

### Linux
```bash
dotnet publish Anything.UI.Avalonia.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o publish/linux-x64
```

### macOS
```bash
dotnet publish Anything.UI.Avalonia.csproj -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -o publish/osx-x64
```
