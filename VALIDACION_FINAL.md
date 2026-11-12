# Lista de aceptación de la entrega de aula

Estado: **v1.3 compilada y verificada en Editor, Play/TCP e inspección del APK; pendiente de prueba física en Android y pilotaje escolar antes de distribución institucional**. Fecha local: 12/11/2026. Un casillero sin marcar significa pendiente, no aprobado por inferencia.

## Aceptación de las correcciones v1.3

Cada punto de esta sección empieza pendiente. Los resultados históricos de v1.2 no prueban las nuevas pantallas ni el APK v1.3.

- [ ] Inicio desde `MenuPrincipal`, antes del laboratorio; menú legible en ambos ojos y accesible con mirada y botón del visor o A, sin tocar la pantalla. *(Verificado en Unity Play: arranque en `MenuPrincipal`, `6` raycasts físicos desde `Head`, conmutación estéreo guardada y PNG monoculares sin desborde; óptica estereoscópica real y pulsación física en celular pendientes).*
- [x] Rutas y misiones fijadas a una pared real, por encima del suelo, sin seguir la cámara ni tapar las viñetas existentes. *(Verificado en Unity Play e inspección visual: `TU RUTA DE QUÍMICA` fijo en pared junto a `GRUPOS Y SUBNIVELES`, `03_misiones.png` y `15_informe_progreso.png`).*
- [x] Placa discreta **Para docentes** en la pared; citas y DOI ocultos inicialmente. Al abrirla, el contenido sigue en la pared y puede cerrarse con mirada o Atrás. *(Verificado en Unity Play con las `26` páginas de `ScientificLibrary` incluyendo `P05` y `P06` más límites del modelo; `01_fuentes.png`, `01a_*`, `01b_*`).*
- [x] Tutorial accesible desde el menú, con pasos claros y práctica de interacción; terminarlo, repetirlo y volver al menú no modifica el progreso de química. *(Verificado en Unity Play: `4` pasos, flujo `Uso solo el visor` con botón deshabilitado visualmente atenuado en `00e_tutorial_sin_mando.png` y conservación intacta del progreso).*
- [x] Opciones VR interactivas: recenter, eje Y/R3, orientación y ajuste de lentes; comprobar persistencia al cerrar y abrir. *(Verificado en Unity Play e inspección de `00b_opciones.png`; la prueba en mando Bluetooth/celular real sigue pendiente en la sección física).*
- [x] Personajes diferenciados por apariencia, con vista previa y selección persistente; el cambio no altera puntos, habilidades ni respuestas. *(Verificado en `00c_personaje_0..3.png` y `11_lan_avatares_tabla.png`: `4` trajes con pedestal encuadrado y selección persistente).*
- [x] Crear y unirse al aula desde el menú, antes de entrar al laboratorio; IP y parámetros técnicos concentrados allí. *(Verificado en Unity Play y `02_lan_menu.png` / `10_lan_lobby_listo.png`; `AccesoAulaLAN == null` en laboratorio).*
- [x] Sesión TCP real: anfitrión y dos invitados reflejan personaje, alias y estado Listo; al comenzar entran al mismo contexto sin perder conexiones. *(Verificado en Play TCP loopback con `3` participantes y `3` preguntas en mural `AulaQuizMural` sin obstrucciones: `10_lan_lobby_listo.png`..`14_lan_resultado.png`; Wi-Fi entre celulares pendiente).*
- [x] Probar los cuatro personajes entre jugadores, reconexión y cambio de escena; valores de personaje inválidos no bloquean el servidor ni crean objetos fuera de catálogo. *(Verificado en `Tests/Evidence/lan-v1.3.json` y `Tests/Evidence/runtime-v1.3.json`).*
- [x] Probar 8 participantes, rechazo del noveno y rechazo de protocolo incompatible. Invitados no controlan navegación ni respuestas de otros. *(Verificado en `129` comprobaciones de `LanValidation`: `Tests/Evidence/lan-v1.3.json`).*
- [x] Atrás cierra solo el panel activo o vuelve al nivel correcto; no activa dos acciones ni cierra el aula accidentalmente. *(Verificado en Unity Play).*
- [x] Reejecutar compilación C#, validación de las 118 vistas y Play; cero errores críticos y textos sin desbordes en menú, tutorial, opciones, personajes, lobby y paredes. *(`dotnet build` Editor+Android 0/0; `LogicHarness` 51 635/51 635; `LabValidation` 118 vistas, `0` desbordes; `LabRuntimeValidation` 748 checks / 38 093 aserciones, `0` errores; informes en `Tests/Evidence/*-v1.3.json`).*
- [x] Capturas reales del menú, tutorial, opciones, personajes, misiones en pared, fuentes cerradas/abiertas, lobby y avatares sincronizados. *(Inspeccionadas capturas `AutoSync/capturas/*.jpg` y `AutoSync/validacion/runtime_20261112_185405_424/*.png`).*
- [x] APK nuevo 1.3/código 4, mismo paquete y certificado de v1.2, API mínima 26/objetivo 36, INTERNET, proveedor de informes y ARMv7/ARM64. Guardar `Tests/Evidence/apk-v1.3.json`. *(Verificado con `aapt2` y `apksigner`: `11/11` checks, SHA-256 `7D5AA7A53E5DEC2C0567429237E67CDB93856FB17B30A3218808E4CCEED8B538`, informe en `Tests/Evidence/apk-v1.3.json` y `Tests/Evidence/build-v1.3.json`).*
- [ ] Instalar sobre v1.2 en un celular con preferencias/progreso existentes: Android ofrece actualizar y los datos se conservan. La inspección de firma no sustituye esta prueba. *(Sin dispositivo Android conectado por ADB en este entorno).*
- [x] Notas de la release y entrada del README con el formato de v1.1: instalación, requisitos y novedades breves; investigación y resultados técnicos en sus archivos de referencia. *(`NOVEDADES_v1.3.md` y `README.md` alineados).*
- [ ] Push en `main`, tag v1.3, APK y evidencias publicados; descarga pública idéntica al binario verificado y archivos ZIP sin claves privadas. *(Commit en `main` preparado tras estas pruebas; publicación remota y comprobación de descarga pendientes al cierre).*

