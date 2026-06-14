# Anything

GUI for LibAnything with SearchEngine

## Features

- **Search** — fuzzy and regex, debounced input (300ms)
- **Everything-like syntax** — `!exclude`, `"exact phrase"`, `ext:pdf`, `path:`
- **Dark / light theme** — toggle from header bar
- **Settings** — background indexing toggle, rebuild index button
- **Persistent YAML index** — instant startup after first scan
- **Background auto-indexer** — indexes `/` on first run

## Architecture

```
anything-gui (GTK4 + LibAdwaita)  ──→  searchengine (Rust)  ──→  libanything (Rust)
      │                                      │
      └── search queries                     │
                                             ├── walks / recursively
                                             └── writes ~/.config/anything-index.yaml
```

LibAnything and SearchEngine compile into a single `.so`/`.dll` for dynamic loading.

## Build

### Prerequisites

- Rust toolchain
- GTK4 ≥ 4.22 and LibAdwaita ≥ 1.9 (dev packages)

### Native

```sh
cd Anything
cargo build --release
./target/release/anything-gui
```

### Flatpak

```sh
cd Anything
flatpak run --command=bash org.flatpak.Builder
flatpak-builder --user --install build ../flatpak/io.github.anything.yml
flatpak run io.github.anything
```

## Project layout

| Path | Description |
|------|-------------|
| `Anything/` | GTK-rs GUI binary |
| `SearchEngine/` | Search engine library (fuzzy, regex, filters) |
| `LibAnything/` | Low-level filesystem indexer (walks `/`, writes YAML) |
| `flatpak/` | Flatpak packaging files |
