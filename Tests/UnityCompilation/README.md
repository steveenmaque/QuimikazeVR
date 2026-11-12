# Compilación semántica con bibliotecas de Unity

Desde la raíz, con .NET SDK y Unity 6000.6.0f1 instalados:

```powershell
dotnet build Tests/UnityCompilation/UnityCompilation.csproj --nologo
dotnet build Tests/UnityCompilation/UnityCompilation.csproj --nologo -p:DefineConstants=UNITY_ANDROID
```

La primera configuración incluye el código del editor. La segunda también compila las ramas Android/JNI sin `UNITY_EDITOR`. Se puede configurar otra ruta de instalación mediante `-p:UnityEditorPath=RUTA`.

Se enlazan las bibliotecas reales de Unity y las DLL de TextMeshPro/UI importadas en `Library/ScriptAssemblies`. Hace falta que Unity haya importado previamente esos paquetes.

Si Unity vació esa caché y no pudo reconstruirla, el verificador informa ese requisito antes de emitir errores en cascada. Para la comprobación semántica se puede indicar una copia verificada del mismo paquete con `-p:UnityPackageAssembliesPath=RUTA`. Esto no repara la importación ni sustituye la compilación del editor.

Esto detecta errores C# y usos de API incompatibles. No ejecuta Unity, no importa escenas, no fusiona AndroidManifest, no ejecuta Gradle/IL2CPP y no produce un APK. Las salidas se conservan bajo `AutoSync`.