## Evidencia histórica de v1.2

- [x] Pull con avance rápido hasta `5018b29`; cambios locales previos conservados y respaldo previo en la carpeta temporal del usuario.
- [x] Todos los scripts de producción y editor compilados contra las bibliotecas instaladas de Unity 6000.6.0f1 mediante `Tests/UnityCompilation/UnityCompilation.csproj`: 0 errores y 0 advertencias. Esto no ejecuta Unity ni genera APK.
- [x] Harness C# con fuentes de producción: **51 592 comprobaciones, 0 fallos** (16 228 educativas, 35 278 de contenido/controles, 86 de LAN con sockets TCP localhost). Informe reproducible: `AutoSync/logic_results.json`; copia de evidencia en `Tests/Evidence`; detalles del alcance en `Tests/LogicHarness/README.md`.
- [x] Exportación Android: tres fuentes Java compiladas contra API 34, manifiesto XML validado y 13 comprobaciones de rutas ejecutadas en JVM. No equivale a resolver URI ni compartir en Android real.
- [x] Revisión cruzada de LAN y corrección de escrituras bloqueantes, pregunta bloqueada por ausencia de respuesta, navegación de invitados y predicciones químicas asimétricas.
- [x] Unity 6000.6.0f1 ejecutó las pruebas con su serializador nativo. El bloqueo anterior de Windows fue resuelto por el usuario; la validación terminó con éxito. Informe: `Tests/Evidence/unity-v1.2.json`.
- [x] Las 118 vistas se construyeron y se comprobaron con TMP: cero textos desbordados. Se revisaron capturas de las estaciones y se corrigieron encabezados, espaciado y ubicación del tablero. Capturas en el paquete de evidencias de v1.2.
- [x] Flujo real en Play: transiciones, modelos, guardado/restauración nativa, repetición sin farming, CSV y anfitrión GUI con dos clientes TCP reales; tres preguntas compartidas y cierre limpio. Informe: `Tests/Evidence/runtime-v1.2.json`. No equivale a Wi-Fi entre celulares.
- [x] APK IL2CPP ARMv7 + ARM64 generado e inspeccionado: 11 comprobaciones de identidad, versiones, API mínima 26, objetivo 36, INTERNET, proveedor de informes y firma válida de la nueva línea v1.2. Informe: `Tests/Evidence/apk-v1.2.json`.
- [x] Detectada firma distinta a v1.1. Su clave original no está disponible; el propietario autorizó continuar con una nueva base v1.2. La necesidad de reinstalar y la posible pérdida de datos locales están indicadas en README y release. Clave actual respaldada privadamente y certificado fijado para las próximas versiones.
- [ ] Instalar y abrir v1.2 en un celular; después probar actualización v1.2 → siguiente versión con la misma firma y preferencias existentes. El usuario eligió continuar con la inspección del APK sin conectar un dispositivo.

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
dotnet run --project Tests/LogicHarness/LogicHarness.csproj -- --output AutoSync/logic_results.json
```

Con Unity ejecutable en el sistema, usar la versión fijada del proyecto:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe' -batchmode -projectPath $PWD.Path -executeMethod LabQuimicaVR.EditorTools.LabValidation.RunBatch -logFile AutoSync/validation.log
& 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe' -batchmode -projectPath $PWD.Path -executeMethod LabQuimicaVR.EditorTools.LabRuntimeValidation.RunBatch -logFile AutoSync/runtime.log
& 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe' -batchmode -projectPath $PWD.Path -buildTarget Android -executeMethod LabQuimicaVR.EditorTools.LabBuild.BuildApkBatch -logFile AutoSync/apk.log
```

Cerrar cada instancia de Unity antes del siguiente comando. No interpretar el arranque asíncrono de un proceso como compilación terminada: comprobar código de salida y archivos de resultado.
