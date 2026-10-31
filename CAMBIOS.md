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

## Modo 2: Tiempo Objetivo (nuevo)
- Cambia de modo con TAB o con las flechas del selector "MODO DE JUEGO".
- ← → ajustan el objetivo de 5 en 5 min (05:00 a 95:00). ESPACIO inicia la cuenta por voz: "3, 2, 1, ¡Ahora!".
- En "¡Ahora!" empiezan a contar Rojo y Azul. Rojo se detiene con la tecla 1, Azul con la 2 (también funcionan los botones configurados).
- Gana quien quede más cerca del objetivo (se anuncia por voz); se muestra la diferencia de cada uno. Empate = nadie suma punto.
- Música de fondo: F1 → "Música de fondo" → ELEGIR… (MP3). Suena en bucle durante la carrera.
- Usa el paquete NuGet System.Speech (voz de Windows; conviene tener una voz en español instalada).

## Actualización: objetivo con las flechas de abajo + efectos editables
- El juego arranca en modo Tiempo Objetivo. Las flechas ◀ ▶ de la caja de abajo (y ← → del teclado) cambian el número del centro: 05:00, 10:00, 15:00… de 5 en 5 min.
- Rojo (tecla 1 / botón configurado) y Azul (tecla 2 / botón configurado) intentan detenerse en ese número; gana el más cercano.
- Efectos de sonido: cuenta 3-2-1, ¡Ahora!, al detener y ganador. Traen sonido por defecto y en F1 puedes probarlos (▶), cambiarlos por tu MP3/WAV (ELEGIR…) o volver al original (DEFECTO).
- F1 también permite desactivar la voz.
- El modo Tiempo de Reacción sigue disponible con la tecla TAB.
