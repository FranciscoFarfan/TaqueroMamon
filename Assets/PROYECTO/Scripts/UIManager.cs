using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UIManager — Gestiona toda la UI del juego de tacos VR.
///
/// Se suscribe a los eventos del GameManager para actualizar:
///   - HUD: Timer y Score
///   - Tendedero: Tarjetas de pedidos
///   - Pantallas: Inicio y Game Over
///
/// Configuración:
///   1. Colocar en el Canvas principal de la escena.
///   2. Asignar todas las referencias de UI en el Inspector.
///   3. El Canvas puede ser WorldSpace (para VR) o ScreenSpace.
/// </summary>
public class UIManager : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  SINGLETON (opcional, para acceso global)
    // ═══════════════════════════════════════════════════════════════════════════

    public static UIManager Instance { get; private set; }

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR — HUD
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("HUD — Timer")]
    [Tooltip("Texto del temporizador. Formato: MM:SS")]
    [SerializeField] private TMP_Text timerText;

    [Tooltip("Color del timer cuando queda poco tiempo (< 30s).")]
    [SerializeField] private Color timerUrgentColor = Color.red;

    [Tooltip("Color normal del timer.")]
    [SerializeField] private Color timerNormalColor = Color.white;

    [Header("HUD — Score")]
    [Tooltip("Texto del puntaje/dinero del jugador.")]
    [SerializeField] private TMP_Text scoreText;

    [Header("HUD — Contenedor")]
    [Tooltip("GameObject padre del HUD (para mostrar/ocultar).")]
    [SerializeField] private GameObject hudContainer;

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR — TENDEDERO DE PEDIDOS
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Tendedero de pedidos")]
    [Tooltip("Las 3 tarjetas de pedido en el tendedero (OrderCardUI).")]
    [SerializeField] private OrderCardUI[] orderCards;

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR — PANTALLA DE INICIO
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Pantalla de inicio")]
    [Tooltip("Contenedor de la pantalla de inicio.")]
    [SerializeField] private GameObject startScreen;

    [Tooltip("Input para el nombre del jugador (3 caracteres).")]
    [SerializeField] private TMP_InputField playerNameInput;

    [Tooltip("Botón para iniciar la partida.")]
    [SerializeField] private Button startButton;

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR — PANTALLA DE GAME OVER
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Pantalla de Game Over")]
    [Tooltip("Contenedor de la pantalla de game over.")]
    [SerializeField] private GameObject gameOverScreen;

    [Tooltip("Texto que muestra el puntaje final.")]
    [SerializeField] private TMP_Text finalScoreText;

    [Tooltip("Texto que muestra el nombre del jugador.")]
    [SerializeField] private TMP_Text playerNameText;

    [Tooltip("Botón para reiniciar (volver a la pantalla de inicio).")]
    [SerializeField] private Button restartButton;

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR — AUDIO
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Audio (opcional)")]
    [Tooltip("Sonido al recibir puntos.")]
    [SerializeField] private AudioClip scoreUpSound;

    [Tooltip("Sonido de game over.")]
    [SerializeField] private AudioClip gameOverSound;

    [Tooltip("Sonido de cuenta regresiva (últimos 10 seg).")]
    [SerializeField] private AudioClip countdownTickSound;

    // ═══════════════════════════════════════════════════════════════════════════
    //  ESTADO PRIVADO
    // ═══════════════════════════════════════════════════════════════════════════

    private AudioSource _audioSource;
    private int _lastCountdownSecond = -1;
    private int _previousScore = 0;

    // ═══════════════════════════════════════════════════════════════════════════
    //  UNITY
    // ═══════════════════════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
    }

    void OnEnable()
    {
        // Suscribirse a los eventos del GameManager (puede que aún no exista en Awake)
        SubscribeToEvents();
    }

    void Start()
    {
        // Intentar suscribirse de nuevo por si el GameManager se creó después
        SubscribeToEvents();

        // Configurar botones
        if (startButton != null)
            startButton.onClick.AddListener(OnStartButtonPressed);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartButtonPressed);

        // Limitar el input del nombre a 3 caracteres
        if (playerNameInput != null)
            playerNameInput.characterLimit = 3;

        // Estado inicial: mostrar pantalla de inicio
        ShowStartScreen();
    }

    void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning) return;

        UpdateTimer();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  SUSCRIPCIONES
    // ═══════════════════════════════════════════════════════════════════════════

    private bool _subscribed = false;

    private void SubscribeToEvents()
    {
        if (_subscribed) return;
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnScoreChanged += UpdateScore;
        GameManager.Instance.OnOrdersChanged += UpdateOrders;
        GameManager.Instance.OnGameOver += ShowGameOver;

        _subscribed = true;
    }

    private void UnsubscribeFromEvents()
    {
        if (!_subscribed) return;
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnScoreChanged -= UpdateScore;
        GameManager.Instance.OnOrdersChanged -= UpdateOrders;
        GameManager.Instance.OnGameOver -= ShowGameOver;

        _subscribed = false;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ACTUALIZACIÓN DEL HUD
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Actualiza el texto del temporizador cada frame.</summary>
    private void UpdateTimer()
    {
        if (timerText == null || GameManager.Instance == null) return;

        float time = GameManager.Instance.TimeRemaining;
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);

        timerText.text = $"{minutes:00}:{seconds:00}";

        // Cambiar color cuando queda poco tiempo
        timerText.color = time <= 30f ? timerUrgentColor : timerNormalColor;

        // Efecto de cuenta regresiva en los últimos 10 segundos
        if (time <= 10f && countdownTickSound != null)
        {
            int currentSecond = Mathf.FloorToInt(time);
            if (currentSecond != _lastCountdownSecond && currentSecond >= 0)
            {
                _lastCountdownSecond = currentSecond;
                _audioSource.PlayOneShot(countdownTickSound);
            }
        }
    }

    /// <summary>Actualiza el texto del score cuando cambia.</summary>
    private void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"${score}";

        // Efecto de sonido si subió el score
        if (score > _previousScore && scoreUpSound != null && _audioSource != null)
            _audioSource.PlayOneShot(scoreUpSound);

        _previousScore = score;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ACTUALIZACIÓN DEL TENDEDERO
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Actualiza las tarjetas del tendedero con los pedidos activos.</summary>
    private void UpdateOrders(IReadOnlyList<TacoOrder> orders)
    {
        if (orderCards == null) return;

        for (int i = 0; i < orderCards.Length; i++)
        {
            if (orderCards[i] == null) continue;

            if (i < orders.Count)
                orderCards[i].SetOrder(orders[i]);
            else
                orderCards[i].ClearOrder();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  PANTALLAS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Muestra la pantalla de inicio.</summary>
    private void ShowStartScreen()
    {
        if (startScreen != null) startScreen.SetActive(true);
        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        if (hudContainer != null) hudContainer.SetActive(false);

        // Limpiar las tarjetas del tendedero
        if (orderCards != null)
        {
            foreach (OrderCardUI card in orderCards)
            {
                if (card != null) card.ClearOrder();
            }
        }
    }

    /// <summary>Muestra el HUD durante la partida.</summary>
    private void ShowGameHUD()
    {
        if (startScreen != null) startScreen.SetActive(false);
        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        if (hudContainer != null) hudContainer.SetActive(true);

        _lastCountdownSecond = -1;
        _previousScore = 0;
    }

    /// <summary>Muestra la pantalla de game over con el score final.</summary>
    private void ShowGameOver(int finalScore)
    {
        if (startScreen != null) startScreen.SetActive(false);
        if (gameOverScreen != null) gameOverScreen.SetActive(true);
        if (hudContainer != null) hudContainer.SetActive(false);

        if (finalScoreText != null)
            finalScoreText.text = $"${finalScore}";

        if (playerNameText != null && GameManager.Instance != null)
            playerNameText.text = GameManager.Instance.PlayerName;

        // Audio de game over
        if (gameOverSound != null && _audioSource != null)
            _audioSource.PlayOneShot(gameOverSound);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  BOTONES
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Callback del botón "Iniciar".</summary>
    private void OnStartButtonPressed()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[UIManager] No se encontró GameManager en la escena.");
            return;
        }

        string playerName = "AAA";
        if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
            playerName = playerNameInput.text;

        ShowGameHUD();
        GameManager.Instance.StartGame(playerName);
    }

    /// <summary>Callback del botón "Reiniciar" en la pantalla de game over.</summary>
    private void OnRestartButtonPressed()
    {
        ShowStartScreen();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA — Para otros scripts
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Muestra un mensaje temporal en el HUD (para feedback como "¡Tortilla quemada!").
    /// </summary>
    public void ShowToast(string message, float duration = 2f)
    {
        // Implementación simple por ahora vía Debug
        // TODO: Agregar un texto temporal en el HUD
        Debug.Log($"[UIManager] Toast: {message}");
    }

    /// <summary>
    /// Fuerza la actualización de los pedidos (útil cuando se llama manualmente).
    /// </summary>
    public void RefreshOrders()
    {
        if (GameManager.Instance != null)
            UpdateOrders(GameManager.Instance.ActiveOrders);
    }
}
