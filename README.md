# QuimikazeVR · Laboratorio Químico VR

Laboratorio virtual de química para chicos de 10 a 14 años que no tienen acceso a un laboratorio real. Funciona con un **celular Android dentro de un visor tipo VR Box o Cardboard**, el botón del visor y un **mando Bluetooth genérico**.

Entras a una sala blanca con la **tabla periódica completa de los 118 elementos**. Al elegir un elemento pasas a su propio nivel, con:
- su **modelo 3D** (Bohr, nube electrónica y núcleo);
- su **historia**: línea de tiempo, descubridor y datos curiosos;
- sus **propiedades**: grupo, periodo, valencias y configuración electrónica;
- una **estación de reacciones** con otros elementos.

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
5. Abre **Laboratorio Químico VR**.

### Primera vez con el visor
1. Coloca el celular en el visor, en horizontal.
2. Mira al frente unos segundos: la vista se centra sola.
3. Si se ve doble, ajusta la separación de las lentes con **L1 / R1** del mando.
4. Si la vista queda torcida, **mantén pulsado** el botón del visor para recentrarla.

---

## 🎮 Controles

| Acción | Visor / mando | En el editor de Unity |
|---|---|---|
| Seleccionar lo que miras | Toque corto del botón del visor o **A** | Clic izquierdo, Espacio o Enter |
| Recentrar la vista | **Mantener pulsado** el botón del visor (1.5 s) o **Y** | R |
| Caminar | Joystick izquierdo o cruceta | WASD o flechas |
| Girar | Joystick derecho | Q / E |
| Mirar alrededor | Mover la cabeza | Mantener clic derecho y mover el mouse |
| Volver a la tabla | **B** o botón atrás del celular | Escape |
| Salir de la app (en la tabla) | Botón atrás del celular **dos veces** | — |
| Cambiar el modelo del átomo | **X** | M |
| Ajustar las lentes si se ve doble | **L1 / R1** (1 mm por pulsación; se guarda) | [ / ] |
| Pantalla dividida para el visor | Automática en el celular | V |

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
Trabajamos con **ramas**: cada persona hace sus cambios en una rama propia y después se unen a `main`.

1. **Actualiza tu copia** antes de empezar:
   ```bash
   git checkout main
   ```
   ```bash
   git pull
   ```
2. **Crea una rama** con un nombre que diga qué vas a hacer:
   ```bash
   git checkout -b historia-elementos-57-118
   ```
3. Haz tus cambios en Unity o en los scripts y **pruébalos con ▶ Play**.
4. **Guarda tus cambios** en Git:
   ```bash
   git add -A
   ```
   ```bash
   git commit -m "Corrige la historia de los lantánidos"
   ```
5. **Sube tu rama** a GitHub:
   ```bash
   git push -u origin historia-elementos-57-118
   ```
6. En GitHub aparecerá el botón **Compare & pull request**. Ábrelo, explica qué cambiaste y crea el **Pull Request**.
7. Otro miembro del equipo lo revisa y pulsa **Merge pull request**. Así tus cambios entran a `main`.
8. Vuelve al paso 1 para la siguiente tarea.

**Con GitHub Desktop** es lo mismo:
- *Fetch/Pull origin* para actualizar.
- *Current branch → New branch* para crear la rama.
- Escribes el resumen y pulsas *Commit to …*.
- *Push origin* para subir y *Create Pull Request* para el Pull Request.

### 6. Reglas para no pisarse el trabajo
- **No editen la misma escena (`.unity`) al mismo tiempo.** Git no puede mezclar bien dos cambios en una escena. Avisen en el grupo quién está con cuál.
- Las escenas se **generan por código**. Los cambios del laboratorio van en `Assets/Editor/LabSceneBuilder.cs` y luego se regeneran con el menú **Laboratorio VR → Construir escenas**. Si se edita una escena a mano, el próximo *Construir escenas* borra esos cambios.
- Los textos de los elementos están en `Assets/Resources/Datos/*.txt`. Son ideales para repartir: por ejemplo, una persona revisa los elementos 1 al 56 y otra del 57 al 118.
- **No suban** `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Builds/` ni APKs. El archivo `.gitignore` ya los excluye.
- Hagan commits pequeños y con mensajes claros.

### 7. Si hay un conflicto
Un conflicto aparece en `git pull` o en el Pull Request cuando dos personas cambiaron las mismas líneas.
- **Script o texto (`.cs`, `.txt`):** abre el archivo y busca las marcas `<<<<<<<`, `=======` y `>>>>>>>`. Deja la versión correcta, borra las marcas y ejecuta `git add` y luego `git commit`.
- **Escena:** quédate con una de las dos versiones y vuelve a generar las escenas con **Construir escenas**:
  ```bash
  git checkout --theirs Assets/Scenes/TablaPeriodica.unity
  ```

---

## 📦 Publicar una nueva versión del APK

