using System;
using System.Collections.Generic;
using System.IO;
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
        _activeOrders.Clear();

        // Cambiar mundo
        SetWorldState(gameRunning: true);

        // Generar pedidos iniciales
        for (int i = 0; i < maxActiveOrders; i++)
            GenerateNewOrder();

        _isGameRunning = true;

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

        SaveScore();

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
        TacoOrder order = _activeOrders.Find(o => o.OrderId == orderId);
        if (order == null)
        {
            Debug.LogWarning($"[GameManager] OrderCompleted: no existe pedido con ID {orderId}");
            return;
        }

        order.Complete();
        _activeOrders.Remove(order);

        AddPoints(reward);

        // Reemplazar con un pedido nuevo (sólo si el juego sigue corriendo)
        if (_isGameRunning)
            GenerateNewOrder();

        OnOrdersChanged?.Invoke(ActiveOrders);

        Debug.Log($"[GameManager] Pedido #{orderId} completado. Recompensa: {reward}");
    }

    /// <summary>
    /// Llamar desde el script del NPC cuando el plato fue incorrecto.
    /// Opcional: por ahora solo elimina el pedido sin penalizar (puedes ajustar).
    /// </summary>
    /// <param name="orderId">ID del pedido fallido.</param>
    public void OrderFailed(int orderId)
    {
        TacoOrder order = _activeOrders.Find(o => o.OrderId == orderId);
        if (order == null) return;

        _activeOrders.Remove(order);

        if (_isGameRunning)
            GenerateNewOrder();

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
    private void GenerateNewOrder()
    {
        if (_activeOrders.Count >= maxActiveOrders) return;

        int    tacoCount = Random.Range(1, 6);          // 1–5 tacos
        string meat      = availableMeats[Random.Range(0, availableMeats.Length)];
        int    reward    = tacoCount * pointsPerTaco;

        TacoOrder newOrder = new TacoOrder(_nextOrderId++, tacoCount, meat, reward);
        _activeOrders.Add(newOrder);

        Debug.Log($"[GameManager] Nuevo pedido #{newOrder.OrderId}: {tacoCount}x {meat} (${reward})");
    }

    /// <summary>Guarda nombre y puntuación en un archivo .txt.</summary>
    private void SaveScore()
    {
        string path      = Path.Combine(Application.persistentDataPath, "scores.txt");
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string line      = $"{timestamp} | {_playerName} | {_score} pts\n";

        try
        {
            File.AppendAllText(path, line);
            Debug.Log($"[GameManager] Score guardado en: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameManager] Error al guardar score: {e.Message}");
        }
    }
}
