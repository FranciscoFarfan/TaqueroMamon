# Plan de Implementación: Taquero Mamón

Basado en el archivo de `PENDIENTES.md`, he estructurado las tareas en fases lógicas para abordarlas de la forma más eficiente. Esto minimizará los conflictos entre scripts y te permitirá ver resultados rápidamente.

---

## 🏃 Fase 1: Ajustes Rápidos y Correcciones Visuales (Quick Wins)

*Estas tareas son independientes y se pueden solucionar rápidamente directamente desde el editor de Unity.*

- [X] **Agrandar el área de entrega:**
  - **Acción:** Seleccionar el prefab/objeto que tiene el script `DeliveryArea.cs` y aumentar el tamaño de su BoxCollider (o el colisionador que utilice) en el eje X/Z.
- [X] **Corregir texturas de las plantas:**
  - **Acción:** Revisar el material asignado a los modelos de las plantas, asegurar que el *Shader* sea correcto (por ejemplo, URP Lit o Standard) y que la textura difusa/albedo esté bien asignada.
- [ ] **Actualizar el Prefab del Plato:**
  - **Acción:** Importar o modificar el modelo 3D para que parezca un plato de plástico (típico de taquería). Asignar materiales con un poco de brillo/smoothness y actualizar el prefab existente (`PlateSocket`).
- [ ] **Gestionar el Cuchillo al terminar el juego:**
  - **Acción:** En `GameManager.cs`, en el método donde finaliza el juego, buscar la referencia del cuchillo (o todos los objetos interactuables) y reiniciarlo a su posición original o destruirlo/desactivarlo.

---

## 🎮 Fase 2: Flujo del Juego, UI y Estadísticas

*Tareas relacionadas con el inicio y fin de la partida, así como la recolección de datos.*

- [ ] **Instrucciones detalladas de inicio:**
  - **Acción:** Modificar el Canvas del Menú Principal. Cambiar los textos para explicar claramente cómo jugar, los botones a usar y las penalizaciones.
- [ ] **Control de Tiempos (Animaciones del Sol):**
  - **Acción:** En `GameManager.cs`, utilizar corrutinas (`IEnumerator`) al momento de iniciar y terminar el juego.
  - **Inicio:** `yield return new WaitForSeconds(tiempo);` para que la animación del sol termine antes de hacer *spawn* de clientes o soltar al jugador.
  - **Fin:** Misma técnica; esperar a que el sol se oculte antes de mostrar la pantalla final.
- [ ] **Sistema de Resumen de Partida (End Screen):**
  - **Acción:**
    1. Crear variables en `GameManager.cs` para llevar el conteo de: `tacosEntregados`, `ordenesCompletadas`, `tortillasCaidas`, `tacosCaidos`, `carneCaida` y el puntaje `Total`.
    2. Crear un panel en la UI (Canvas de fin de juego) con textos para estas métricas.
    3. Al terminar el juego, actualizar los textos con estas variables.
- [ ] **Pausar / Terminar la partida:**
  - **Acción:** Configurar un botón del control VR (ej. Botón de Menú/Start). Al presionarlo, activar un Canvas flotante frente al jugador con opciones de "Continuar" o "Terminar Partida" (que llamaría al método de fin de juego del `GameManager`).

---

## 🌙 Fase 3: Inmersión, Audio y Transiciones (Polish)

*Mejoras significativas en la experiencia del jugador y el entorno.*

- [ ] **Transiciones (Fades) en Teletransporte:**
  - **Acción:** Implementar un efecto de fundido a negro (Fade In / Fade Out) en la cámara de VR. Se debe llamar:
    1. Cuando el jugador se teletransporta del menú principal a la taquería.
    2. Cuando se teletransporta de vuelta o termina la partida.
- [ ] **Ambientación Nocturna:**
  - **Acción:** Configurar la iluminación general o hacer un cambio de Skybox en el código cuando la partida termina, de manera que la iluminación ambiental cambie de atardecer/día a noche cerrada.
- [ ] **Sistema de Audio Dinámico (Mañana / Tarde / Noche):**
  - **Acción:** Crear un nuevo script (ej. `AudioManager.cs` o `AmbientSoundManager.cs`).
  - **Requisitos del script:**
    - Tener 3 `AudioClips` de ambiente largo (loops continuos para Mañana, Tarde, Noche).
    - Tener 3 arreglos de `AudioClips` con sonidos cortos/aleatorios (perros, autos, gente, grillos).
    - Usar una corrutina que cada *X* segundos (generados aleatoriamente entre 30 y 50) reproduzca un clip aleatorio del arreglo correspondiente a la etapa actual del día.
