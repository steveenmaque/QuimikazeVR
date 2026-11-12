# Lista de aceptación de la entrega de aula

Estado: **candidato en desarrollo; no aprobado todavía para distribución escolar**. Fecha local: 11/11/2026. Un casillero sin marcar significa pendiente, no aprobado por inferencia.

## Evidencia ejecutada

- [x] Pull con avance rápido hasta `5018b29`; cambios locales previos conservados y respaldo previo en la carpeta temporal del usuario.
- [x] Todos los scripts de producción y editor compilados contra las bibliotecas instaladas de Unity 6000.6.0f1 mediante `Tests/UnityCompilation/UnityCompilation.csproj`: 0 errores y 0 advertencias. Esto no ejecuta Unity ni genera APK.
- [x] Harness C# con fuentes de producción: **51 592 comprobaciones, 0 fallos** (16 228 educativas, 35 278 de contenido/controles, 86 de LAN con sockets TCP localhost). Informe reproducible: `AutoSync/logic_results.json`; copia de evidencia en `Tests/Evidence`; detalles del alcance en `Tests/LogicHarness/README.md`.
- [x] Exportación Android: tres fuentes Java compiladas contra API 34, manifiesto XML validado y 13 comprobaciones de rutas ejecutadas en JVM. No equivale a resolver URI ni compartir en Android real.
- [x] Revisión cruzada de LAN y corrección de escrituras bloqueantes, pregunta bloqueada por ausencia de respuesta, navegación de invitados y predicciones químicas asimétricas.
- [ ] Validación con el motor Unity y su serializador nativo. El intento pasó la licencia tras iniciar sesión; Windows bloqueó `Bee.TundraBackend.dll` con `0x800711C7`. Evidencia: `AutoSync/compile-retry.log`. No se cambiaron políticas de seguridad.
- [ ] Construcción de escenas, inspección visual y capturas: `Quimikaze → Validar entrega completa` o `LabValidation.RunBatch`.
- [ ] Prueba en Play: `LabRuntimeValidation.RunBatch` (sin `-quit`); resultado y capturas bajo `AutoSync/validacion`.
- [ ] APK IL2CPP ARMv7 + ARM64 generado, instalado y abierto correctamente; verificar permiso Android `INTERNET` en el manifiesto final.

## Revisión de cada viñeta

| Viñeta | Comprobación de aceptación | Respaldo / evidencia requerida |
| --- | --- | --- |
| Tabla periódica | 118 casillas sin superposición; colores acompañados de código; mirada, ficha, categorías y entrada funcionan | Datos IUPAC [D01], comprobaciones automáticas y captura frontal |
| Bloques, grupos y periodos | Diferenciar fila, columna y subnivel; no aplicar reglas simplificadas a todas las configuraciones | IUPAC/NIST [D01/D02], capturas laterales |
| Historia y cronología | Texto completo legible y cada fecha/atribución contrastada individualmente; diferenciar descubrimiento, síntesis y nombre | Revisión editorial pendiente para las 118 reseñas; no basta bibliografía general |
| Propiedades | Valores con unidades; estado desconocido explícito; neutrones ilustrativos y pesos promedio diferenciados | Q01/Q02/D02, pruebas estructurales y revisión docente |
| Bohr, nube y núcleo | Tres modos con transición, sin objetos sobrantes; aviso de modelo sin escala y nube ilustrativa | D02 y revisión visual por elemento |
| Reacciones | Conservar átomos, simetría A+B/B+A, distinguir ejemplo de base y propuesta formal; no inferir explosión por electronegatividad | Q03, 7021 parejas probadas; fuentes por ejemplo experimental todavía pendientes |
| Vitrina y fotografía | 118 representaciones o alternativa explícita, créditos, sin atribuir muestras físicas a superpesados | Créditos existentes; inspección individual de formas y fotos pendiente |
| Descubridores | Retratos declarados cargan; textos, autorías y fechas contrastados; no texto cortado | Pruebas de recursos + revisión editorial individual pendiente |
| Naturaleza y paisaje | Sin confundir paisaje ilustrativo, mineral y elemento libre; verificar las localizaciones del Perú | Créditos y revisión científica individual pendiente |
| Retos | Tres preguntas, opciones distintas, explicación correcta, errores registrados, repetir no acumula puntos sin mejorar | P01/P02 y pruebas del catálogo; revisión docente del lenguaje pendiente |
| Rutas y resultados | Seis rutas con objetivo visible, desbloqueo consistente y CSV de 118 filas sin nombres | P03/P04, pruebas del progreso; umbral 2/3 propuesto, pendiente de validación docente |
| Fuentes | Biblioteca navegable sin Internet, DOI visible y límites expresos | `FUENTES_CIENTIFICAS.md`; comprobar lectura en visor |

