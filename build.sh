#!/bin/bash

# Anything Build Script
# Builds the Avalonia project for multiple platforms

set -e

echo "Building Anything..."

# Detect OS
OS="unknown"
if [[ "$OSTYPE" == "linux-gnu"* ]]; then    OS="linux"
elif [[ "$OSTYPE" == "darwin"* ]]; then    OS="macos"
elif [[ "$OSTYPE" == "cygwin" ]] || [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "win32" ]]; then    OS="windows"
fi

echo "Detected OS: $OS"

# Build for current platform
case $OS in
    linux)
        echo "Building for Linux..."
        dotnet publish Anything.UI.Avalonia/Anything.UI.Avalonia.csproj \
            -c Release \
            -r linux-x64 \
            --self-contained true \
            -p:PublishSingleFile=true \
            -o dist/linux-x64
        cp icon.png dist/linux-x64/
        cp icon.ico dist/linux-x64/
        echo "Linux build complete: dist/linux-x64/Anything.UI.Avalonia"
        ;;
    macos)
        echo "Building for macOS..."
        dotnet publish Anything.UI.Avalonia/Anything.UI.Avalonia.csproj \
            -c Release \
            -r osx-x64 \
            --self-contained true \
            -p:PublishSingleFile=true \
            -o dist/osx-x64
        echo "macOS build complete: dist/osx-x64/Anything.UI.Avalonia"
        ;;
    windows)
        echo "Building for Windows..."
        dotnet publish Anything.UI.Avalonia/Anything.UI.Avalonia.csproj \
            -c Release \
            -r win-x64 \
            --self-contained true \
            -p:PublishSingleFile=true \
            -o dist/win-x64
        cp icon.png dist/win-x64/
        cp icon.ico dist/win-x64/
        echo "Windows build complete: dist/win-x64/Anything.UI.Avalonia.exe"
        ;;
    *)
        echo "Unknown OS. Building for all platforms..."
        dotnet publish Anything.UI.Avalonia/Anything.UI.Avalonia.csproj \
            -c Release \
            -r linux-x64 \
            --self-contained true \
            -p:PublishSingleFile=true \
            -o dist/linux-x64

        dotnet publish Anything.UI.Avalonia/Anything.UI.Avalonia.csproj \
            -c Release \
            -r win-x64 \
            --self-contained true \
            -p:PublishSingleFile=true \
            -o dist/win-x64

        dotnet publish Anything.UI.Avalonia/Anything.UI.Avalonia.csproj \
            -c Release \
            -r osx-x64 \
            --self-contained true \
            -p:PublishSingleFile=true \
            -o dist/osx-x64

        # Android (requires .NET MAUI workload)
        echo "Building for Android..."
        dotnet publish Anything.UI.Avalonia.Android/Anything.UI.Avalonia.Android.csproj \
            -c Release \
            -r android-arm64 \
            -o dist/android
        ;;
esac

echo "Build complete!"
