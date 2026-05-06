# 🌮 Guía de Integración — Taquero Mamón VR

## Paso 0: Tags y Layers

Antes de todo, crear estos **Tags** en Unity (`Edit > Project Settings > Tags and Layers`):

| Tag          | ¿Ya existe?                    |
| ------------ | ------------------------------- |
| `Tortilla` | Verificar                       |
| `Cuchillo` | ✅ Ya se usa en MeatCutter      |
| `Pastor`   | Verificar                       |
| `taco`     | Verificar                       |
| `Plato`    | Crear                           |
| `Floor`    | ✅ Ya se usa en DroppableObject |

---

## Paso 1: Preparar el Prefab de Tortilla

El prefab `Tortilla.prefab` en `Assets/PROYECTO/Prefabs/Interactables/` necesita estos componentes:

### Componentes necesarios:

1. **Tag**: `Tortilla`
2. **Rigidbody**: Ya debería tener
3. **Collider**: Ya debería tener (asegurar que NO sea trigger para la física). Agregar un **segundo collider** trigger más grande para que detecte colisiones con la carne de pastor.
4. **XRGrabInteractable**: Ya debería tener
5. **TortillaManager** (NUEVO):
   - `Cook Time`: 5
   - `Burn Time`: 45
   - `Raw Material`: Material de tortilla normal
   - `Cooked Material`: Material con tono más dorado/café
   - `Burnt Material`: Material oscuro/negro
   - `Burn Penalty`: 10
6. **TacoAssembler** (NUEVO):
   - `Taco Pastor Prefab` → Arrastra `TacoPastor.prefab`
   - `Taco Bistec Prefab` → Arrastra `TacoBistec.prefab`
   - `Taco Queso Prefab` → Arrastra `TacoQueso.prefab`
   - `Taco Generic Prefab` → Arrastra `TacoListo.prefab`
   - `Secondary Button Action` → Ver sección "Input Action" abajo
   - `Pastor Tag`: "Pastor"
7. **DroppableObject**: Ya podría tener, si no:
   - `Penalty Points`: 5
   - `Penalty Reason`: "Tortilla caída"

### Configurar el Input Action del botón secundario:

1. Ir a `Assets/` y buscar el **XRI Default Input Actions** (o tu Input Actions asset)
2. Encontrar la acción del botón **A** (mano derecha) o **X** (mano izquierda)
   - En XRI es típicamente: `XRI LeftHand Interaction/Activate` o crear una nueva
3. Crear una nueva acción si no existe:
   - Nombre: `SecondaryButton`
   - Binding: `<XRController>{LeftHand}/secondaryButton`
4. Arrastrar esa referencia al campo `Secondary Button Action` del TacoAssembler

> [!TIP]
> Si no quieres complicarte con InputActionReference, puedes cambiar el script para usar `XRController.inputDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool pressed)` directamente. Pero la aproximación con InputActionReference es más limpia.

---

## Paso 2: Preparar los Prefabs de Taco

Para cada prefab de taco (`TacoPastor.prefab`, `TacoBistec.prefab`, `TacoQueso.prefab`, `TacoListo.prefab`):

1. **Tag**: `taco`
2. **Rigidbody**: Agregar si no tiene
3. **Collider**: Agregar si no tiene
4. **XRGrabInteractable**: Agregar para que el jugador pueda agarrarlo
5. **TacoData** (NUEVO):
   - `TacoPastor.prefab` → `meatType`: "Pastor"
   - `TacoBistec.prefab` → `meatType`: "Bistec"
   - `TacoQueso.prefab` → `meatType`: "Queso"
   - `TacoListo.prefab` → `meatType`: "Generico"
6. **DroppableObject**:
   - `Penalty Points`: 5
   - `Penalty Reason`: "Taco caído"

---

## Paso 3: Configurar el Prefab de Pastor (trozo)

El prefab `Pastor.prefab`:

1. **Tag**: `Pastor`
2. **Rigidbody**: Agregar (NO kinematic, gravity ON)
3. **Collider**: Agregar con **Is Trigger = TRUE** (para que el TacoAssembler lo detecte con OnTriggerEnter)
4. **DroppableObject**:
   - `Penalty Points`: 3
   - `Penalty Reason`: "Carne de pastor caída"

> [!IMPORTANT]
> El collider del trozo de pastor debe ser **trigger** para que pueda colisionar con la tortilla y ser detectado por TacoAssembler.OnTriggerEnter. Si necesitas que también tenga física de colisión con el suelo, agrega DOS colliders: uno trigger (para la tortilla) y uno normal (para el suelo).

---

## Paso 4: Configurar el Prefab de Plato

El prefab `Plato.prefab`:

