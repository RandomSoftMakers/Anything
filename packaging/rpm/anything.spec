%bcond_without prebuilt

Name:           anything
Version:        1.0.0
Release:        1%{?dist}
Summary:        Lightning fast local file search

License:        GPL-3.0
URL:            https://github.com/RandomSoftMakers/Anything
Source0:        anything-%{version}.tar.gz

%if %{without prebuilt}
BuildRequires:  dotnet-sdk-10.0
Requires:       dotnet-runtime-10.0
%endif

%description
A minimalist, cross-platform file search tool for Windows, Linux, and macOS.
Features instant search results with a clean interface.

%prep
%setup -q

%if %{with prebuilt}
%build
# Binary is pre-built by CI; nothing to build.

%install
mkdir -p %{buildroot}/usr/bin
mkdir -p %{buildroot}/usr/share/applications
mkdir -p %{buildroot}/usr/share/icons/hicolor/256x256/apps

cp %{_builddir}/anything-%{version}/Anything.UI.Avalonia %{buildroot}/usr/bin/anything
chmod +x %{buildroot}/usr/bin/anything

cp %{_builddir}/anything-%{version}/icon.png \
   %{buildroot}/usr/share/icons/hicolor/256x256/apps/anything.png

cat > %{buildroot}/usr/share/applications/anything.desktop << 'DESKTOP'
[Desktop Entry]
Name=Anything
Comment=Lightning fast local file search
Exec=/usr/bin/anything
Icon=anything
Terminal=false
Type=Application
Categories=Utility;Search;
DESKTOP

%else
%build
dotnet publish Anything.UI.Avalonia/Anything.UI.Avalonia.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    -o %{buildroot}/usr/share/anything

%install
mkdir -p %{buildroot}/usr/bin
mkdir -p %{buildroot}/usr/share/applications
mkdir -p %{buildroot}/usr/share/icons/hicolor/256x256/apps

cp %{_builddir}/anything-%{version}/icon.png \
   %{buildroot}/usr/share/icons/hicolor/256x256/apps/anything.png 2>/dev/null || true

cat > %{buildroot}/usr/bin/anything << 'SCRIPT'
#!/bin/bash
dotnet /usr/share/anything/Anything.UI.Avalonia.dll "$@"
SCRIPT
chmod +x %{buildroot}/usr/bin/anything

cat > %{buildroot}/usr/share/applications/anything.desktop << 'DESKTOP'
[Desktop Entry]
Name=Anything
Comment=Lightning fast local file search
Exec=/usr/bin/anything
Icon=anything
Terminal=false
Type=Application
Categories=Utility;Search;
DESKTOP
%endif

%files
/usr/bin/anything
/usr/share/applications/anything.desktop
/usr/share/icons/hicolor/256x256/apps/anything.png
%if %{without prebuilt}
/usr/share/anything/
%endif

%changelog
* Tue May 5 2026 Anything Team <contact@anything.app> - 1.0.0-1
- Initial package release
