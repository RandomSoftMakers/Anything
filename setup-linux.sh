#!/bin/bash

# Anything Linux Setup Script
# This script installs Anything on Linux systems

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}Anything - Local File Search Tool${NC}"
echo "Installing Anything..."

# Check if running as root
if [ "$EUID" -ne 0 ]; then
    SUDO="sudo"
else
    SUDO=""
fi

# Detect package manager
if command -v apt-get &> /dev/null; then
    PKG_MANAGER="apt"
elif command -v dnf &> /dev/null; then
    PKG_MANAGER="dnf"
elif command -v yum &> /dev/null; then
    PKG_MANAGER="yum"
elif command -v pacman &> /dev/null; then
    PKG_MANAGER="pacman"
elif command -v zypper &> /dev/null; then
    PKG_MANAGER="zypper"
else
    echo -e "${RED}Unsupported package manager. Please install .NET 10.0 manually.${NC}"
    exit 1
fi

echo -e "${YELLOW}Detected package manager: $PKG_MANAGER${NC}"

# Install .NET 10.0 if not present
if ! command -v dotnet &> /dev/null; then
    echo -e "${YELLOW}Installing .NET 10.0...${NC}"
    case $PKG_MANAGER in
        apt)
            $SUDO apt-get update
            $SUDO apt-get install -y dotnet-sdk-10.0
            ;;
        dnf|yum)
            $SUDO $PKG_MANAGER install -y dotnet-sdk-10.0
            ;;
        pacman)
            $SUDO pacman -S --noconfirm dotnet-sdk
            ;;
        zypper)
            $SUDO zypper install -y dotnet-sdk-10.0
            ;;
    esac
else
    echo -e "${GREEN}.NET is already installed${NC}"
fi

# Build the Avalonia project
echo -e "${YELLOW}Building Anything...${NC}"
dotnet publish Anything.UI.Avalonia/Anything.UI.Avalonia.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -o ./dist/linux-x64

# Install to /usr/local/bin
echo -e "${YELLOW}Installing to /usr/local/bin...${NC}"
$SUDO cp ./dist/linux-x64/Anything.UI.Avalonia /usr/local/bin/anything
$SUDO chmod +x /usr/local/bin/anything

# Create desktop entry
echo -e "${YELLOW}Creating desktop entry...${NC}"
cat > /tmp/anything.desktop << EOF
[Desktop Entry]
Name=Anything
Comment=Lightning fast local file search
Exec=/usr/local/bin/anything
Icon=/usr/local/share/icons/anything.png
Terminal=false
Type=Application
Categories=Utility;Search;
EOF

$SUDO mv /tmp/anything.desktop /usr/share/applications/anything.desktop

# Copy icon if exists
if [ -f "icon.png" ]; then
    $SUDO cp icon.png /usr/local/share/icons/anything.png
fi

echo -e "${GREEN}Installation complete!${NC}"
echo "You can now run 'anything' from your terminal or application menu."
