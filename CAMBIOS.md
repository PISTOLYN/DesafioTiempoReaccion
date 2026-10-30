# Cambios

## Errores corregidos
- Los displays de tiempo de cada equipo estaban fijos en "00.00": ahora muestran el tiempo ganador.
- Escribir en el título de la configuración lanzaba excepción (binding TwoWay a una propiedad de solo lectura).
- Si había una salida en falso durante la cuenta regresiva, la ronda "resucitaba" sola. Ahora cada ronda se cancela correctamente.
- Mantener una tecla pulsada (auto-repetición) contaba como pulsaciones nuevas y daba salidas en falso.
- Con la configuración abierta, escribir A / L / R daba puntos o reiniciaba el juego.
- El motor usaba locks + Dispatcher.Invoke dentro del lock (riesgo de bloqueo). Ahora corre en el hilo de la UI con async/await.
- El tiempo límite no avisaba a la UI cuando se agotaba; ahora muestra "TIEMPO AGOTADO".
- La animación del marcador (Opacity 1 → 1.5) no hacía nada; ahora el marcador pulsa.
- Se refrescaba toda la ventana reasignando el DataContext en cada cambio; ahora se actualizan solo los controles necesarios.

## Skin nuevo
- Fondo de estadio con focos, título en 3 líneas, paneles biselados rojo/azul, marcadores "PUNTO",
  displays de 7 segmentos dibujados con vectores (sin fuentes externas), botones metálicos 3D,
  panel central con caja de señal y selector de modo.

## Novedades
- El centro cuenta el tiempo en vivo y la caja se pone verde con "¡AHORA!" + sonido.
- Configuración persistente (%AppData%\DesafioTiempoReaccion\settings.json) con captura real de teclas.
- F11 pantalla completa, F1 configuración, ESPACIO inicia ronda.
