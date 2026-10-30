; Inno Setup 6 - compilar con ISCC.exe setup.iss (después de publicar.bat)
[Setup]
AppName=Desafío Tiempo de Reacción
AppVersion=2.0.0
DefaultDirName={autopf}\DesafioTiempoReaccion
DefaultGroupName=Desafío Tiempo de Reacción
OutputDir=Output
OutputBaseFilename=Setup_DesafioTiempoReaccion
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible

[Files]
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion; Check: Is64BitInstallMode
Source: "..\publish\win-x86\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion; Check: not Is64BitInstallMode

[Icons]
Name: "{group}\Desafío Tiempo de Reacción"; Filename: "{app}\DesafioTiempoReaccion.exe"
Name: "{autodesktop}\Desafío Tiempo de Reacción"; Filename: "{app}\DesafioTiempoReaccion.exe"

[Run]
Filename: "{app}\DesafioTiempoReaccion.exe"; Description: "Abrir el juego"; Flags: nowait postinstall skipifsilent
