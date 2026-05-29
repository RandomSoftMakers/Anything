# Anything

Fast file search with Qt 6 and Rust backend.

## Features

- **Search** — fuzzy and regex, debounced input (300ms)
- **Everything-like syntax** — `!exclude`, `"exact phrase"`, `ext:pdf`, `path:C:\Docs`
- **Dark / light theme** — toggle with `🌙`/`☀️` button
- **Mica effect** (Windows 11) — translucent backdrop via `DwmSetWindowAttribute`
- **About dialog** — version info + syntax reference
- **Cross-platform** — compiles on Windows, Linux, macOS

## Architecture

```
GUI (Qt 6)  →  QLibrary  →  searchengine.{dll,so,dylib}  →  libanything
```

The GUI loads the Rust library dynamically at runtime — no static linking required.

## Build

### Prerequisites

- Qt 6 (Core, Gui, Widgets)
- CMake ≥ 3.22
- C++17 compiler
- Rust toolchain

### Windows (MinGW)

```bat
cd SearchEngine && cargo build --release && cd ..
cd Anything
mkdir build && cd build
cmake .. -G "MinGW Makefiles" -DCMAKE_PREFIX_PATH=C:\Qt\6.4.2\mingw_64 -DCMAKE_BUILD_TYPE=Release
mingw32-make -j4
```

### Linux

```sh
cd SearchEngine && cargo build --release && cd ..
cd Anything
mkdir build && cd build
cmake .. -DCMAKE_PREFIX_PATH=/usr/lib/x86_64-linux-gnu/cmake/Qt6
make -j$(nproc)
```

### macOS

Same as Linux. The Rust library becomes `libsearchengine.dylib`.

## Files

| Path | Description |
|------|-------------|
| `src/main.cpp` | Entry point |
| `src/MainWindow.h/.cpp` | Main window, Mica, themes, search, about |
| `src/SearchEngineApi.h/.cpp` | QLibrary wrapper for searchengine FFI |
| `src/FileResultModel.h/.cpp` | QAbstractListModel for search results |
| `CMakeLists.txt` | CMake project file |