### Compilar
1. En Unity: menú **Laboratorio VR → Compilar APK (Android)**. Tarda unos 3 minutos y la primera vez más.
2. El APK queda en `Builds/LaboratorioQuimicaVR.apk`, y el resultado en `AutoSync/build_result.txt`.
3. Antes de publicar, pruébalo en un celular.

### Subir a GitHub Releases (desde la web)
1. En el repositorio, entra a **Releases → Draft a new release**.
2. En **Choose a tag** escribe la nueva versión, por ejemplo `v1.1`, y pulsa *Create new tag*.
3. Título: `QuimikazeVR v1.1`. En la descripción explica qué cambió.
4. Arrastra el APK a la zona *Attach binaries*. **Cámbiale el nombre a `QuimikazeVR.apk` antes de subirlo**, para que el enlace de descarga de este README siempre apunte a la última versión.
5. Pulsa **Publish release**.

### Subir a GitHub Releases (desde la terminal, con GitHub CLI)
Instala GitHub CLI desde https://cli.github.com e inicia sesión una vez con `gh auth login`. Después:
```bash
copy Builds\LaboratorioQuimicaVR.apk Builds\QuimikazeVR.apk
```
```bash
gh release create v1.1 Builds/QuimikazeVR.apk --title "QuimikazeVR v1.1" --notes "Qué cambió en esta versión"
```

---

## 🧪 Cómo está hecho el proyecto

Unity 6 (6000.6.0f1) · Built-in Render Pipeline · Android en horizontal · IL2CPP, ARMv7 + ARM64.

### Carpetas
| Carpeta | Qué contiene |
|---|---|
| `Assets/Scripts/VR` | Visor en el celular (pantalla dividida, giroscopio), puntero por mirada y controles |
| `Assets/Scripts/Tabla` | Tabla periódica, leyendas y datos curiosos |
| `Assets/Scripts/Elemento` | Modelo del átomo, nube electrónica, muestra real y estación de reacciones |
| `Assets/Scripts/Datos` | Base de datos de elementos, configuración electrónica y reacciones |
| `Assets/Scripts/Core` | Estado del juego, transiciones, sonidos y fábrica de objetos 3D |
| `Assets/Editor` | Constructor de escenas, materiales y mallas, y compilación del APK |
| `Assets/Resources/Datos` | Textos de los 118 elementos, editables |
| `Assets/Resources/LabMaterials`, `LabMeshes` | Materiales y modelos 3D generados |
| `Assets/Scenes` | `TablaPeriodica` y `ElementoDetalle` |

### Escenas
| Escena | Qué contiene |
|---|---|
| `TablaPeriodica` | Pantalla de inicio: una sala blanca cerrada con la zona de inicio y la tabla periódica completa al frente. A los costados están los bloques s/p/d/f, los periodos, los tipos de elementos (al elegir uno se resalta su familia en la tabla), los grupos y sus nombres, los controles y datos curiosos. Atrás: el título, "la tabla en números" y "¿qué es un átomo?". |
| `ElementoDetalle` | El nivel de cada elemento. **Al frente:** el modelo 3D. **Izquierda:** historia, línea de tiempo, descubridor, origen del nombre, aportes y dato curioso. **Derecha:** propiedades (grupo, periodo, bloque, valencias, equivalente químico, configuración electrónica y electrones por nivel). **Adelante:** la estación de reacciones y los botones del modelo y de anterior/siguiente. **Atrás:** volver a la tabla. |

### Menú "Laboratorio VR" en Unity
- **Construir escenas**: regenera desde el código los materiales, las mallas y las dos escenas.
- **Mostrar laboratorio**: abre la escena principal y encuadra la vista.
- **Vista previa: siguiente elemento**: con `ElementoDetalle` abierta, muestra el siguiente elemento.
- **Capturar vistas** / **Probar en modo Play**: guardan imágenes de control en `AutoSync/`.
- **Compilar APK (Android)**: genera `Builds/LaboratorioQuimicaVR.apk`.

### Datos
Los textos están en `Assets/Resources/Datos`, en archivos separados por `|`:
- `elementos_base.txt`: símbolo, nombre, masa, categoría, estado, electronegatividad, puntos de fusión y ebullición, densidad, valencias, descubridor y año.
- `elementos_historia_1.txt` (Z 1–56) y `elementos_historia_2.txt` (Z 57–118): origen del nombre, historia, línea de tiempo con el formato `año=evento;año=evento`, aportes y valor, y dato curioso.

La configuración electrónica se calcula con la regla de Moeller e incluye las excepciones reales (Cr, Cu, Pd, Au…). Las reacciones más comunes están escritas a mano en `ReactionDatabase.cs`. Las demás combinaciones se predicen con las valencias, y en pantalla se indica que es una predicción.

### Limitaciones conocidas
- No hay corrección de la distorsión de las lentes; la imagen solo se centra en cada lente.
- Los celulares que solo tienen OpenGL ES 3.0 no pueden ejecutar Unity 6.
- Algunos mandos Bluetooth genéricos pueden tener los botones en otro orden.
