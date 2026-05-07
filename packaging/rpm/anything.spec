Name:           anything
Version:        1.0.0
Release:        1%{?dist}
Summary:        Lightning fast local file search

License:        GPL-3.0
URL:            https://github.com/AnythingDevelopmentTeam/Anything
Source0:        anything-%{version}.tar.gz

BuildRequires:  dotnet-sdk-10.0
Requires:       dotnet-runtime-10.0

%description
A minimalist, cross-platform file search tool for Windows, Linux, and macOS.
Features instant search results with a clean interface.

%prep
%setup -q

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

cp -r %{buildroot}/usr/share/anything/* %{buildroot}/usr/share/anything/
cat > %{buildroot}/usr/bin/anything << 'EOF'
#!/bin/bash
dotnet /usr/share/anything/Anything.UI.Avalonia.dll "$@"
EOF
chmod +x %{buildroot}/usr/bin/anything

cat > %{buildroot}/usr/share/applications/anything.desktop << 'EOF'
[Desktop Entry]
Name=Anything
Comment=Lightning fast local file search
Exec=/usr/bin/anything
Icon=anything
Terminal=false
Type=Application
Categories=Utility;Search;
EOF

%files
/usr/bin/anything
/usr/share/anything/
/usr/share/applications/anything.desktop

%changelog
* Tue May 5 2026 Anything Team <contact@anything.app> - 1.0.0-1
- Initial package release
