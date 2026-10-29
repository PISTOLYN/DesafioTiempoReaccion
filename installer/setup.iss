[Setup]
AppName=Desafío Tiempo de Reacción
AppVersion=1.0.0
DefaultDirName={autopf}\DesafioTiempoReaccion
DefaultGroupName=Desafío Tiempo de Reacción
OutputDir=Output
OutputBaseFilename=Instalador_Desafio
Compression=lzma2
SolidCompression=yes
; Esto asegura que se instale correctamente en Windows de 64 bits
ArchitecturesInstallIn64BitMode=x64

[Files]
; Toma todos los archivos del juego generados por GitHub
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs

[Icons]
; Crea el acceso directo en el menú de inicio
Name: "{group}\Desafío Tiempo de Reacción"; Filename: "{app}\DesafioTiempoReaccion.exe"
; Crea el acceso directo en el ESCRITORIO
Name: "{autodesktop}\Desafío Tiempo de Reacción"; Filename: "{app}\DesafioTiempoReaccion.exe"
