# 📋 TaqueroMamón VR — Cambios Pendientes

> Creado: 2026-05-12  
> Última actualización: 2026-05-12

---

## 🔴 Prioridad Alta

---

### 1. Fijar cámara del headset (no seguir movimiento físico dentro del juego)

**Problema:** Si el jugador se mueve físicamente con los lentes Oculus en la vida real, el XR Origin se traslada dentro del juego.  
**Causa probable:** El `Tracking Origin Mode` del XR Origin está en `Floor` o `Device`, y el tracking de posición está habilitado.  
**Solución propuesta:**

- En el componente **XR Origin** (o **TrackedPoseDriver** de la cámara principal), desactivar el tracking de posición:
  - Buscar el `TrackedPoseDriver` en la Main Camera del XR Origin.
  - Cambiar **Position Input** a `None` (o deshabilitar `Track Position`).
  - Mantener habilitado solo **Rotation** si se quiere que el jugador pueda girar la cabeza.
- Alternativa por script: crear un script `LockPlayerPosition.cs` que en cada `LateUpdate` fuerce la posición del XR Origin a un punto fijo.
- **⚠️ Nota:** Esto afecta la experiencia VR. Evaluar si conviene bloquear completamente o solo limitar el área de movimiento.

**Archivos a modificar:** Inspector del XR Origin (sin cambios de código si se hace desde el Inspector).  
**Script opcional:** `LockPlayerPosition.cs` (nuevo)

---

### 2. HUD — Tacos entregados no se actualiza en tiempo real

**Problema:** El HUD de tacos entregados no refleja cambios en tiempo real.  
**Causa identificada:** `UIManager` se suscribe a `OnScoreChanged` y `OnOrdersChanged`, pero si `GameManager` aún no existe cuando `UIManager.Start()` corre, `SubscribeToEvents()` retorna sin suscribirse (`GameManager.Instance == null`). El flag `_subscribed` queda en `false`, pero no hay reintento posterior en `Update()`.  
**Solución propuesta:**

En `UIManager.cs`, agregar un reintento de suscripción en `Update()` cuando no se ha suscrito aún:

```csharp
// En UIManager.Update():
void Update()
{
    // Reintentar suscripción si aún no se logró
    if (!_subscribed)
        SubscribeToEvents();

    if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning) return;
    UpdateTimer();
}
```

También agregar un evento en `GameManager` para conteo de tacos entregados separado del score (opcional, si el HUD muestra "tacos entregados" y no solo dinero). Actualmente `GameManager` solo tiene `OnScoreChanged`, no hay un contador de tacos entregados separado. Si el HUD necesita mostrar ese dato, hay que:

1. Agregar `private int _tacosDelivered = 0;` en `GameManager.cs`
2. Incrementarlo en `OrderCompleted()` 
3. Exponer `public int TacosDelivered => _tacosDelivered;`
4. Agregar `public event Action<int> OnTacosDeliveredChanged;`
5. Conectar el evento al campo correspondiente en el HUD desde `UIManager`

**Archivos a modificar:**
- `Assets/PROYECTO/Scripts/UIManager.cs` — Reintento de suscripción en `Update()`
- `Assets/PROYECTO/Scripts/GameManager.cs` — Contador de tacos + evento (si aplica)

---

### 3. UI Menús sobrepuestos después del Game Over

**Problema:** Al terminar la partida (Game Over), los menús aparecen superpuestos (ej. `startScreen` + `gameOverScreen` visibles al mismo tiempo, o `menuBG` no se desactivó al reiniciar).  
**Causa identificada:**
- `ShowGameOver()` desactiva `startScreen` y `nameEntryScreen`, pero **no desactiva `menuBG` ni `scoreBG`**.
- Al reiniciar (`OnQuickRestartPressed` → `ShowGameHUD`), `gameOverScreen` se desactiva, pero `menuBG` puede seguir activo si quedó del estado previo.

**Solución propuesta:**

En `UIManager.cs`, hacer que `ShowGameOver()` también oculte `menuBG` y `scoreBG`:

```csharp
private void ShowGameOver(int finalScore)
{
    SetHandRays(true);

    if (startScreen    != null) startScreen.SetActive(false);
    if (nameEntryScreen!= null) nameEntryScreen.SetActive(false);
    if (hudContainer   != null) hudContainer.SetActive(false);
    if (menuBG         != null) menuBG.SetActive(false);   // ← AGREGAR
    if (scoreBG        != null) scoreBG.SetActive(false);  // ← AGREGAR
    // ... resto del código
}
```

Y en `ShowGameHUD()` también limpiar todos los overlays:

```csharp
private void ShowGameHUD()
{
    if (startScreen    != null) startScreen.SetActive(false);
    if (gameOverScreen != null) gameOverScreen.SetActive(false);
    if (nameEntryScreen!= null) nameEntryScreen.SetActive(false);
    if (menuBG         != null) menuBG.SetActive(false);   // ← AGREGAR
    if (scoreBG        != null) scoreBG.SetActive(false);  // ← AGREGAR
    if (hudContainer   != null) hudContainer.SetActive(true);
    // ...
}
```

**Archivos a modificar:**
- `Assets/PROYECTO/Scripts/UIManager.cs`

---

### 4. MenuBG no se desactiva al reiniciar el juego

**Problema:** Al volver a iniciar la partida desde Game Over (Quick Restart o al presionar Start desde el menú), el `menuBG` permanece visible en pantalla.  
**Causa:** `OnQuickRestartPressed()` llama a `ShowGameHUD()`, que no desactiva `menuBG`. Ver corrección en el punto anterior (ítem 3).  
**Solución:** Incluida en el fix del ítem 3 (agregar `menuBG.SetActive(false)` en `ShowGameHUD()`).