1. **Tag**: `Plato`
2. **Rigidbody**: Ya debería tener
3. **Colliders**:
   - Un collider **sólido** para la física normal
   - Un collider **trigger** más grande (esfera o caja) para detectar tacos que se colocan encima
4. **XRGrabInteractable**: Ya debería tener
5. **PlateSocket** (NUEVO):
   - `Max Tacos`: 5
   - `Taco Tag`: "taco"
   - Crear 5 GameObjects vacíos como hijos del plato, posicionados donde van los tacos:
     ```
     Plato/
     ├─ TacoSlot1 (pos: 0, 0.02, 0)
     ├─ TacoSlot2 (pos: 0.03, 0.02, 0)
     ├─ TacoSlot3 (pos: -0.03, 0.02, 0)
     ├─ TacoSlot4 (pos: 0, 0.02, 0.03)
     └─ TacoSlot5 (pos: 0, 0.02, -0.03)
     ```
   - Arrastrar estos 5 slots al array `Taco Snap Points`
6. **DroppableObject**:
   - `Penalty Points`: 15
   - `Penalty Reason`: "Plato caído"

---

## Paso 5: Configurar el Comal (6 slots)

1. Crear **6 GameObjects vacíos** como hijos del comal, posicionados en cada lugar donde puede ir una tortilla:

   ```
   Comal/
   ├─ ComalSlot1 (agregar BoxCollider trigger + ComalSocket)
   ├─ ComalSlot2 (agregar BoxCollider trigger + ComalSocket)
   ├─ ComalSlot3 (agregar BoxCollider trigger + ComalSocket)
   ├─ ComalSlot4 (agregar BoxCollider trigger + ComalSocket)
   ├─ ComalSlot5 (agregar BoxCollider trigger + ComalSocket)
   └─ ComalSlot6 (agregar BoxCollider trigger + ComalSocket)
   ```
2. A cada slot agregar:

   - **BoxCollider** (o SphereCollider) → **Is Trigger = TRUE**
   - **ComalSocket** script
   - Ajustar el tamaño del collider para que cubra el área donde se pone una tortilla

---

## Paso 6: Configurar las Carnes en la Plancha

Para cada montón de carne estática en la plancha (Bistec, Queso, etc.):

1. Al objeto del montón de carne agregar:
   - **Collider** → **Is Trigger = TRUE**
   - **MeatPileSocket** (NUEVO):
     - Montón de Bistec → `meatType`: "Bistec"
     - Montón de Queso → `meatType`: "Queso"
     - Otros montones → El string que corresponda

> [!NOTE]
> Los montones de carne son **estáticos** (no se mueven, no se agarran). Solo son zonas trigger donde el jugador pone la tortilla.

---

## Paso 7: Configurar el Trompo (MeatCutter)

El script `MeatCutter.cs` ya está en el trompo. Solo verificar:

1. `Pastor Prefab` → Arrastra `Pastor.prefab`
2. `Fuerza Lanzamiento`: 3 (ajustar al gusto)
3. `Use Knife Direction`: true
4. `Random Spread`: 0.3
5. `Cooldown Corte`: 0.5
6. `Punto De Spawn`: Si quieres que siempre salga del mismo punto del trompo

---

## Paso 8: Configurar el Cuchillo

El prefab `Meat Cleaver.prefab`:

1. **Tag**: `Cuchillo` (ya debería tener)
2. **XRGrabInteractable**: Ya debería tener
3. Verificar que tiene al menos un **Collider** (puede ser trigger)

---

## Paso 9: Configurar el TortillaSpawner

1. Crear un **GameObject vacío** cerca del área de trabajo, nómbralo `TortillaSpawner`
2. Agregar el script **TortillaSpawner**
3. Configurar:
   - `Tortilla Prefab` → `Tortilla.prefab`
   - `Spawn Point` → Un Transform donde aparecen las tortillas (ej. sobre una mesa)
   - `Max Tortillas`: 12
   - `Spawn Interval`: 10
   - `Spawn Initial Batch`: true
   - `Initial Batch Size`: 6

---

## Paso 10: Configurar los NPCs

### En los Prefabs de NPC:

Los prefabs en `Prefabs/NPC/prefabs/` ya tienen `PersonController`. Agregar:

1. **PersonInteraction** (ya reescrito) — verificar que está actualizado
2. Configurar en el Inspector:
   - `Detection Radius`: 1.5
   - `Plate Tag`: "Plato"
   - `Points Per Matching Taco`: 10
   - (Opcional) `Order Bubble` y `Order Bubble Text` si quieres mostrar el pedido sobre la cabeza

### En PersonSpawner:

