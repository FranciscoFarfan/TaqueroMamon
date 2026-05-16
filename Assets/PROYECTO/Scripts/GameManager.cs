using System;
using System.Collections.Generic;

using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// GameManager — Singleton central del juego de tacos VR.
///
/// Responsabilidades:
///  - Alternar el estado del mundo (abierto / cerrado).
///  - Gestionar el temporizador de 3 minutos.
///  - Mantener y exponer la puntuación (dinero).
///  - Administrar los 3 pedidos activos simultáneos.
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
        DontDestroyOnLoad(gameObject);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR ─ Referencias del mundo
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Estado del mundo")]
    [Tooltip("GameObject con todo lo estático cuando el juego ESTÁ corriendo (taquería abierta).")]
    [SerializeField] private GameObject worldActive;

    [Tooltip("GameObject con todo lo estático cuando el juego NO ha iniciado (taquería cerrada).")]
    [SerializeField] private GameObject worldInactive;

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR ─ Configuración del juego
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Configuración")]
    [Tooltip("Duración de la partida en segundos (default 180 = 3 min).")]
    [SerializeField] private float gameDuration = 180f;

    [Tooltip("Número máximo de pedidos activos simultáneos.")]
    [SerializeField] private int maxActiveOrders = 3;

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
    
    private int _tacosDelivered = 0; // Nuevo contador
    
    public int TacosDelivered => _tacosDelivered; // Propiedad pública
    // Evento para notificar cambios en la cantidad de tacos
    public event Action<int> OnTacosDeliveredChanged;

    private readonly List<TacoOrder> _activeOrders = new List<TacoOrder>();

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
    /// Inicia una nueva partida.
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
        _tacosDelivered = 0; // Resetear al iniciar
        OnTacosDeliveredChanged?.Invoke(_tacosDelivered);
        _activeOrders.Clear();

        // Cambiar mundo
        SetWorldState(gameRunning: true);

        // Generar pedidos iniciales
        for (int i = 0; i < maxActiveOrders; i++)
            GenerateNewOrder();

        _isGameRunning = true;

        // Confinar el cursor dentro de la ventana del juego
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;

        OnScoreChanged?.Invoke(_score);
        OnOrdersChanged?.Invoke(ActiveOrders);

        Debug.Log($"[GameManager] Partida iniciada — Jugador: {_playerName}");
    }

    /// <summary>
    /// Termina la partida (se llama automáticamente al acabar el tiempo).
    /// También se puede llamar manualmente para forzar fin.
    /// </summary>
    public void EndGame()
    {
        if (!_isGameRunning) return;

        _isGameRunning = false;
        SetWorldState(gameRunning: false);

        // Liberar el cursor para poder usar menús
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // El guardado de score ahora es opcional vía LeaderboardManager
        // y se controla desde UIManager (solo si el jugador decide guardar)

        OnGameOver?.Invoke(_score);

        Debug.Log($"[GameManager] Partida terminada — Score final: {_score}");
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
        _score = Mathf.Max(0, _score - amount);
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

        // INCREMENTAR CONTADOR DE TACOS
        _tacosDelivered += order.TacoCount; 
        OnTacosDeliveredChanged?.Invoke(_tacosDelivered); // Notificar a la UI

        AddPoints(reward);

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
    //  API PÚBLICA ─ Penalizaciones rápidas
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Llamar desde DroppableObject (u otros scripts) para reportar
    /// una penalización directa (plato caído, tortilla quemada, etc.).
    /// </summary>
    /// <param name="penalty">Puntos a restar.</param>
    /// <param name="reason">Motivo (solo para debug).</param>
    public void ApplyPenalty(int penalty, string reason = "Objeto caído")
    {
        SubtractPoints(penalty);
        Debug.Log($"[GameManager] Penalización: {reason} → -{penalty}");
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
