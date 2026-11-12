# Quimikaze v1.2 — Aula educativa y multijugador LAN

Tercera versión, posterior a v1.0 y v1.1. Los cambios están integrados en `main`. APK nuevo de 52 270 925 bytes, compilado con Unity 6000.6.0f1 e IL2CPP ARMv7/ARM64; versión 1.2, código 3, Android mínimo 8.0 y objetivo API 36.

**Cambio de firma autorizado:** la clave de v1.1 se perdió al reinstalar el equipo. No puede instalarse v1.2 como actualización directa de v1.1: requiere reinstalar una vez y puede perder datos locales. Se respaldó privadamente la clave actual y se fijó su certificado como base de las próximas actualizaciones. No se incluye ninguna clave en Git.

- Seis rutas educativas, retos para los 118 elementos y cuatro variantes por dominio, con explicaciones, reintentos, puntos por mejora y progreso local persistente.
- Aula LAN de 2 a 8 participantes: anfitrión Android, lobby, estados listo, avatares, exploración compartida y preguntas dirigidas por el anfitrión.
- Corrección del sentido vertical de la cámara y ajuste persistente mediante R3 o el botón Eje Y.
- Distinción entre categorías y grupos, bismuto sin isótopos estables, modelos ilustrativos y predicciones de reacciones identificadas como tales.
- Biblioteca científica con artículos y DOI; informe docente CSV con posibilidad de compartir desde Android al finalizar el aula LAN.
- Pruebas automáticas de datos, aprendizaje, controles, protocolo TCP, límites de mensajes y ocho participantes. Se conserva una checklist separada para inspección visual y pruebas físicas pendientes.

## Verificación de esta preparación

La suite de lógica y TCP pasó 51 592 comprobaciones; las funciones de rutas de informes pasaron 13 comprobaciones en JVM. El módulo Android de informes también se ensambló con Gradle 9.1 / AGP 9.0.0 como AAR.

Unity 6000.6.0f1 construyó las 118 vistas con cero textos desbordados. La prueba real en Play pasó 76 comprobaciones distintas: persistencia nativa, modelos, retos, tres sesiones TCP reales, tres preguntas compartidas y CSV. Se corrigieron encabezados, controles bajo el suelo y avatares que tapaban el panel. El bloqueo anterior de Windows fue resuelto por el usuario y ya no impide ejecutar el motor.

El APK pasó 11 comprobaciones de identidad, versión, API, arquitecturas, permisos, proveedor de informes y firma de la nueva línea. La incompatibilidad con v1.1 figura expresamente en `Tests/Evidence/apk-v1.2.json`. La revisión editorial individual de las reseñas y las pruebas Android/visor/mando siguen indicadas en `VALIDACION_FINAL.md`.