## Prueba física de controles y comodidad

- [ ] Joystick derecho: arriba mira arriba y abajo mira abajo en Android; repetir con mando Windows.
- [ ] R3 alterna una sola vez por pulsación; inversión persiste tras cerrar/reabrir y puede cambiarse con botón de mirada.
- [ ] Probar mando real de Steveen y un segundo modelo: los mandos genéricos pueden mapear R3 en otro botón.
- [ ] Zona muerta evita deriva; mantener joystick no atraviesa paredes; giroscopio y mando no invierten la inclinación.
- [ ] A selecciona; toque largo recentra sin pulsar; B cierra panel y conserva sesión; L1/R1 ajustan lentes.
- [ ] Visor en ambas orientaciones, recenter, modo mono, ausencia de giroscopio y texto de todas las viñetas a distancia real.
- [ ] Sesión de 20 minutos en dos gamas Android: registrar FPS, temperatura, batería, mareo y cortes; permitir salir inmediatamente ante molestias.

## Prueba LAN en aula

- [ ] Dos Android en mismo Wi-Fi: crear aula en celular anfitrión, introducir IPv4 mediante mirada, marcar listos e iniciar.
- [ ] Repetir con el punto de acceso del anfitrión y datos móviles desconectados; no depender de Internet.
- [ ] Probar 2, 4 y 8 jugadores: avatares, alias, posiciones, mismo elemento y respuestas/puntos coherentes.
- [ ] Noveno jugador rechazado con motivo claro; una versión de protocolo diferente no entra.
- [ ] Solo anfitrión cambia elemento, inicia, revela y avanza; invitado no provoca transiciones repetidas.
- [ ] Respuesta duplicada o tardía no suma; alumno que no responde no impide continuar al docente.
- [ ] IP inválida, IP no disponible, puerto ocupado, pérdida Wi-Fi y aislamiento de clientes: mensaje visible y posibilidad de volver a intentar.
- [ ] Desconectar invitado libera plaza; apagar o suspender anfitrión informa cierre y conserva progreso local.
- [ ] Reiniciar lobby tras desconexión y repetir 10 ciclos sin sockets, hilos ni avatares sobrantes.
- [ ] Confirmar que la red escolar permite tráfico entre dispositivos y que el puerto TCP 47771 está permitido. No requiere redirección de puertos del router.

## Aprobación para una institución

- [ ] Docente de química revisa las afirmaciones y las preguntas; registrar responsable, fecha y observaciones resueltas.
- [ ] Pilotaje con población objetivo: objetivos, pretest/postest equivalentes, medición diferida y usabilidad. No prometer mejoras porcentuales sin resultados propios.
- [ ] Revisar licencias de imágenes/fuentes y atribuciones para el uso comercial concreto.
- [ ] Docente puede recuperar y comprender el CSV desde Android: terminar aula LAN, pulsar Informe y compartir por el selector del sistema; cancelar debe conservar la copia local.
- [ ] Cero bloqueos críticos pendientes en dispositivos objetivo; todas las casillas anteriores cuentan con evidencia y responsable.

## Reproducción local

```powershell
dotnet build Tests/UnityCompilation/UnityCompilation.csproj --nologo
pwsh -NoProfile -File Tests/LogicHarness/Run-InProcess.ps1
```

Con Unity ejecutable en el sistema, usar la versión fijada del proyecto:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe' -batchmode -projectPath $PWD.Path -executeMethod LabQuimicaVR.EditorTools.LabValidation.RunBatch -logFile AutoSync/validation.log
& 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe' -batchmode -projectPath $PWD.Path -executeMethod LabQuimicaVR.EditorTools.LabRuntimeValidation.RunBatch -logFile AutoSync/runtime.log
& 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe' -batchmode -projectPath $PWD.Path -buildTarget Android -executeMethod LabQuimicaVR.EditorTools.LabBuild.BuildApkBatch -logFile AutoSync/apk.log
```

Cerrar cada instancia de Unity antes del siguiente comando. No interpretar el arranque asíncrono de un proceso como compilación terminada: comprobar código de salida y archivos de resultado.
