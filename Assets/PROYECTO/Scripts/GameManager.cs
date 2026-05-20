using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Random = UnityEngine.Random;

/// <summary>
/// GameManager — Singleton central del juego de tacos VR.
///
/// Responsabilidades:
///  - Alternar el estado del mundo (abierto / cerrado).
///  - Gestionar el temporizador de 3 minutos.
///  - Mantener y exponer la puntuación (dinero).
///  - Administrar los 3 pedidos activos simultáneos.
///  - Llevar estadísticas detalladas de la partida.
///  - Coordinar transiciones con esperas para animaciones del sol.
///  - Guardar el puntaje final en un .txt.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  SINGLETON
    // ═══════════════════════════════════════════════════════════════════════════

    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR ─ Referencias del mundo
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Estado del mundo")]
    [Tooltip("GameObject con todo lo estático cuando el juego ESTÁ corriendo (taquería abierta).")]
    [SerializeField] private GameObject worldActive;

    [Tooltip("GameObject con todo lo estático cuando el juego NO ha iniciado (taquería cerrada).")]
    [SerializeField] private GameObject worldInactive;

    [Tooltip("(Opcional) Controlador de la iluminación ambiental. Si se asigna, hará la transición mañana↔día automáticamente.")]
    [SerializeField] private AmbientLightController ambientLightController;

    [Header("Manos del jugador")]
    [Tooltip("XRDirectInteractor de la mano izquierda (para forzar soltar objetos al terminar).")]
    [SerializeField] private XRDirectInteractor leftHandInteractor;

    [Tooltip("XRDirectInteractor de la mano derecha (para forzar soltar objetos al terminar).")]
    [SerializeField] private XRDirectInteractor rightHandInteractor;

    [Header("Cuchillo")]
    [Tooltip("(Opcional) Referencia al GameObject del cuchillo para resetearlo al terminar.")]
    [SerializeField] private GameObject knifeObject;

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR ─ Configuración del juego
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Configuración")]
    [Tooltip("Duración de la partida en segundos (default 180 = 3 min).")]
    [SerializeField] private float gameDuration = 180f;

    [Tooltip("Número máximo de pedidos activos simultáneos.")]
    [SerializeField] private int maxActiveOrders = 3;

    [Header("Tiempos de espera para animaciones")]
    [Tooltip("Segundos a esperar al INICIAR para la animación del sol (antes de generar pedidos).")]
    [SerializeField] private float startAnimationDelay = 3f;

    [Tooltip("Segundos a esperar al TERMINAR para la animación del sol (antes de mostrar Game Over).")]
    [SerializeField] private float endAnimationDelay = 5f;

    [Header("Carnes disponibles")]
    [Tooltip("Lista de tipos de carne que pueden pedirse. Ajústala a tus strings definitivos.")]
    [SerializeField] private string[] availableMeats = { "Pastor", "Bistec", "Chorizo", "Suadero", "Carnitas" };

    [Tooltip("Peso del Pastor en la selección aleatoria. 1 = igual que los demás, 2.5 = 2.5× más probable.")]
    [SerializeField] private float pastorWeight = 2.5f;

    [Header("Economía")]
    [Tooltip("Puntos base por taco (reward = tacoCount * pointsPerTaco).")]
    [SerializeField] private int pointsPerTaco = 10;

    // ═══════════════════════════════════════════════════════════════════════════
    //  ESTADO PRIVADO
    // ═══════════════════════════════════════════════════════════════════════════

    private bool   _isGameRunning  = false;
    private float  _timeRemaining  = 0f;
    private int    _score          = 0;
    private string _playerName     = "AAA";
    private int    _nextOrderId    = 0;

    // Posición original del cuchillo
    private Vector3 _knifeInitialPosition;
    private Quaternion _knifeInitialRotation;
    private Transform _knifeInitialParent;

    private readonly List<TacoOrder> _activeOrders = new List<TacoOrder>();

    // ═══════════════════════════════════════════════════════════════════════════
    //  ESTADÍSTICAS DE LA PARTIDA
    // ═══════════════════════════════════════════════════════════════════════════

    private int _tacosDelivered    = 0;
    private int _ordersCompleted   = 0;
    private int _tortillasLost     = 0;
    private int _tacosLost         = 0;
    private int _meatDropped       = 0;
    private int _platesDropped     = 0;
    private int _totalEarned       = 0;
    private int _totalPenalties    = 0;

    // ═══════════════════════════════════════════════════════════════════════════
    //  PROPIEDADES PÚBLICAS (para la UI)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>¿Hay una partida en curso?</summary>
    public bool IsGameRunning => _isGameRunning;

    /// <summary>Segundos restantes en la partida actual.</summary>
    public float TimeRemaining => _timeRemaining;

    /// <summary>Puntuación / dinero actual del jugador.</summary>
    public int Score => _score;

    /// <summary>Nombre del jugador (3 caracteres).</summary>
    public string PlayerName => _playerName;

    /// <summary>Lista de pedidos activos (solo lectura para la UI).</summary>
    public IReadOnlyList<TacoOrder> ActiveOrders => _activeOrders.AsReadOnly();

    // --- Estadísticas ---
    public int TacosDelivered  => _tacosDelivered;
    public int OrdersCompleted => _ordersCompleted;
    public int TortillasLost   => _tortillasLost;
    public int TacosLost       => _tacosLost;
    public int MeatDropped     => _meatDropped;
    public int PlatesDropped   => _platesDropped;
    public int TotalEarned     => _totalEarned;
    public int TotalPenalties  => _totalPenalties;

    // Evento para notificar cambios en la cantidad de tacos
    public event Action<int> OnTacosDeliveredChanged;

    // ═══════════════════════════════════════════════════════════════════════════
    //  EVENTOS (opcionales, para que la UI se suscriba)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Se dispara cada vez que cambia el score.</summary>
    public event Action<int> OnScoreChanged;

    /// <summary>Se dispara cuando se agrega o reemplaza un pedido.</summary>
    public event Action<IReadOnlyList<TacoOrder>> OnOrdersChanged;

    /// <summary>Se dispara cuando la partida termina, pasa el score final.</summary>
    public event Action<int> OnGameOver;

    // ═══════════════════════════════════════════════════════════════════════════
    //  UNITY LOOP
    // ═══════════════════════════════════════════════════════════════════════════

    void Start()
    {
        // Estado inicial: mostrar mundo cerrado
        SetWorldState(gameRunning: false);

        // Guardar posición original del cuchillo
        if (knifeObject != null)
        {
            _knifeInitialPosition = knifeObject.transform.position;
            _knifeInitialRotation = knifeObject.transform.rotation;
            _knifeInitialParent = knifeObject.transform.parent;
        }
    }

    void Update()
    {
        if (!_isGameRunning) return;

        _timeRemaining -= Time.deltaTime;

        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            EndGame();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA ─ Control de partida
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Inicia una nueva partida. La secuencia espera a que termine la animación
    /// del sol antes de generar pedidos y activar el temporizador.
    /// </summary>
    /// <param name="playerName">Nombre del jugador (se trunca / rellena a 3 caracteres).</param>
    public void StartGame(string playerName)
    {
        // Sanitizar nombre a exactamente 3 caracteres
        playerName = playerName.ToUpper().Trim();
        if (playerName.Length > 3) playerName = playerName.Substring(0, 3);
        while (playerName.Length < 3) playerName += "X";
        _playerName = playerName;

        // Resetear estado
        _score         = 0;
        _timeRemaining = gameDuration;
        _nextOrderId   = 0;
        _activeOrders.Clear();

        // Resetear estadísticas
        _tacosDelivered  = 0;
        _ordersCompleted = 0;
        _tortillasLost   = 0;
        _tacosLost       = 0;
        _meatDropped     = 0;
        _platesDropped   = 0;
        _totalEarned     = 0;
        _totalPenalties  = 0;

        OnTacosDeliveredChanged?.Invoke(_tacosDelivered);

        // Cambiar mundo
        SetWorldState(gameRunning: true);

        // Confinar el cursor dentro de la ventana del juego
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;

        OnScoreChanged?.Invoke(_score);
        OnOrdersChanged?.Invoke(ActiveOrders);

        Debug.Log($"[GameManager] Partida iniciada — Jugador: {_playerName}");

        // Iniciar secuencia con espera para la animación del sol
        StartCoroutine(StartGameSequence());
    }

    /// <summary>
    /// Secuencia de inicio: transicionar la luz, esperar la animación del sol,
    /// y luego generar pedidos e iniciar el temporizador.
    /// </summary>
    private IEnumerator StartGameSequence()
    {
        // Transicionar la iluminación a ambiente de DÍA
        if (ambientLightController != null)
            ambientLightController.TransitionToDay();

        // Cambiar audio ambiental a fase de tarde (juego)
        if (AmbientSoundManager.Instance != null)
            AmbientSoundManager.Instance.SetAfternoonPhase();

        // Esperar la animación del sol
        yield return new WaitForSeconds(startAnimationDelay);

        // Generar pedidos iniciales
        for (int i = 0; i < maxActiveOrders; i++)
            GenerateNewOrder();

        OnOrdersChanged?.Invoke(ActiveOrders);

        // AHORA sí empieza el juego
        _isGameRunning = true;

        Debug.Log($"[GameManager] Animación de inicio completada — ¡El juego comienza!");
    }

    /// <summary>
    /// Termina la partida. Espera la animación del sol antes de disparar el evento
    /// de Game Over para que la UI muestre los resultados.
    /// </summary>
    public void EndGame()
    {
        if (!_isGameRunning) return;

        _isGameRunning = false;

        // Forzar que el jugador suelte todo lo que tenga en las manos
        ForceReleaseHeldObjects();

        // Resetear el cuchillo a su posición original
        ResetKnife();

        Debug.Log($"[GameManager] Partida terminando — Esperando animación del sol ({endAnimationDelay}s)...");

        // Iniciar secuencia de fin con espera
        StartCoroutine(EndGameSequence());
    }

    /// <summary>
    /// Secuencia de fin: transicionar la luz a noche, esperar la animación,
    /// y luego mostrar el resultado.
    /// </summary>
    private IEnumerator EndGameSequence()
    {
        // Transicionar la iluminación a ambiente de NOCHE
        if (ambientLightController != null)
            ambientLightController.TransitionToNight();

        // Cambiar audio ambiental a fase de noche
        if (AmbientSoundManager.Instance != null)
            AmbientSoundManager.Instance.SetNightPhase();

        // Esperar la animación del sol
        yield return new WaitForSeconds(endAnimationDelay);

        // Desactivar preventivamente las pantallas del menú antes de que se active el worldInactive
        if (UIManager.Instance != null)
        {
            UIManager.Instance.PrepareForGameOver();
        }

        SetWorldState(gameRunning: false);

        // Liberar el cursor para poder usar menús
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        OnGameOver?.Invoke(_score);

        Debug.Log($"[GameManager] Partida terminada — Score final: {_score} (Ganado: ${_totalEarned}, Perdido: ${_totalPenalties})");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA ─ Puntuación
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Agrega puntos al score y notifica.</summary>
    public void AddPoints(int amount)
    {
        if (amount <= 0) return;
        _score += amount;
        OnScoreChanged?.Invoke(_score);
        Debug.Log($"[GameManager] +{amount} puntos → Total: {_score}");
    }

    /// <summary>Resta puntos al score (no baja de 0) y notifica.</summary>
    public void SubtractPoints(int amount)
    {
        if (amount <= 0) return;
        _score -= amount; // Se permite bajar de 0 para que las deudas/mermas se reflejen correctamente
        OnScoreChanged?.Invoke(_score);
        Debug.Log($"[GameManager] -{amount} puntos → Total: {_score}");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA ─ Pedidos
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Llamar desde el script del NPC cuando evaluó el plato y es correcto.
    /// Agrega la recompensa y reemplaza ese pedido por uno nuevo.
    /// </summary>
    /// <param name="orderId">ID del pedido completado.</param>
    /// <param name="reward">Puntos que paga el NPC.</param>
    public void OrderCompleted(int orderId, int reward)
    {
        int index = _activeOrders.FindIndex(o => o != null && o.OrderId == orderId);
        if (index == -1) return;

        TacoOrder order = _activeOrders[index];
        order.Complete();

        // INCREMENTAR CONTADORES
        _tacosDelivered += order.TacoCount;
        _ordersCompleted++;
        _totalEarned += reward;
        OnTacosDeliveredChanged?.Invoke(_tacosDelivered);

        AddPoints(reward);

        // Mostrar notificación de ganancia en verde en el HUD del mundo
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast($"¡Pedido Entregado!: +${reward}", 3f, false);
        }

        if (_isGameRunning)
        {
            _activeOrders[index] = CreateNewOrder();
        }
        else
        {
            _activeOrders[index] = null;
        }

        OnOrdersChanged?.Invoke(ActiveOrders);
    }

    /// <summary>
    /// Llamar desde el script del NPC cuando el plato fue incorrecto.
    /// Opcional: por ahora solo elimina el pedido sin penalizar (puedes ajustar).
    /// </summary>
    /// <param name="orderId">ID del pedido fallido.</param>
    public void OrderFailed(int orderId)
    {
        int index = _activeOrders.FindIndex(o => o != null && o.OrderId == orderId);
        if (index == -1) return;

        if (_isGameRunning)
        {
            _activeOrders[index] = CreateNewOrder();
        }
        else
        {
            _activeOrders[index] = null;
        }

        OnOrdersChanged?.Invoke(ActiveOrders);

        Debug.Log($"[GameManager] Pedido #{orderId} fallido / rechazado.");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA ─ Reportes de estadísticas (para DroppableObject, etc.)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Reporta una tortilla perdida (caída o quemada).</summary>
    public void ReportTortillaLost(int penalty, string reason = "Tortilla perdida")
    {
        _tortillasLost++;
        _totalPenalties += penalty;
        SubtractPoints(penalty);

        if (UIManager.Instance != null)
            UIManager.Instance.ShowToast($"{reason}: -${penalty}");

        Debug.Log($"[GameManager] Stat: Tortilla perdida ({reason}). Total tortillas perdidas: {_tortillasLost}");
    }

    /// <summary>Reporta tacos perdidos (caídos solos, en plato, o merma).</summary>
    public void ReportTacosLost(int count, int penalty, string reason = "Taco perdido")
    {
        _tacosLost += count;
        _totalPenalties += penalty;
        SubtractPoints(penalty);

        if (UIManager.Instance != null)
            UIManager.Instance.ShowToast($"{reason}: -${penalty}");

        Debug.Log($"[GameManager] Stat: {count} taco(s) perdido(s) ({reason}). Total tacos perdidos: {_tacosLost}");
    }

    /// <summary>Reporta carne de pastor caída al suelo.</summary>
    public void ReportMeatDropped(int penalty, string reason = "Pastor caído")
    {
        _meatDropped++;
        _totalPenalties += penalty;
        SubtractPoints(penalty);

        if (UIManager.Instance != null)
            UIManager.Instance.ShowToast($"{reason}: -${penalty}");

        Debug.Log($"[GameManager] Stat: Pastor caído. Total pastor caído: {_meatDropped}");
    }

    /// <summary>Reporta un plato caído (incluyendo los tacos que traía).</summary>
    public void ReportPlateDropped(int tacosOnPlate, int penalty, string reason = "Plato caído")
    {
        _platesDropped++;
        _tacosLost += tacosOnPlate; // Los tacos en el plato también se pierden
        _totalPenalties += penalty;
        SubtractPoints(penalty);

        if (UIManager.Instance != null)
            UIManager.Instance.ShowToast($"{reason}: -${penalty}");

        Debug.Log($"[GameManager] Stat: Plato caído con {tacosOnPlate} taco(s). Total platos caídos: {_platesDropped}");
    }

    /// <summary>Reporta tacos de merma (extras en un pedido).</summary>
    public void ReportWasteTacos(int wasteCount, int penalty)
    {
        _tacosLost += wasteCount;
        _totalPenalties += penalty;
        SubtractPoints(penalty);

        if (UIManager.Instance != null)
            UIManager.Instance.ShowToast($"Merma de tacos ({wasteCount}): -${penalty}");

        Debug.Log($"[GameManager] Stat: Merma de {wasteCount} taco(s). Total tacos perdidos: {_tacosLost}");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA ─ Penalizaciones rápidas (genérica, para casos no categorizados)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Llamar desde DroppableObject (u otros scripts) para reportar
    /// una penalización directa (plato caído, tortilla quemada, etc.).
    /// </summary>
    /// <param name="penalty">Puntos a restar.</param>
    /// <param name="reason">Motivo (solo para debug).</param>
    public void ApplyPenalty(int penalty, string reason = "Objeto caído")
    {
        _totalPenalties += penalty;
        SubtractPoints(penalty);
        Debug.Log($"[GameManager] Penalización: {reason} → -{penalty}");

        // Mostrar notificación visual en la interfaz/HUD del jugador
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast($"{reason}: -${penalty}");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA ─ Limpieza de objetos de gameplay
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Destruye todos los objetos de gameplay sueltos en la escena
    /// (tortillas, trozos de pastor, tacos, platos).
    /// Se usa para reiniciar la partida sin recargar la escena.
    /// </summary>
    public void ResetGameplayObjects()
    {
        string[] gameplayTags = { "Tortilla", "Pastor", "taco", "Plato" };

        foreach (string tag in gameplayTags)
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in objects)
            {
                Destroy(obj);
            }
            if (objects.Length > 0)
                Debug.Log($"[GameManager] Limpieza: {objects.Length} objetos con tag '{tag}' destruidos.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  PRIVADO ─ Forzar liberación de objetos en las manos
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fuerza a ambas manos a soltar cualquier objeto que estén sosteniendo.
    /// Se llama al terminar la partida para que el jugador no se quede con objetos.
    /// </summary>
    private void ForceReleaseHeldObjects()
    {
        ReleaseInteractor(leftHandInteractor);
        ReleaseInteractor(rightHandInteractor);
    }

    private void ReleaseInteractor(XRDirectInteractor interactor)
    {
        if (interactor == null) return;
        if (!interactor.hasSelection) return;

        // Copiar la lista porque SelectExit la modifica
        var selected = new List<IXRSelectInteractable>(interactor.interactablesSelected);
        foreach (var interactable in selected)
        {
            if (interactor.interactionManager != null)
            {
                interactor.interactionManager.SelectExit(interactor, interactable);
                Debug.Log($"[GameManager] Forzado soltar: {interactable}");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  PRIVADO ─ Cuchillo
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Resetea el cuchillo a su posición original.</summary>
    private void ResetKnife()
    {
        if (knifeObject == null) return;

        // Desactivar física temporal
        Rigidbody knifeRb = knifeObject.GetComponent<Rigidbody>();
        if (knifeRb != null)
        {
            knifeRb.velocity = Vector3.zero;
            knifeRb.angularVelocity = Vector3.zero;
            knifeRb.isKinematic = true;
        }

        // Restaurar parent original
        knifeObject.transform.SetParent(_knifeInitialParent);

        // Restaurar posición y rotación
        knifeObject.transform.position = _knifeInitialPosition;
        knifeObject.transform.rotation = _knifeInitialRotation;

        // Reactivar física
        if (knifeRb != null)
            knifeRb.isKinematic = false;

        Debug.Log("[GameManager] Cuchillo reseteado a su posición original.");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  PRIVADO ─ Lógica interna
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Activa/desactiva los objetos del mundo según el estado del juego.</summary>
    private void SetWorldState(bool gameRunning)
    {
        if (worldActive   != null) worldActive.SetActive(gameRunning);
        if (worldInactive != null) worldInactive.SetActive(!gameRunning);
    }

    /// <summary>Genera un nuevo pedido aleatorio y lo agrega a la lista activa.</summary>
    private TacoOrder CreateNewOrder()
    {
        int    tacoCount = Random.Range(1, 6);          // 1–5 tacos
        string meat      = PickWeightedMeat();
        int    reward    = tacoCount * pointsPerTaco;

        TacoOrder newOrder = new TacoOrder(_nextOrderId++, tacoCount, meat, reward);
        Debug.Log($"[GameManager] Nuevo pedido #{newOrder.OrderId}: {tacoCount}x {meat} (${reward})");
        return newOrder;
    }

    private void GenerateNewOrder()
    {
        if (_activeOrders.Count >= maxActiveOrders) return;
        _activeOrders.Add(CreateNewOrder());
    }

    /// <summary>
    /// Selecciona una carne con peso ponderado.
    /// Pastor tiene más probabilidad (pastorWeight). Intenta no repetir carne
    /// ya activa en otro pedido (hasta 3 intentos).
    /// </summary>
    private string PickWeightedMeat()
    {
        string chosen = null;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            chosen = SampleWeightedMeat();
            // Si no hay duplicado entre pedidos activos, o ya no quedan opciones únicas, aceptar
            bool alreadyActive = _activeOrders.Exists(o => o != null && o.MeatType == chosen);
            if (!alreadyActive || _activeOrders.Count >= availableMeats.Length)
                break;
        }
        return chosen;
    }

    /// <summary>
    /// Samplea una carne con probabilidad ponderada:
    /// "Pastor" tiene peso <c>pastorWeight</c>, las demás tienen peso 1.
    /// </summary>
    private string SampleWeightedMeat()
    {
        float totalWeight = 0f;
        foreach (string m in availableMeats)
            totalWeight += (m == "Pastor") ? pastorWeight : 1f;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (string m in availableMeats)
        {
            cumulative += (m == "Pastor") ? pastorWeight : 1f;
            if (roll <= cumulative)
                return m;
        }
        return availableMeats[availableMeats.Length - 1];
    }

}
