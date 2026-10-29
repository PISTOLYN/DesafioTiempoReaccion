@echo off
REM Genera el juego listo para instalar (requiere .NET 8 SDK)
dotnet publish -c Release -r win-x64 --self-contained true -o publish\win-x64
dotnet publish -c Release -r win-x86 --self-contained true -o publish\win-x86
echo.
echo Listo. Carpetas: publish\win-x64 (Win10/11 64 bits) y publish\win-x86 (Win10 32 bits)
pause
