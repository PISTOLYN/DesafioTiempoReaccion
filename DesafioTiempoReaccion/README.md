# Desafío Tiempo de Reacción Challenge

Juego para Windows 10 (32/64 bits) y Windows 11 (64 bits). Equipo ROJO vs equipo AZUL.

## Modos (selector en el centro o menú)
1. **Tiempo de reacción**: espera la señal verde y pulsa. Salida en falso = gana el otro.
2. **Cuenta regresiva**: detén tu cronómetro lo más cerca de 00.00 sin pasarte.
3. **Botón rápido**: más pulsaciones en 10 segundos.

## Teclas (editables)
Rojo `A` · Azul `L` · Iniciar `Espacio` · Reiniciar `R` · Puntos `Q/Z` (rojo) `P/N` (azul) · Menú `M` · Pantalla completa `F11`.
También se puede dar clic a los botones grandes.

## Menú (tecla M)
Modo, configurar teclado, sonidos (carpeta), editar textos, volumen, voz, pantalla completa, reiniciar, apagar PC, cerrar.

## Archivos editables
`%AppData%\DesafioTiempoReaccion\config.json` (textos, modos, teclas) y carpeta `sonidos` (inicio, senal, ganador, error en mp3/wav).

## Compilar
1. Instalar .NET 8 SDK. 2. Ejecutar `publicar.bat`. 3. Compilar `installer/setup.iss` con Inno Setup.
