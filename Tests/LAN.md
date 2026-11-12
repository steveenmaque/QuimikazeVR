# Aula LAN: uso y aceptación

Implementación sin servicio externo: un celular Android actúa como servidor TCP IPv4 en el puerto 47771. Máximo 8 participantes contando al anfitrión. No necesita cuentas, Internet ni abrir puertos del router.

## Uso

1. Conectar todos los celulares al mismo Wi-Fi. También sirve el punto de acceso de un celular: los invitados se conectan a ese punto y el dueño crea el aula.
2. Abrir **Aula LAN** junto a la zona de inicio. El alias inicial es anónimo; **Cambiar alias** permite editarlo con el teclado del teléfono.
3. El anfitrión pulsa **Crear aula** y comparte una de sus IPv4 mostradas. Si hay VPN o varias interfaces, usar la correspondiente al Wi-Fi/punto de acceso, no la VPN.
4. Los invitados introducen la IPv4 con el teclado numérico por mirada y pulsan **Unirme**. Marcan **Estoy listo**.
5. Fuera del reto, el anfitrión elige el elemento. Todos entran a su laboratorio y ven a los compañeros como avatares sin colisiones. Los invitados pueden moverse y observar; la navegación entre laboratorios la dirige el anfitrión.
6. Con al menos dos participantes y todos listos, el anfitrión inicia el reto del elemento actual: tres preguntas sobre protones, categoría de la leyenda y periodo. Cada participante responde una vez por pregunta. El servidor calcula los puntos; repetir el paquete/respuesta no suma.
7. Al responder todos aparece la explicación. Si alguien no responde, el anfitrión puede **Revelar y cerrar pregunta**; las respuestas pendientes cuentan sin acierto. Solo el anfitrión avanza.
8. Al finalizar se muestran resultados individuales y del equipo. El progreso local se registra mediante LearningProgress. El anfitrión puede preparar otra ronda; los invitados deben marcar Listo nuevamente.
9. **Cerrar panel** permite investigar las viñetas. Durante el reto, B/atrás vuelve al panel del aula. Para explorar individualmente, elegir **Salir del aula**. El anfitrión dispone de **Cerrar aula para todos**.

## Diseño y límites

- El anfitrión controla lobby, escena, elemento, preguntas y puntos. No hay migración de servidor: si sale, se suspende o pierde conexión, hay que crear otra aula.
- Las poses se publican a 5 Hz y se agrupan en snapshots de hasta 5 Hz; los avatares se interpolan localmente y no tienen colliders. El servidor verifica identidad, contexto, números finitos, límites espaciales y frecuencia.
- Los sockets leen/escriben en hilos de fondo. Colas de escritura limitadas a 32 tramas por conexión; trama máxima 8192 bytes. Los eventos se aplican en el hilo Unity.
- Conexión inicial: límite 4 segundos. Sin tráfico entrante: desconexión tras 15 segundos. Heartbeat cada 2 segundos. Al suspender la app se cierra su sesión deliberadamente; reanudar requiere conectar de nuevo.
- No se admiten nuevos integrantes durante una ronda. Aula llena/ronda iniciada/IPv4 incorrecta/puerto ocupado generan mensajes diferenciados.
- Los alias y puntos viajan por TCP local sin cifrado. Usar una red de confianza y alias; no introducir datos personales. No hay descubrimiento automático ni conexiones por Internet.
- Una red escolar puede impedir conexiones entre clientes (aislamiento Wi-Fi). TI debe permitir comunicación local TCP 47771. El punto de acceso del anfitrión sirve de alternativa cuando la política del centro lo permite.
- La prueba automatizada de localhost verifica protocolo y sockets; no demuestra compatibilidad de radios Wi-Fi, hotspot, mandos, temperatura ni rendimiento de un celular real.

## Pruebas automáticas

En Unity: **Quimikaze → Validar aula LAN (localhost)** o `LabQuimicaVR.EditorTools.LanValidation.Run()`. `Checks` y `Passed` contienen el conteo y las categorías superadas. Incluye direcciones inválidas, alias, TCP fragmentado dentro de UTF-8, trama sobredimensionada, puerto ocupado, backpressure, autoridad, seguimiento de escena, poses inválidas, respuestas duplicadas, cierre docente de espera, 8 participantes, rechazo del noveno, desconexión, conexión fallida, cancelación concurrente y timeout real de 15 segundos. Debe ejecutarse en el hilo principal del editor; tarda más de 15 segundos por el timeout real.

## Checklist de aceptación en dispositivos (pendiente hasta aportar evidencia)

- [ ] Instalar el mismo APK en 2 celulares Android; crear/unir/listo/iniciar/responder/ver resumen y repetir.
- [ ] Repetir con 3, 4 y 8 celulares; comprobar rechazo explícito del noveno.
- [ ] Repetir con Wi-Fi institucional y hotspot del anfitrión, con datos móviles desactivados.
- [ ] Verificar IPv4 mostrada en el anfitrión; probar VPN activada/desactivada y varias interfaces.
- [ ] Recorrer tabla → elemento A → elemento B → tabla desde el anfitrión; todos siguen una sola transición y ven los mismos avatares/contexto.
- [ ] Intentar cambiar de elemento desde un invitado: se muestra instrucción y se evita la transición incorrecta.
- [ ] Caminar con todos los jugadores: sin colisiones entre avatares, sin movimientos de cámara inducidos por la red y sin superposición inicial de cabezas.
- [ ] Verificar aislamiento del foco y movimiento al leer el panel; B/atrás cierra/abre el panel sin abandonar accidentalmente la ronda.
- [ ] Dejar un jugador sin responder: el anfitrión revela/cierra la pregunta y puede completar la sesión.
- [ ] Desactivar Wi-Fi de un invitado durante la ronda: desaparece tras timeout y los demás continúan.
- [ ] Bloquear/suspender un invitado y reabrir: informa desconexión y permite volver al lobby siguiente.
- [ ] Bloquear/suspender/cerrar al anfitrión: todos reciben desconexión; crear otra aula y reconectar sin reiniciar la app.
- [ ] IPv4 errónea, puerto ocupado y red con aislamiento: mensaje comprensible y recuperación mediante nueva conexión.
- [ ] Mantener sesión 30 minutos con 8 jugadores; medir FPS, memoria, temperatura, latencia y batería del anfitrión. No declarar apto para venta sin umbrales acordados y evidencia real.
- [ ] Verificar visor, teclado por mirada, alias y dirección IP en ambos giros horizontales del teléfono.
- [ ] Comprobar que ronda completada registra progreso una vez; repetir la misma puntuación no permite acumular puntos de aprendizaje artificialmente.

Cada caso requiere fecha, modelo/Android, versión/identificador de APK, red usada, resultado y captura/video/log. Las casillas se mantienen pendientes hasta ejecutarlas físicamente.
