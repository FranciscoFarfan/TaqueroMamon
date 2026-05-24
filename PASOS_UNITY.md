# Pasos Finales a Realizar en Unity Editor 🎮🌮

¡Listo! He implementado todo el código para cumplir con tu plan. Ahora solo necesitas hacer algunas asignaciones visuales y en los inspectores dentro de Unity. Aquí tienes la lista paso a paso:

---

## 1. 🔲 Agrandar el Área de Entrega (DeliveryArea)

- [X] Busca en tu jerarquía el objeto que contiene el script **`DeliveryArea.cs`**.
- [X] Debería tener un **BoxCollider** configurado como *Is Trigger*.
- [X] Aumenta los valores de `Size` en `X` y `Z` para que la zona sea más indulgente y fácil de acertar para el jugador.

## 2. 🌿 Corregir Texturas de las Plantas

- [X] Selecciona los modelos de las plantas en tu jerarquía o en la carpeta `Assets`.
- [X] Busca el material que tienen asignado (`MeshRenderer` > `Materials`).
- [X] Asegúrate de que el **Shader** sea compatible con tu render pipeline (ej. `Universal Render Pipeline/Lit` o `Standard`).
- [X] Si la planta se ve rosa (magenta), significa que falta reasignar el material o cambiar el shader. Arrastra la textura correcta de la hoja al campo `Base Map` o `Albedo`.

## 3. 🍽️ Actualizar el Prefab del Plato

- [X] Importa tu nuevo modelo 3D del plato de plástico.
- [X] Abre el prefab original `PlateSocket` (o reemplázalo cuidando no borrar los scripts).
- [X] Asegúrate de que el material del nuevo plato tenga un poco de **Smoothness** (brillo) para que parezca plástico (si usas URP Lit, súbele el Smoothness a 0.5-0.7).
- [X] **Muy importante:** Asegúrate de que el componente `PlateSocket.cs` siga en el prefab y de volver a asignar (si los borraste) los **puntos de Snap** (`TacoSnapPoints`) para que los tacos sepan dónde acomodarse visualmente.

## 4. 🎛️ Configurar Audio Ambiental (`AmbientSoundManager.cs`)

- [X] Crea un GameObject vacío en la escena y nómbralo **`AmbientSoundManager`**.
- [X] Arrástrale el script `AmbientSoundManager.cs`.
- [ ] Ve agregando tus **AudioClips** en los espacios correspondientes:
  - [ ] **Mañana (Menú):** `morningAmbient` (loop de fondo) y `morningSFX` (arreglo de audios cortos, suenan cada 30-50s).
  - [ ] **Tarde (Juego):** `afternoonAmbient` y `afternoonSFX`.
  - [ ] **Noche (Game Over):** `nightAmbient` y `nightSFX`.

## 5. 🌒 Configurar Iluminación Nocturna (`AmbientLightController.cs`)

- [X] Selecciona el objeto que tiene el script `AmbientLightController`.
- [X] Verás que agregué una nueva sección llamada **`Ambiente de NOCHE (fin del juego)`**.
- [X] Revisa los colores y configúralos a tu gusto. Si tienes un material de Skybox nocturno (estrellado oscuro), asígnalo en la ranura `Night Skybox`.

## 6. 🖤 Configurar el Fade a Negro (`ScreenFader.cs`)

- [X] En tu **XR Origin**, busca la **cámara principal** del jugador.
- [X] Haz clic derecho sobre la cámara -> `UI` -> `Canvas`.
- [X] Cambia el Canvas a **`Render Mode: World Space`**. Ajústalo para que esté justo frente a la cara de la cámara (casi pegado), cubriendo todo el campo de visión. Quítale el `Graphic Raycaster` para evitar bugs.
- [X] Dentro del Canvas, crea una `Image` o `Panel` que cubra todo y ponle color **Negro sólido**.
- [X] Agrégale al Canvas (o a la imagen) un componente **`Canvas Group`**. Pon su `Alpha` en 0.
- [X] A ese mismo objeto, agrégale el script **`ScreenFader.cs`** y enlázale el `Canvas Group` que acabas de crear. ¡Listo, los teletransportes (al inicio y al final del juego) ahora tendrán transición!

## 7. 📊 Configurar el Resumen de Partida (Game Over Canvas)

- [X] Ve a tu Canvas de Game Over (donde ya muestras el Score Final).
- [X] Añade nuevos textos (UIToolkit o TextMeshPro) para mostrar las estadísticas en la misma pantalla de fin de juego. Por ejemplo: Tacos Entregados: x0

  Órdenes Completadas: x0

  Tortillas Perdidas: x0

  Carne Caída: x0

  Tacos Perdidos/Mermados: x0

  Total Ganado: $0

  Total Penalizaciones: $0
- [X] Selecciona el objeto que tiene tu script **`UIManager`**.
- [X] Ahora verás una nueva sección llamada **`Estadísticas (Game Over)`**. Arrastra allí cada uno de los textos que creaste en el paso anterior.

## 8. ⏸️ Menú de Pausa Flotante (`PauseManager.cs`)

- [ ] Crea un nuevo Canvas (World Space) y ponle un Panel con botones para "Continuar" y "Terminar Partida".
- [ ] Posiciónalo frente al jugador (puedes hacerlo hijo de la cámara, o que un script lo siga, o simplemente dejarlo flotando en un lugar accesible).
- [ ] Añádele el script **`PauseManager.cs`** al Canvas o a un GameObject gestor.
- [ ] Asigna el `Pause Canvas` y los botones.
- [ ] **Input Action:** Selecciona qué botón de tu control VR servirá para pausar y asígnalo en el campo `Pause Action` (ej. el botón Start/Menú del controlador izquierdo).

## 9. 📻 Configurar los Comerciales en la Radio

* [ ] Selecciona el objeto de tu **Radio** en la escena o prefab (el que tiene el script `Radio.cs`).
* [ ] En el Inspector verás una nueva lista llamada **`Ads`** justo debajo de las canciones.
* [ ] Arrastra ahí tus audios de comerciales/anuncios.
* [ ] Por defecto, el campo `Songs Before Ad` está en **2**. Esto significa que sonarán 2 canciones, luego 1 comercial, luego 2 canciones, etc. Si luego decides que sea cada 3 canciones, ¡solo le cambias el número ahí mismo!

## 10. 🔪 Configurar el reinicio del Cuchillo (al terminar juego)

- [X] Selecciona el prefab o el objeto del **Cuchillo**.
- [X] Verifica que el GameManager o el script correspondiente esté configurado para regresar el cuchillo a la mesa y evitar que se quede tirado en el suelo cuando termina la partida.

## 11. 📝 Añadir Instrucciones más detalladas

- [ ] Ve a tu Canvas del Menú Principal.
- [ ] Modifica o añade un nuevo panel de texto que contenga **instrucciones más detalladas** sobre cómo jugar antes de que el jugador inicie la partida.

## 12. ⏳ Configurar tiempos de espera (Animación del Sol)

- [X] Selecciona tu **GameManager**.
- [X] Ajusta las variables de tiempo de espera (`Wait Time` o similar) para que la animación del sol tenga tiempo suficiente para ejecutarse al **iniciar el juego**.
- [X] Haz lo mismo para la animación del sol al **terminar el juego**, de manera que el sol baje y se haga de noche antes de que aparezca la pantalla de fin de partida.

---

Con esto, las mecánicas y lógicas que pediste ya estarán funcionando juntas a la perfección. ¡A echar tacos! 🌮🔥