Ya fue modificado. Verificar que:

- `People Prefabs` → Los prefabs de NPC
- `Spawn Point` → Punto de entrada
- `Queue Manager` → Referencia al QueueManager
- `Exit Point` → Punto de salida

### En QueueManager:

No necesita cambios, pero verificar:

- `Queue Spots` → 3 posiciones de la fila (Transforms vacíos)

---

## Paso 11: Configurar la UI

### 11a. Canvas del HUD (World Space)

Crear un Canvas WorldSpace posicionado frente al jugador (o en el puesto de tacos):

```
Canvas_HUD (WorldSpace)/
├─ TimerText (TextMeshPro - "03:00")
├─ ScoreText (TextMeshPro - "$0")
└─ ...
```

### 11b. Canvas del Tendedero (World Space)

Crear un Canvas WorldSpace en el tendedero (las cuerdas donde se cuelgan los pedidos):

```
Canvas_Tendedero (WorldSpace)/
├─ OrderCard_1/
│   ├─ Background (Image)
│   ├─ MeatTypeText (TMP - "Pastor")
│   ├─ TacoCountText (TMP - "x3")
│   └─ RewardText (TMP - "$30")
├─ OrderCard_2/ (misma estructura)
└─ OrderCard_3/ (misma estructura)
```

Agregar **OrderCardUI** a cada `OrderCard_X` y asignar las referencias.

### 11c. Canvas Inicio y Game Over (World Space o Overlay)

```
Canvas_Screens/
├─ StartScreen/
│   ├─ Title (TMP - "TAQUERO MAMÓN")
│   ├─ PlayerNameInput (TMP_InputField)
│   └─ StartButton (Button)
├─ GameOverScreen/
│   ├─ GameOverTitle (TMP - "¡SE ACABÓ!")
│   ├─ FinalScoreText (TMP)
│   ├─ PlayerNameText (TMP)
│   └─ RestartButton (Button)
```

### 11d. UIManager

1. Crear (o usar) un **GameObject** con el script **UIManager**
2. Asignar TODAS las referencias:
   - **HUD**: `Timer Text`, `Score Text`, `HUD Container`
   - **Tendedero**: Los 3 `OrderCardUI`
   - **Start Screen**: El container, el input, el botón
   - **Game Over**: El container, los textos, el botón de reiniciar

---

## Paso 12: GameManager

Verificar que el **GameManager** tiene:

- `World Active` → El GameObject con todo lo del juego activo (taquería abierta)
- `World Inactive` → El GameObject con lo del juego inactivo (taquería cerrada)
- `Game Duration`: 180
- `Max Active Orders`: 3
- `Available Meats`: ["Pastor", "Bistec", "Queso"] (o las que tengas)
- `Points Per Taco`: 10

---

## Checklist Final

### Tags ✅

- [X] `Tortilla` creado y asignado al prefab Tortilla
- [X] `Cuchillo` asignado al prefab Meat Cleaver
- [X] `Pastor` creado y asignado al prefab Pastor (trozo)
- [X] `taco` creado y asignado a TacoPastor, TacoBistec, TacoQueso, TacoListo
- [X] `Plato` creado y asignado al prefab Plato
- [X] `Floor` asignado al suelo

### Prefabs ✅

- [X] Tortilla tiene: TortillaManager, TacoAssembler, XRGrabInteractable, Rigidbody, DroppableObject
- [X] TacoPastor tiene: TacoData (meatType="Pastor"), XRGrabInteractable, DroppableObject, tag "taco"
- [X] TacoBistec tiene: TacoData (meatType="Bistec"), XRGrabInteractable, DroppableObject, tag "taco"
- [X] TacoQueso tiene: TacoData (meatType="Queso"), XRGrabInteractable, DroppableObject, tag "taco"
- [X] Pastor (trozo) tiene: Rigidbody, Collider (trigger), DroppableObject, tag "Pastor"
- [X] Plato tiene: PlateSocket, XRGrabInteractable, Rigidbody, 2 colliders (1 solid + 1 trigger), tag "Plato"
- [X] Meat Cleaver tiene: XRGrabInteractable, Collider, tag "Cuchillo"

### Escena ✅

- [X] 6 ComalSocket en el comal (cada uno con trigger collider)
- [X] MeatPileSocket en cada montón de carne de la plancha
- [X] MeatCutter en el trompo de pastor
- [X] TortillaSpawner en algún punto de la escena
- [X] GameManager singleton en la escena
- [ ] PersonSpawner configurado con QueueManager
- [ ] QueueManager con 3 spots de fila
- [ ] UIManager con todas las referencias de UI

### UI ✅

