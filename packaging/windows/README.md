# Windows Packaging

## Creating MSI Installer

### Prerequisites
- WiX Toolset: https://wixtoolset.org/
- Or install via Chocolatey: `choco install wixtoolset`

### Steps

1. Build the project (if not already built):
   ```powershell
   .\build.ps1
   ```

2. Run the MSI creation script:
   ```batch
   create-msi.bat
   ```
   
   Or manually:
   ```batch
   candle.exe -dSourceDir=..\..\dist\win-x64 Product.wxs
   light.exe -out ..\..\dist\installer\Anything-Setup.msi Product.wixobj
   ```

3. The MSI will be created at: `dist/installer/Anything-Setup.msi`

## Files
- `Product.wxs` - WiX source file defining the installer
- `build-msi.ps1` - PowerShell script to build MSI
- `create-msi.bat` - Batch script to build MSI (Windows)
- `README.md` - This file

## Distribution
The resulting `Anything-Setup.msi` can be distributed to users for easy installation.
