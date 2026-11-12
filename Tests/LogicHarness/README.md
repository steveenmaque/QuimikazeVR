# Pruebas de lógica portables (.NET 10)

Este ejecutable enlaza directamente las fuentes C# de producción y los validadores de `Assets/Editor`. No copia los algoritmos del juego. No necesita paquetes NuGet ni una licencia de Unity.

Desde la raíz del repositorio:

```powershell
dotnet run --project Tests/LogicHarness/LogicHarness.csproj -- --suite all
```

El resultado JSON se guarda en `AutoSync/logic_results.json`; el proceso devuelve 1 si alguna suite falla. Se puede seleccionar `education`, `content` o `lan`, y elegir otro archivo mediante `--output RUTA`.

También puede ejecutarse dentro de PowerShell 7 sobre .NET 10, compilando directamente las mismas fuentes mediante `Add-Type` en memoria, sin cargar los binarios generados:

```powershell
& ./Tests/LogicHarness/Run-InProcess.ps1 -Suite all
```

Este flujo no modifica la política de ejecución ni Control de aplicaciones. Si una directiva del equipo impide la compilación dinámica, debe resolverse mediante el administrador correspondiente.

Se ejercitan los datos TXT reales, retos, progresión, cálculo de puntuación, recuperación de registros, CSV, funciones puras de ejes y conexiones TCP reales sobre localhost. `UnityAdapters.cs` proporciona únicamente los tipos de Unity requeridos para compilarlos fuera del editor. Las llamadas de entrada física lanzan una excepción en vez de fingir un mando.

El adaptador de recursos de imagen comprueba archivo y firma PNG/JPEG; no importa ni renderiza texturas. JSON usa System.Text.Json y PlayerPrefs permanece en memoria. Estas pruebas **no equivalen** a ejecutar Unity, compilar IL2CPP, validar el APK, probar el visor/mando ni jugar entre celulares por Wi-Fi. Sus resultados tampoco verifican científicamente cada texto del catálogo. El informe enumera estas limitaciones y hashes SHA-256 de las fuentes ejecutadas.
