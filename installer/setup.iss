[Setup]
AppName=Desafío Tiempo de Reacción
AppVersion=1.0.0
DefaultDirName={autopf}\DesafioTiempoReaccion
DefaultGroupName=Desafío Tiempo de Reacción
OutputDir=Output
OutputBaseFilename=Instalador_Desafio
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "publish\win-x64\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Desafío Tiempo de Reacción"; Filename: "{app}\DesafioTiempoReaccion.exe"
Name: "{autodesktop}\Desafío Tiempo de Reacción"; Filename: "{app}\DesafioTiempoReaccion.exe"
