# Quimikaze v1.2 — Aula educativa y multijugador LAN

Tercera versión prevista, posterior a v1.0 y v1.1. Los cambios de código están integrados en `main`. La publicación del APK queda pendiente de compilar y verificar el binario nuevo; no se reutiliza el APK de v1.1.

- Seis rutas educativas, retos para los 118 elementos y cuatro variantes por dominio, con explicaciones, reintentos, puntos por mejora y progreso local persistente.
- Aula LAN de 2 a 8 participantes: anfitrión Android, lobby, estados listo, avatares, exploración compartida y preguntas dirigidas por el anfitrión.
- Corrección del sentido vertical de la cámara y ajuste persistente mediante R3 o el botón Eje Y.
- Distinción entre categorías y grupos, bismuto sin isótopos estables, modelos ilustrativos y predicciones de reacciones identificadas como tales.
- Biblioteca científica con artículos y DOI; informe docente CSV con posibilidad de compartir desde Android al finalizar el aula LAN.
- Pruebas automáticas de datos, aprendizaje, controles, protocolo TCP, límites de mensajes y ocho participantes. Se conserva una checklist separada para inspección visual y pruebas físicas pendientes.

## Verificación de esta preparación

La suite de lógica y TCP pasó 51 592 comprobaciones; las funciones de rutas de informes pasaron 13 comprobaciones en JVM. El módulo Android de informes también se ensambló con Gradle 9.1 / AGP 9.0.0 como AAR. Estas comprobaciones no equivalen a construir ni ejecutar el APK completo.

El editor local está bloqueado por Smart App Control: `Bee.TundraBackend.dll` no puede cargarse y el compilador de shaders no puede cargar `spirv-cross-c-shared.dll`. No se cambiaron las políticas de Windows. Se guardaron capturas y registros de los errores reales en `AutoSync/Evidencias_v1.2`.

Para publicar, verificar paquete, versión 1.2, código 3, firma, permiso INTERNET y presencia del proveedor de informes en el APK nuevo. La revisión editorial individual de las reseñas y las pruebas Android/visor/mando siguen indicadas en `VALIDACION_FINAL.md`.