---

## 🟠 Prioridad Media

---

### 5. Reducir área del trigger de entrega de taco

**Problema:** El collider del área de entrega (donde el NPC recibe el plato) es demasiado grande y acepta platos desde lejos.  
**Solución propuesta:**
- En la escena, localizar el GameObject del área de entrega (probablemente hijo del NPC o zona de mostrador).
- Reducir el tamaño del `BoxCollider` o `SphereCollider` desde el Inspector.
- Sugerencia de radio si es `SphereCollider`: reducir a ~0.2–0.3 unidades.
- Si se usa `BoxCollider`, ajustar las dimensiones `X`, `Y`, `Z` al tamaño real del área de mostrador.

**Archivos a modificar:** Inspector del GameObject de la zona de entrega (sin cambio de código).

---

### 6. Restricción: no aceptar plato si no cumple el pedido

**Problema:** El NPC acepta cualquier plato, aunque el contenido no coincida con el pedido activo.  
**Causa:** No hay validación implementada en `PlateSocket.cs` ni en el script del NPC que evalúe si el plato cumple con la orden.  
**Solución propuesta:**

Crear o actualizar el script de `PersonInteraction` (o el equivalente que maneja la recepción del plato en el NPC) para validar:

```csharp
// Pseudocódigo de validación antes de aceptar el plato:
PlateSocket plate = deliveredPlate.GetComponent<PlateSocket>();
TacoOrder order = GameManager.Instance.ActiveOrders[npcOrderIndex];

bool meetsOrder = plate.TacoCount >= order.TacoCount
               && plate.CountMatchingTacos(order.MeatType) >= order.TacoCount;

if (meetsOrder)
    GameManager.Instance.OrderCompleted(order.OrderId, order.Reward);
else
    // Rechazar: mostrar feedback visual/audio, no completar pedido
    ShowRejectionFeedback();
```

- `PlateSocket` ya tiene `TacoCount` y `CountMatchingTacos(meatType)` implementados. ✅
- Solo falta la capa de validación en el receptor.
- Considerar agregar feedback visual al rechazar (texto flotante, sonido, shake de cámara, etc.)

**Archivos a modificar:**
- Script del NPC/zona de entrega (crear `PersonInteraction.cs` si no existe, o actualizar el existente)

---

### 7. Desactivar NPC al terminar el juego

**Problema:** Los NPCs siguen activos (animaciones, lógica, colliders) después de que la partida termina.  
**Solución propuesta:**

Opción A — Desde `GameManager.EndGame()`:
```csharp
// En GameManager.cs, al final de EndGame():
[SerializeField] private GameObject[] npcsToDisable;

public void EndGame()
{
    // ... código existente ...
    foreach (var npc in npcsToDisable)
        if (npc != null) npc.SetActive(false);
}
```

Opción B — Los NPCs se suscriben al evento `OnGameOver`:
```csharp
// En el script del NPC:
void OnEnable()  => GameManager.Instance.OnGameOver += OnGameOver;
void OnDisable() => GameManager.Instance.OnGameOver -= OnGameOver;

private void OnGameOver(int score) => gameObject.SetActive(false);
```

Se recomienda la **Opción B** para mantener desacoplado el `GameManager`.  
Si los NPCs forman parte de `worldActive`, simplemente hacer que `SetWorldState(false)` los desactive automáticamente (revisar jerarquía de la escena).

**Archivos a modificar:**
- `Assets/PROYECTO/Scripts/GameManager.cs` (Opción A) o script del NPC (Opción B)

---

## 🟡 Prioridad Baja / Mejoras

---

### 8. Colider de zona de entrega — achicarlo más específicamente

**Problema:** El collider de entrega (zona donde se coloca el plato para entregar) es demasiado generoso.  
**Nota:** Similar al ítem 5. Separado porque puede referirse a otro collider (el del trigger de _PlateSocket_ en el plato, no la zona del NPC).  
**Solución:**
- Reducir el trigger en el prefab del Plato si `PlateSocket` usa un collider propio.
- Asegurarse de que el rango de detección de tacos hacia el plato sea preciso (radio sugerido: ≤ 0.15 unidades).

---

## ✅ Estado de implementación

| # | Tarea | Estado | Dificultad |
|---|-------|--------|------------|
| 1 | Fijar cámara (no seguir movimiento físico) | ⏳ Pendiente | Media |
| 2 | Menús sobrepuestos post-GameOver | ✅ Implementado | Baja |
| 3 | MenuBG no se desactiva al reiniciar | ✅ Implementado | Baja |
| 4 | Reducir área del trigger de entrega | ⏳ Pendiente | Muy Baja |
| 5 | Restricción: no aceptar plato incorrecto | ⏳ Pendiente | Media |
| 6 | Desactivar NPC al terminar el juego | ✅ Implementado | Baja |
| 7 | Achitar collider de entrega (plato) | ⏳ Pendiente | Muy Baja |

---

## 📝 Notas técnicas generales

- `GameManager` usa `DontDestroyOnLoad`, lo que significa que persiste entre escenas. Si se reinicia la escena en lugar del juego, puede haber duplicados. Verificar que el singleton lo maneje bien.
- `UIManager.SubscribeToEvents()` tiene un guard de `_subscribed`, lo que evita doble suscripción, pero si `GameManager` se crea después de `UIManager.Start()`, nunca se suscribe hasta que `OnEnable` vuelva a llamarse. Agregar reintento en `Update`.
- La jerarquía de `worldActive` / `worldInactive` en `GameManager.SetWorldState()` puede ser la solución más simple para los NPCs si están bajo ese GameObject padre.
