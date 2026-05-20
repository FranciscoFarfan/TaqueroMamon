# Pasos Finales a Realizar en Unity Editor 🎮🌮

¡Listo! He implementado todo el código para cumplir con tu plan. Ahora solo necesitas hacer algunas asignaciones visuales y en los inspectores dentro de Unity. Aquí tienes la lista paso a paso:

---

## 1. 🔲 Agrandar el Área de Entrega (DeliveryArea)
1. Busca en tu jerarquía el objeto que contiene el script **`DeliveryArea.cs`**.
2. Debería tener un **BoxCollider** configurado como *Is Trigger*.
3. Aumenta los valores de `Size` en `X` y `Z` para que la zona sea más indulgente y fácil de acertar para el jugador.

## 2. 🌿 Corregir Texturas de las Plantas
1. Selecciona los modelos de las plantas en tu jerarquía o en la carpeta `Assets`.
2. Busca el material que tienen asignado (`MeshRenderer` > `Materials`).
3. Asegúrate de que el **Shader** sea compatible con tu render pipeline (ej. `Universal Render Pipeline/Lit` o `Standard`).
4. Si la planta se ve rosa (magenta), significa que falta reasignar el material o cambiar el shader. Arrastra la textura correcta de la hoja al campo `Base Map` o `Albedo`.

## 3. 🍽️ Actualizar el Prefab del Plato
1. Importa tu nuevo modelo 3D del plato de plástico.
2. Abre el prefab original `PlateSocket` (o reemplázalo cuidando no borrar los scripts).
3. Asegúrate de que el material del nuevo plato tenga un poco de **Smoothness** (brillo) para que parezca plástico (si usas URP Lit, súbele el Smoothness a 0.5-0.7).
4. **Muy importante:** Asegúrate de que el componente `PlateSocket.cs` siga en el prefab y de volver a asignar (si los borraste) los **puntos de Snap** (`TacoSnapPoints`) para que los tacos sepan dónde acomodarse visualmente.

## 4. 🎛️ Configurar Audio Ambiental (`AmbientSoundManager.cs`)
1. Crea un GameObject vacío en la escena y nómbralo **`AmbientSoundManager`**.
2. Arrástrale el script `AmbientSoundManager.cs`.
3. Ve agregando tus **AudioClips** en los espacios correspondientes:
   - **Mañana (Menú):** `morningAmbient` (loop de fondo) y `morningSFX` (arreglo de audios cortos).
   - **Tarde (Juego):** `afternoonAmbient` y `afternoonSFX`.
   - **Noche (Game Over):** `nightAmbient` y `nightSFX`.

## 5. 🌒 Configurar Iluminación Nocturna (`AmbientLightController.cs`)
1. Selecciona el objeto que tiene el script `AmbientLightController`.
2. Verás que agregué una nueva sección llamada **`Ambiente de NOCHE (fin del juego)`**.
3. Revisa los colores y configúralos a tu gusto. Si tienes un material de Skybox nocturno (estrellado oscuro), asígnalo en la ranura `Night Skybox`.

## 6. 🖤 Configurar el Fade a Negro (`ScreenFader.cs`)
1. En tu **XR Origin**, busca la **cámara principal** del jugador.
2. Haz clic derecho sobre la cámara -> `UI` -> `Canvas`. 
3. Cambia el Canvas a **`Render Mode: World Space`**. Ajústalo para que esté justo frente a la cara de la cámara (casi pegado), cubriendo todo el campo de visión. Quítale el `Graphic Raycaster` para evitar bugs.
4. Dentro del Canvas, crea una `Image` o `Panel` que cubra todo y ponle color **Negro sólido**.
5. Agrégale al Canvas (o a la imagen) un componente **`Canvas Group`**. Pon su `Alpha` en 0.
6. A ese mismo objeto, agrégale el script **`ScreenFader.cs`** y enlázale el `Canvas Group` que acabas de crear. ¡Listo, los teletransportes ahora tendrán transición!

## 7. 📊 Configurar el Resumen de Partida (Game Over Canvas)
1. Ve a tu Canvas de Game Over (donde ya muestras el Score Final).
2. Añade nuevos textos (UIToolkit o TextMeshPro) para mostrar las estadísticas. Por ejemplo:
   - Tacos Entregados: [0]
   - Órdenes Completadas: [0]
   - Tortillas Perdidas: [0]
   - Carne Caída: [0]
   - Tacos Perdidos/Mermados: [0]
   - Total Ganado: [$0]
   - Total Penalizaciones: [$0]
3. Selecciona el objeto que tiene tu script **`UIManager`**.
4. Ahora verás una nueva sección llamada **`Estadísticas (Game Over)`**. Arrastra allí cada uno de los textos que creaste en el paso anterior.

## 8. ⏸️ Menú de Pausa Flotante (`PauseManager.cs`)
1. Crea un nuevo Canvas (World Space) y ponle un Panel con botones para "Continuar" y "Terminar Partida".
2. Posiciónalo frente al jugador (puedes hacerlo hijo de la cámara, o que un script lo siga, o simplemente dejarlo flotando en un lugar accesible).
3. Añádele el script **`PauseManager.cs`** al Canvas o a un GameObject gestor.
4. Asigna el `Pause Canvas` y los botones.
5. **Input Action:** Selecciona qué botón de tu control VR servirá para pausar y asígnalo en el campo `Pause Action` (ej. el botón Start/Menú del controlador izquierdo).

## 9. 📻 Configurar los Comerciales en la Radio
1. Selecciona el objeto de tu **Radio** en la escena o prefab (el que tiene el script `Radio.cs`).
2. En el Inspector verás una nueva lista llamada **`Ads`** justo debajo de las canciones.
3. Arrastra ahí tus audios de comerciales/anuncios.
4. Por defecto, el campo `Songs Before Ad` está en **2**. Esto significa que sonarán 2 canciones, luego 1 comercial, luego 2 canciones, etc. Si luego decides que sea cada 3 canciones, ¡solo le cambias el número ahí mismo!

---

Con esto, las mecánicas y lógicas que pediste ya estarán funcionando juntas a la perfección. ¡A echar tacos! 🌮🔥
