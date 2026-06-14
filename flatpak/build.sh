#!/bin/bash
set -euo pipefail

# Build Anything flatpak using flatpak build (not flatpak-builder)
# This avoids needing Rust toolchain inside the flatpak sandbox.

export PATH="$HOME/.cargo/bin:$PATH"
ROOT="$(dirname "$0")/.."

echo "==> Building anything-gui binary..."
cargo build --release --manifest-path "$ROOT/Cargo.toml" --bin anything-gui

echo "==> Initializing flatpak build directory..."
BUILD_DIR="_build"
REPO_DIR="_repo"
rm -rf "$BUILD_DIR" "$REPO_DIR"

flatpak build-init "$BUILD_DIR" io.github.anything org.gnome.Sdk org.gnome.Platform 50

echo "==> Installing files..."
install -Dm755 "$ROOT/target/release/anything-gui" "$BUILD_DIR/files/bin/anything-gui"
install -Dm644 "$ROOT/flatpak/io.github.anything.desktop" "$BUILD_DIR/files/share/applications/io.github.anything.desktop"
install -Dm644 "$ROOT/flatpak/io.github.anything.metainfo.xml" "$BUILD_DIR/files/share/metainfo/io.github.anything.metainfo.xml"
install -Dm644 "$ROOT/icon.png" "$BUILD_DIR/files/share/icons/hicolor/96x96/apps/io.github.anything.png"
install -Dm644 "$ROOT/icon.png" "$BUILD_DIR/files/share/icons/hicolor/256x256/apps/io.github.anything.png"
install -Dm644 "$ROOT/LANG.ru.yaml" "$BUILD_DIR/files/share/anything/LANG.ru.yaml"
install -Dm644 "$ROOT/LANG.en.yaml" "$BUILD_DIR/files/share/anything/LANG.en.yaml"

echo "==> Finishing build..."
flatpak build-finish "$BUILD_DIR" \
    --socket=wayland \
    --socket=fallback-x11 \
    --socket=session-bus \
    --share=ipc \
    --filesystem=host \
    --device=dri

echo "==> Exporting..."
flatpak build-export "$REPO_DIR" "$BUILD_DIR"

echo "==> Building bundle..."
flatpak build-bundle "$REPO_DIR" io.github.anything.flatpak io.github.anything --runtime-repo=https://flathub.org/repo/flathub.flatpakrepo

echo "==> Installing..."
flatpak --user install -y --or-update io.github.anything.flatpak

echo "==> Cleaning up..."
rm -rf "$BUILD_DIR" "$REPO_DIR" io.github.anything.flatpak

echo "==> Done. Installed size: $(flatpak info io.github.anything | grep 'Installed')"