- [ ] Canvas HUD con Timer y Score
- [ ] Canvas Tendedero con 3 OrderCardUI
- [ ] Canvas Start Screen con input y botón
- [ ] Canvas Game Over con score final y botón restart
- [ ] UIManager tiene TODAS las referencias

---

## Troubleshooting

### "La tortilla no se cocina"

- Verificar que el ComalSocket tiene un collider **trigger**
- Verificar que la tortilla tiene tag `Tortilla`
- Verificar que la tortilla tiene el componente `TortillaManager`
- Verificar que la tortilla tiene `Rigidbody`

### "La carne no vuela al cortar"

- Verificar que el cuchillo tiene tag `Cuchillo`
- Verificar que MeatCutter tiene `pastorPrefab` asignado
- Verificar que `GameManager.IsGameRunning == true`
- Verificar el cooldown de corte

### "La tortilla no se convierte en taco"

- Verificar que `TacoAssembler.HasMeat == true` (la carne se asignó)
- Verificar que la tortilla está en la mano (`_isInHand == true`)
- Verificar que el `SecondaryButtonAction` está asignado y funciona
- Verificar que el `GameManager.IsGameRunning == true`
- Probar en consola: el mensaje `"Presiona botón secundario para armar taco"` debería aparecer

### "El NPC no recibe el plato"

- Verificar que el plato tiene tag `Plato`
- Verificar que el plato tiene `PlateSocket` con al menos 1 taco
- Verificar que el NPC tiene `PersonInteraction` con `_hasArrived == true`
- Verificar que el `detectionRadius` es suficiente
- Acercar más el plato al NPC

### "No aparecen los pedidos en el tendedero"

- Verificar que UIManager tiene las referencias a los `OrderCardUI`
- Verificar que UIManager se suscribe al evento `OnOrdersChanged`
- Verificar que GameManager genera los pedidos (ver consola)

### "Los NPCs no aparecen"

- Verificar que `GameManager.IsGameRunning == true`
- Verificar que `PersonSpawner` tiene `peoplePrefabs` asignados
- Verificar que `QueueManager` tiene spots libres

---

## Diagrama de Conexiones de Componentes

```
┌──────────────────────────────────────────────────────────────────────┐
│ ESCENA                                                               │
│                                                                      │
│  ┌─────────────┐    eventos     ┌──────────────┐                     │
│  │ GameManager  │──────────────→│  UIManager    │                     │
│  └──────┬──────┘                └──────────────┘                     │
│         │                                                            │
│         │ genera pedidos                                             │
│         ▼                                                            │
│  ┌─────────────┐    asigna      ┌──────────────┐                    │
│  │ TacoOrder    │──────────────→│ PersonSpawner │                    │
│  │ (datos)      │               └──────┬───────┘                    │
│  └─────────────┘                       │ crea NPCs                  │
│                                        ▼                             │
│                                ┌──────────────┐                     │
│                                │ NPC          │                      │
│                                │ ├ Controller │                      │
│                                │ └ Interaction│←─── evalúa plato    │
│                                └──────────────┘                     │
│                                                                      │
│  ┌──────────────────── COCINA ────────────────────────┐             │
│  │                                                     │             │
│  │  Comal                    Trompo        Plancha     │             │
│  │  ┌──────────┐           ┌──────────┐  ┌──────────┐│             │
│  │  │ComalSocket│           │MeatCutter│  │MeatPile  ││             │
│  │  │(x6 slots) │           │          │  │Socket    ││             │
│  │  └────┬─────┘           └────┬─────┘  └────┬─────┘│             │
│  │       │ cocina                │ lanza        │ asigna│             │
│  │       ▼                      ▼               ▼      │             │
│  │  ┌──────────────────────────────────────────────┐  │             │
│  │  │ TORTILLA                                      │  │             │
│  │  │ ├ TortillaManager (cocción)                   │  │             │
│  │  │ ├ TacoAssembler (ensamblaje)                  │  │             │
│  │  │ └ XRGrabInteractable (VR grab)               │  │             │
│  │  └───────────────────┬──────────────────────────┘  │             │
│  │                      │ botón secundario             │             │
│  │                      ▼                              │             │
│  │               ┌──────────┐                          │             │
│  │               │ TACO     │                          │             │
│  │               │ ├ TacoData│                          │             │
│  │               │ └ Grab   │                          │             │
│  │               └────┬─────┘                          │             │
│  │                    │ se coloca                       │             │
│  │                    ▼                                 │             │
│  │               ┌──────────┐                          │             │
│  │               │ PLATO    │                          │             │
│  │               │ PlateSocket│                         │             │
│  │               └──────────┘                          │             │
│  └─────────────────────────────────────────────────────┘             │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```
