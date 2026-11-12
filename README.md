# Quimikaze · Laboratorio virtual de química en VR

Laboratorio virtual de química para chicos de 10 a 14 años que no tienen acceso a un laboratorio real. Funciona con un **celular Android dentro de un visor tipo VR Box o Cardboard**, el botón del visor y un **mando Bluetooth genérico**.

Al abrir el juego aparece un **menú principal que puedes usar con el visor**: **Jugar**, **Tutorial**, **Aula LAN**, **Mi personaje**, **Opciones** y **Salir**.

Dentro del laboratorio, las **rutas y misiones están en la pared**. La información de investigación queda en una placa discreta **Para docentes**; solo se despliega al seleccionarla.

Al entrar a la sala encuentras la **tabla periódica completa de los 118 elementos**. Al elegir un elemento pasas a su propio nivel, con:
- su **modelo 3D** (Bohr, nube electrónica y núcleo);
- su **historia**: línea de tiempo, descubridor y datos curiosos;
- sus **propiedades**: grupo, periodo, valencias y configuración electrónica;
- una **estación de reacciones** con otros elementos;
- una **vitrina con la muestra real** del elemento: un modelo 3D hecho a semejanza de su foto real, con la foto al lado;
- los **descubridores con su retrato** y **dónde se encuentra en la naturaleza**, con datos del Perú;
- una **transición con un paisaje 360°** relacionado con el elemento: el espacio, una mina, el mar, el bosque…

> Novedades de la versión 1.3: [NOVEDADES_v1.3.md](NOVEDADES_v1.3.md).

---

## 📲 Descargar el APK

### ⬇️ [Descargar QuimikazeVR.apk (última versión)](https://github.com/steveenmaque/QuimikazeVR/releases/latest/download/QuimikazeVR.apk)

