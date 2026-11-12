# Informes CSV: compartir en Android

El botón **Informe** guarda el CSV y solicita el selector del sistema para compartirlo. En PC solicita abrir la carpeta. Cancelar conserva el archivo; no se afirma que otra aplicación lo haya enviado.

Mientras hay una sesión LAN conectada, guarda el archivo y explica que se debe cerrar el aula antes de compartir. Así el selector de otra app no suspende accidentalmente al anfitrión durante la clase.

El proveedor propio expone únicamente `content://${applicationId}.reports/reports/Quimikaze_AAAAMMDD_HHMMSS_mmm.csv`, de solo lectura, bajo `getExternalFilesDir(null)/Informes`. Valida nombre, URI y rutas canónicas. `exported=false`, `grantUriPermissions=true`, `ClipData` y permiso temporal solo de lectura. No añade permisos de almacenamiento ni cuentas. El usuario elige el destino.

Se usa `.androidlib` moderno con `build.gradle`, namespace, manifiesto en la raíz y fuentes Java `src/main/java`: convenciones contrastadas con los plugins oficiales instalados `UnityFoldingFeature.androidlib` y `com.unity3d.accessibility.androidlib` de Unity 6000.6.0f1. El `sourceSets` declara explícitamente el manifiesto raíz. No depende del formato Ant antiguo `project.properties`.

Fuentes oficiales consultadas antes de implementar:

- [Android: compartir archivos y permisos temporales](https://developer.android.com/training/secure-file-sharing/share-file).
- [Android: ContentProvider y openFile](https://developer.android.com/reference/android/content/ContentProvider).
- [Android: Intent ACTION_SEND, EXTRA_STREAM y chooser](https://developer.android.com/reference/android/content/Intent).
- [Android: OpenableColumns](https://developer.android.com/reference/android/provider/OpenableColumns).
- [Unity: importar proyectos de biblioteca Android](https://docs.unity3d.com/Manual/android-library-project-import.html). La web redirigió y no fue legible en la herramienta; se contrastó el ejemplo oficial local instalado arriba.

Validación ejecutable: compilar los tres archivos Java Android contra `SDK/platforms/android-34/android.jar`; compilar y ejecutar `ReportsPathPolicyTest.java` con `ReportsPathPolicy.java` en JVM. Esta prueba comprueba ruta válida, recorrido de directorios, nombres/extensiones ajenos, archivo ausente y almacenamiento nulo. No verifica resolución real de URI ni permisos del sistema Android.

Resultado ejecutado: `javac` del OpenJDK incluido con Unity compiló los tres archivos contra Android API 34. La JVM ejecutó **13 comprobaciones, todas correctas**. La advertencia de `javac` indica que `-source 8/-target 8` se utilizó sin bootstrap Java 8; no hubo errores. Evidencia local: `AutoSync/reports_java_results.json`.

Pendiente obligatorio antes de entrega: compilar APK/manifest fusionado; en Android abrir Compartir, cancelar, enviar CSV a una aplicación de archivos y leerlo completo; confirmar que URI sin permiso, escritura, URI ajena y recorrido codificado son rechazados; comprobar Android 8 y versión actual. No se ejecutó un dispositivo ni emulador Android durante esta implementación.