En la página de [Releases](https://github.com/steveenmaque/QuimikazeVR/releases) están todas las versiones.

### Requisitos del celular
- Android **8.0 o superior**, de 32 o 64 bits.
- **OpenGL ES 3.1 o Vulkan**, que tienen casi todos los celulares de los últimos años.
- **Giroscopio**, para que la vista siga la cabeza. Sin giroscopio la app funciona, pero con menos inmersión.
- Opcional: visor VR Box o Cardboard y un mando Bluetooth.

### Instalar en el celular
1. Abre el enlace de arriba **desde el celular**. También puedes descargar el APK en la computadora y pasarlo por WhatsApp, Google Drive o un cable USB.
2. Abre el archivo descargado desde **Descargas** o el administrador de archivos.
3. Android pedirá **permitir instalar apps desconocidas** para el navegador o el administrador de archivos. Actívalo y vuelve atrás.
4. Pulsa **Instalar**. Si aparece *Google Play Protect*, elige **Más detalles → Instalar de todos modos**. Es normal: la app no viene de Play Store.
5. Abre **Quimikaze** y empieza por el tutorial del menú. Si tienes **v1.2**, elige **Actualizar** cuando Android lo ofrezca; conserva la app instalada.

Si todavía tienes **v1.1**, su clave anterior se perdió: esa versión requiere reinstalar una vez y la desinstalación puede borrar datos locales.

### Conservar las próximas actualizaciones

La firma compartida por v1.2 y v1.3 queda fijada en `Tests/Test-ApkRelease.ps1`. Conservar un respaldo privado de `.android/debug.keystore` fuera del repositorio y en un almacenamiento externo antes de reinstalar el equipo. El archivo de claves no debe subirse a GitHub. Para verificar cada APK, ejecutar el comprobador con la versión y el código que correspondan; mantener el certificado esperado.

### Primera vez con el visor
1. Coloca el celular en el visor, en horizontal y **del lado que quieras**: con los botones de volumen hacia arriba o hacia abajo. La pantalla gira sola, también si el celular tiene el bloqueo de rotación activado.
2. Mira al frente unos segundos: la vista se centra sola.
3. Si se ve doble, ajusta la separación de las lentes con **L1 / R1** del mando.
4. Si la vista queda torcida, **mantén pulsado** el botón del visor para recentrarla.
5. Desde el menú, abre **Tutorial**, elige **Mi personaje** y revisa **Opciones** antes de entrar al laboratorio. Todo se selecciona con la mirada y el botón del visor o **A**.
6. Para **transmitir la pantalla** (Meet, Zoom…), comparte la pantalla del celular: la imagen sale derecha, sin importar de qué lado esté el celular.

### Jugar con tus compañeros
1. Conecta los celulares al **mismo Wi-Fi o punto de acceso**.
2. En el menú principal, abre **Mi personaje** para elegir tu apariencia y luego **Aula LAN**.
3. Una persona crea el aula; las demás introducen la IPv4 del anfitrión y se unen.
4. Marca **Listo**. Con dos o más participantes listos, el anfitrión pulsa **Entrar al laboratorio**.

El menú y el tutorial se usan con la mirada y el botón del visor o A. En el tutorial puedes elegir **Uso solo el visor** si no tienes un mando para practicar el desplazamiento.

---

## 🎮 Controles

| Acción | Visor / mando | En el editor de Unity |
|---|---|---|
| Seleccionar lo que miras | Toque corto del botón del visor o **A** | Clic izquierdo, Espacio o Enter |
| Recentrar la vista | **Mantener pulsado** el botón del visor (1.5 s) o **Y** | R |
| Caminar | Joystick izquierdo o cruceta | WASD o flechas |
| Girar | Joystick derecho | Q / E |
| Mirar arriba / abajo | Eje vertical del joystick derecho | T / G o RePág / AvPág |
| Invertir el eje vertical y guardar | Pulsar **R3** o botón **Eje Y** en **Opciones** | I |
| Mirar alrededor | Mover la cabeza | Mantener clic derecho y mover el mouse |
| Volver o abrir el menú | **B** o botón atrás del celular | Escape |
| Salir de la app | Opción **Salir** del menú principal | — |
| Cambiar el modelo del átomo | **X** | M |
| Ajustar las lentes si se ve doble | **L1 / R1** (1 mm por pulsación; se guarda) | [ / ] |
| Pantalla dividida para el visor | Automática en el celular | V |
| Orientación de la pantalla | Automática (gira con el celular), o fija desde **Opciones** | O |
| Saltar la transición del paisaje | Toque del botón del visor o **A** | Clic izquierdo |

---

## 🛠️ Herramientas necesarias para colaborar

| Herramienta | Para qué | Descarga |
|---|---|---|
| **Cuenta de GitHub** | Acceder al repositorio y subir cambios | https://github.com/signup |
| **Git** | Control de versiones (clonar, commit, push, pull) | https://git-scm.com/download/win |
| **GitHub Desktop** *(opcional, recomendado si no usas la terminal)* | Hacer lo mismo que Git, pero con botones | https://desktop.github.com |
| **Unity Hub** | Instalar Unity y abrir el proyecto | https://unity.com/download |
| **Unity 6000.6.0f1** (Unity 6.6) | El motor del juego. **Tiene que ser esta versión exacta** | Desde Unity Hub → *Installs* |
| **Módulo Android Build Support** (con OpenJDK y Android SDK & NDK) | Compilar el APK | Unity Hub → *Installs* → ⚙️ → *Add modules* |
| **Visual Studio Code** o **Visual Studio Community** | Editar los scripts en C# | https://code.visualstudio.com · https://visualstudio.microsoft.com |
| **Celular Android** + visor + mando Bluetooth *(opcional)* | Probar el APK | — |

---

## 👥 Cómo colaborar paso a paso

### 1. Obtener acceso (una sola vez)
1. Crea tu cuenta en GitHub y pásale tu **nombre de usuario** al dueño del repositorio.
2. El dueño entra a **Settings → Collaborators → Add people** y te invita.
3. Acepta la invitación desde el correo o desde https://github.com/notifications.

### 2. Instalar las herramientas (una sola vez)
1. Instala **Git**. Durante la instalación puedes dejar todas las opciones por defecto.
2. Configura tu nombre y correo. Así aparecerán tus cambios:
   ```bash
   git config --global user.name "Tu Nombre"
   ```
   ```bash
   git config --global user.email "tu-correo@ejemplo.com"
   ```
3. Instala **Unity Hub**. Inicia sesión con una cuenta de Unity; la licencia *Personal* es gratis.
4. En Unity Hub ve a **Installs → Install Editor**. Busca la versión **6000.6.0f1**; si no aparece, entra a *Archive* en https://unity.com/releases/editor/archive. Marca **Android Build Support** con *OpenJDK* y *Android SDK & NDK Tools*.

### 3. Descargar el proyecto (clonar)
**Con la terminal:**
```bash
git clone https://github.com/steveenmaque/QuimikazeVR.git
```
**Con GitHub Desktop:** *File → Clone repository → URL*, pega `https://github.com/steveenmaque/QuimikazeVR.git` y elige una carpeta.

> 💡 Clona el proyecto en una ruta corta y **fuera de OneDrive o Google Drive**, por ejemplo `C:\UnityProjects\`. Unity crea una carpeta `Library/` de varios GB, y si la nube la sincroniza todo se vuelve lento y pueden dañarse archivos.

### 4. Abrir en Unity
1. En Unity Hub: **Projects → Add → Add project from disk** y elige la carpeta `QuimikazeVR`.
2. La **primera vez tarda varios minutos**, porque Unity reconstruye la carpeta `Library/`. Es normal.
3. Abre la escena `Assets/Scenes/TablaPeriodica` y pulsa **▶ Play** para probar.

### 5. Trabajar en una tarea (cada vez)
La entrega actual se mantiene en **main**, sin ramas de Pull Request.

1. **Actualiza tu copia** antes de empezar:
   ```bash
   git checkout main
   git pull --ff-only
   ```
2. Haz tus cambios en Unity o en los scripts y **pruébalos con ▶ Play**.
3. Revisa `git status` y añade únicamente los archivos de tu tarea. No incluyas claves de firma ni cambios ajenos.
4. **Guarda y sube los cambios revisados**:
   ```bash
   git commit -m "Describe el cambio realizado"
   git push origin main
   ```
5. Coordina con el equipo antes de editar los mismos archivos y vuelve al paso 1 para la siguiente tarea.

**Con GitHub Desktop:** selecciona *main*, usa *Fetch/Pull origin*, revisa los archivos, confirma en *main* y pulsa *Push origin*.

### 6. Reglas para no pisarse el trabajo
- **No editen la misma escena (`.unity`) al mismo tiempo.** Git no puede mezclar bien dos cambios en una escena. Avisen en el grupo quién está con cuál.
- Las escenas se **generan por código**. Los cambios del laboratorio van en `Assets/Editor/LabSceneBuilder.cs` y luego se regeneran con el menú **Quimikaze → Construir escenas**. Si se edita una escena a mano, el próximo *Construir escenas* borra esos cambios.
- Los textos de los elementos están en `Assets/Resources/Datos/*.txt`. Son ideales para repartir: por ejemplo, una persona revisa los elementos 1 al 56 y otra del 57 al 118.
- **No suban** `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Builds/` ni APKs. El archivo `.gitignore` ya los excluye.
- Hagan commits pequeños y con mensajes claros.

### 7. Si hay un conflicto
Un conflicto puede aparecer cuando dos personas cambiaron las mismas líneas. Revisa los cambios locales y remotos antes de resolverlo.
- **Script o texto (`.cs`, `.txt`):** abre el archivo y busca las marcas `<<<<<<<`, `=======` y `>>>>>>>`. Deja la versión correcta, borra las marcas y ejecuta `git add` y luego `git commit`.
- **Escena:** resuelve primero los cambios del código que la genera y regénérala con **Construir escenas**. No elijas una versión a ciegas ni sobrescribas cambios locales sin revisarlos.

---

## 📦 Publicar una nueva versión del APK

### Compilar
1. En Unity: menú **Quimikaze → Compilar APK (Android)**. Puede tardar varios minutos, especialmente la primera vez.
2. El APK queda en `Builds/QuimikazeVR.apk`, ya con el nombre correcto para subirlo, y el resultado en `AutoSync/build_result.txt`.
3. Revisa el resultado y ejecuta `Tests/Test-ApkRelease.ps1` sobre el APK. La v1.3 debe conservar el paquete y el certificado de v1.2, con código de versión 4.
4. Prueba la instalación sobre v1.2 en un celular antes de dar por comprobada la actualización física; registra las pruebas pendientes en [VALIDACION_FINAL.md](VALIDACION_FINAL.md).

### Subir a GitHub Releases (desde la web)
1. En el repositorio, entra a **Releases → Draft a new release**.
2. En **Choose a tag** escribe la nueva versión, por ejemplo `v1.3`, y pulsa *Create new tag*.
3. Título: `Quimikaze v1.3`. En la descripción explica qué cambió.
4. Arrastra `Builds/QuimikazeVR.apk` a la zona *Attach binaries*. **El archivo debe llamarse `QuimikazeVR.apk`**, para que el enlace de descarga de este README siempre apunte a la última versión.
5. Pulsa **Publish release**.

### Subir a GitHub Releases (desde la terminal, con GitHub CLI)
Instala GitHub CLI desde https://cli.github.com e inicia sesión una vez con `gh auth login`. Después:
```bash
gh release create v1.3 Builds/QuimikazeVR.apk --title "Quimikaze v1.3" --notes-file NOVEDADES_v1.3.md
```

---

## 🧪 Cómo está hecho el proyecto

Unity 6 (6000.6.0f1) · Built-in Render Pipeline · Android en horizontal · IL2CPP, ARMv7 + ARM64.

### Carpetas
| Carpeta | Qué contiene |
|---|---|
| `Assets/Scripts/VR` | Visor en el celular (pantalla dividida, giroscopio), puntero por mirada y controles |
| `Assets/Scripts/Tabla` | Tabla periódica, leyendas y datos curiosos |
| `Assets/Scripts/Elemento` | Modelo del átomo, nube electrónica, estación de reacciones, muestras reales (`ElementSample` y el generador de formas `MeshKit`) y la transición con paisajes (`ElementIntro`) |
| `Assets/Scripts/Datos` | Base de datos de elementos, configuración electrónica y reacciones |
| `Assets/Scripts/Core` | Estado del juego, transiciones, sonidos y fábrica de objetos 3D |
| `Assets/Editor` | Constructor de escenas, materiales y mallas, y compilación del APK |
| `Assets/Resources/Datos` | Textos de los 118 elementos, editables |
| `Assets/Resources/Fotos` | Fotos reales de los elementos y retratos de los descubridores |
| `Assets/Resources/Paisajes` | Paisajes 360° de la transición y de la ventana |
| `Assets/Laboratorio/Fuentes`, `Assets/Resources/Fuentes` | Fuentes Fredoka y Atkinson Hyperlegible Next (.ttf y su versión para TextMeshPro); ver [TIPOGRAFIA.md](TIPOGRAFIA.md) |
| `Assets/Laboratorio/Shaders` | Shader de los íconos y etiquetas redondeadas (`PanelMesh`) |
| `Assets/Resources/LabMaterials`, `LabMeshes` | Materiales y modelos 3D generados |
| `Assets/Scenes` | `MenuPrincipal`, `TablaPeriodica` y `ElementoDetalle` |

### Escenas
| Escena | Qué contiene |
|---|---|
| `MenuPrincipal` | Entrada del juego en VR: tutorial, personaje, opciones, juego individual y preparación del aula LAN. |
| `TablaPeriodica` | Laboratorio: una sala blanca cerrada con la zona de inicio y la tabla periódica completa al frente. A los costados están los bloques s/p/d/f, los periodos, los tipos de elementos (al elegir uno se resalta su familia en la tabla), los grupos y sus nombres, los controles y datos curiosos. Atrás: el título, "la tabla en números" y "¿qué es un átomo?". |
| `ElementoDetalle` | El nivel de cada elemento. **Al frente:** el modelo 3D. **Izquierda:** historia, línea de tiempo, descubridor, origen del nombre, aportes y dato curioso. **Derecha:** propiedades (grupo, periodo, bloque, valencias, equivalente químico, configuración electrónica y electrones por nivel). **Adelante:** la estación de reacciones y los botones del modelo y de anterior/siguiente. **A la derecha:** la vitrina con la muestra real y su foto. **Atrás:** la ventana con el paisaje del elemento, la salida, los descubridores con su retrato y dónde se encuentra en la naturaleza. |

### Menú "Quimikaze" en Unity
- **Construir escenas**: regenera desde el código los materiales, las mallas y las tres escenas.
- **Mostrar laboratorio**: abre la escena principal y encuadra la vista.
- **Crear fuentes**: regenera las fuentes de TextMeshPro con todos los caracteres que usan los textos (después, vuelve a construir las escenas).
- **Vista previa: siguiente elemento**: con `ElementoDetalle` abierta, muestra el siguiente elemento.
- **Capturar vistas** / **Probar en modo Play**: guardan imágenes de control en `AutoSync/`.
- **Capturar muestras (118)**: foto de cada muestra 3D en `AutoSync/muestras`, para compararlas con las fotos reales.
- **Capturar descubridores**: revisa las pantallas de retratos y naturaleza de varios elementos.
- **Reimportar fotos y paisajes**: vuelve a aplicar la compresión de las imágenes.
- **Compilar APK (Android)**: genera `Builds/QuimikazeVR.apk`.

### Datos
Los textos están en `Assets/Resources/Datos`, en archivos separados por `|`:
- `elementos_base.txt`: símbolo, nombre, masa, categoría, estado, electronegatividad, puntos de fusión y ebullición, densidad, valencias, descubridor y año.
- `elementos_historia_1.txt` (Z 1–56) y `elementos_historia_2.txt` (Z 57–118): origen del nombre, historia, línea de tiempo con el formato `año=evento;año=evento`, aportes y valor, y dato curioso.
- `muestras.txt`: cómo se ve cada elemento. Incluye la forma del modelo 3D (tubo, ampolla, cristales, barra, escamas…), el color, el brillo metálico, una descripción, dónde se encuentra en la naturaleza, su paisaje y el pie de la foto.
- `descubridores.txt` y `personas.txt`: retratos, años de vida y descripción de los descubridores.
- `paisajes.txt` y `fotos_elementos.txt`: nombre, autor y licencia de cada paisaje y foto.

Los créditos de todas las imágenes están en [CREDITOS_IMAGENES.txt](CREDITOS_IMAGENES.txt). Las fuentes tienen licencia SIL Open Font License; los textos de la licencia están en `Assets/Laboratorio/Fuentes`.

La configuración electrónica se calcula con la regla de Moeller e incluye las excepciones reales (Cr, Cu, Pd, Au…). Las reacciones más comunes están escritas a mano en `ReactionDatabase.cs`. Las demás combinaciones se predicen con las valencias, y en pantalla se indica que es una predicción.

### Limitaciones conocidas
- No hay corrección de la distorsión de las lentes; la imagen solo se centra en cada lente.
- Los celulares que solo tienen OpenGL ES 3.0 no pueden ejecutar Unity 6.
- Algunos mandos Bluetooth genéricos pueden tener los botones en otro orden.
