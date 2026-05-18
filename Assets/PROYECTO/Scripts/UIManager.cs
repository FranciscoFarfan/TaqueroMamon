using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// UIManager — Gestiona toda la UI del juego de tacos VR.
///
/// Se suscribe a los eventos del GameManager para actualizar:
///   - HUD: Timer y Score
///   - Tendedero: Tarjetas de pedidos
///   - Pantallas: Inicio, Game Over, Name Entry, Leaderboard
///
/// Flujo: MenuBG → (teletransporta a zona de juego) → JUEGA
///        → GameOver (teletransporta a zona de game over) → NAMEENTRY → Score
///
/// El jugador es teletransportado a puntos de referencia en la escena
/// al iniciar la partida y al terminarla.
/// El Name Entry usa 3 caracteres con flechas ↑↓ tipo arcade.
///
/// Configuración:
///   1. Colocar en el Canvas principal de la escena.
///   2. Asignar todas las referencias de UI en el Inspector.
///   3. Asignar el XR Origin y los puntos de teletransporte.
/// </summary>
public class UIManager : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  SINGLETON & STATIC RELOAD STATE
    // ═══════════════════════════════════════════════════════════════════════════

    public static UIManager Instance { get; private set; }

    public static bool shouldStartGameOnLoad = false;
    public static string restartPlayerName = "AAA";
    public static bool shouldShowScoresOnLoad = false;

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR — TELETRANSPORTE
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Teletransporte")]
    [Tooltip("Transform del XR Origin (el objeto raíz del jugador que se teletransporta).")]
    [SerializeField] private Transform xrOrigin;

    [Tooltip("Punto de destino al iniciar la partida (zona de juego).")]
    [SerializeField] private Transform teleportStartPoint;

    [Tooltip("Punto de destino al terminar la partida (zona de Game Over).")]
    [SerializeField] private Transform teleportGameOverPoint;

    [Tooltip("Punto de destino para el menú principal (zona del MenuBG). Si es null, no se teletransporta al menú.")]
    [SerializeField] private Transform teleportMenuPoint;

    [Header("Rayos de manos")]
    [Tooltip("GameObject del rayo de la mano izquierda (se desactiva al jugar, se activa en menús).")]
    [SerializeField] private GameObject leftHandRay;

    [Tooltip("GameObject del rayo de la mano derecha (se desactiva al jugar, se activa en menús).")]
    [SerializeField] private GameObject rightHandRay;

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

    [Tooltip("Texto TMP en la tarjeta general del mundo para mostrar el motivo de la penalización/ganancia.")]
    [SerializeField] private TMP_Text scoreFeedbackText;

    [Tooltip("Color del texto para las notificaciones de ganancia (verde por defecto).")]
    [SerializeField] private Color gainColor = Color.green;

    [Tooltip("Color del texto para las notificaciones de pérdidas/penalizaciones (rojo por defecto).")]
    [SerializeField] private Color lossColor = Color.red;

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
    //  INSPECTOR — MENÚ PRINCIPAL (MenuBG / ScoreBG)
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Menú principal")]
    [Tooltip("Contenedor del menú principal (MenuBG).")]
    [SerializeField] private GameObject menuBG;

    [Tooltip("Contenedor del leaderboard (ScoreBG).")]
    [SerializeField] private GameObject scoreBG;

    [Tooltip("Botón en MenuBG para ir a ScoreBG.")]
    [SerializeField] private Button viewScoresButton;

    [Tooltip("Botón en MenuBG para salir del juego.")]
    [SerializeField] private Button quitButton;

    [Tooltip("Botón en ScoreBG para volver a MenuBG.")]
    [SerializeField] private Button backToMenuButton;

    [Tooltip("Texto TMP donde se muestra el top 10 (dentro de ScoreBG).")]
    [SerializeField] private TMP_Text leaderboardText;

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR — PANTALLA DE INICIO
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Pantalla de inicio")]
    [Tooltip("Contenedor de la pantalla de inicio (StartBG o startScreen).")]
    [SerializeField] private GameObject startScreen;

    [Tooltip("Botón para iniciar la partida.")]
    [SerializeField] private Button startButton;

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR — GAME OVER (Canvas WorldSpace separado)
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Game Over")]
    [Tooltip("Canvas/GameObject del Game Over (WorldSpace separado, se reposiciona frente a cámara).")]
    [SerializeField] private GameObject gameOverScreen;

    [Tooltip("Texto que muestra el puntaje final.")]
    [SerializeField] private TMP_Text finalScoreText;

    [Tooltip("Mensaje que aparece si el jugador entró al top 10.")]
    [SerializeField] private GameObject topTenMessage;

    [Tooltip("Botón para guardar el puntaje (solo visible si es top 10).")]
    [SerializeField] private Button saveScoreButton;

    [Tooltip("Botón de restart rápido.")]
    [SerializeField] private Button quickRestartButton;

    [Tooltip("Botón para ir al menú principal.")]
    [SerializeField] private Button gameOverMenuButton;

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR — NAME ENTRY (Canvas WorldSpace separado)
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Name Entry (Top 10)")]
    [Tooltip("Canvas/GameObject del name entry (WorldSpace separado, se posiciona frente a cámara).")]
    [SerializeField] private GameObject nameEntryScreen;

    [Tooltip("Los 3 textos TMP para cada carácter del nombre (array de 3).")]
    [SerializeField] private TMP_Text[] nameChars;

    [Tooltip("Los 3 botones ↑ para cada carácter (array de 3).")]
    [SerializeField] private Button[] charUpButtons;

    [Tooltip("Los 3 botones ↓ para cada carácter (array de 3).")]
    [SerializeField] private Button[] charDownButtons;

    [Tooltip("Botón OK para confirmar el nombre y guardar.")]
    [SerializeField] private Button nameOkButton;

    [Tooltip("Botón para cancelar e ir al menú.")]
    [SerializeField] private Button nameMenuButton;

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR — AUDIO
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Audio (opcional)")]
    [Tooltip("Música de fondo para el menú principal.")]
    [SerializeField] private AudioClip menuMusic;

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

    // Name entry state
    private char[] _currentNameChars = { 'A', 'A', 'A' };
    private int _pendingScore = 0;

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

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void OnEnable()
    {
        SubscribeToEvents();
    }

    void Start()
    {
        // Suscribirse de nuevo por si el GameManager se creó después
        SubscribeToEvents();

        // Configurar botones — Inicio
        if (startButton != null)
            startButton.onClick.AddListener(OnStartButtonPressed);

        // Configurar botones — Game Over
        if (quickRestartButton != null)
            quickRestartButton.onClick.AddListener(OnQuickRestartPressed);
        if (saveScoreButton != null)
            saveScoreButton.onClick.AddListener(OnSaveScorePressed);
        if (gameOverMenuButton != null)
            gameOverMenuButton.onClick.AddListener(OnGameOverMenuPressed);

        // Configurar botones — Name Entry
        if (nameOkButton != null)
            nameOkButton.onClick.AddListener(OnNameOkPressed);
        if (nameMenuButton != null)
            nameMenuButton.onClick.AddListener(OnNameMenuPressed);

        // Configurar botones de flechas ↑↓
        SetupCharButtons();

        // Configurar botones — Menú / Scores toggle
        if (viewScoresButton != null)
            viewScoresButton.onClick.AddListener(OnViewScoresPressed);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitButtonPressed);
        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(OnBackToMenuPressed);

        // Manejar el estado según si venimos de un reload de escena
        if (shouldStartGameOnLoad)
        {
            shouldStartGameOnLoad = false;
            string playerName = restartPlayerName;

            // Teletransportar al jugador a la zona de juego
            TeleportPlayer(teleportStartPoint);
            ShowGameHUD();
            GameManager.Instance.StartGame(playerName);
        }
        else if (shouldShowScoresOnLoad)
        {
            shouldShowScoresOnLoad = false;
            ShowStartScreen();

            // Desactivar startScreen para evitar que se superponga con scoreBG (no deben haber dos interfaces activas)
            if (startScreen != null) startScreen.SetActive(false);
            if (menuBG != null) menuBG.SetActive(false);
            if (scoreBG != null) scoreBG.SetActive(true);
            TeleportPlayer(teleportMenuPoint);
        }
        else
        {
            ShowStartScreen();
            TeleportPlayer(teleportMenuPoint);
        }
    }

    void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    void Update()
    {
        // Reintentar suscripción si GameManager arrancó después que UIManager
        if (!_subscribed)
            SubscribeToEvents();

        if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning) return;

        UpdateTimer();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  SETUP DE BOTONES DE FLECHAS
    // ═══════════════════════════════════════════════════════════════════════════

    private void SetupCharButtons()
    {
        if (charUpButtons != null)
        {
            for (int i = 0; i < charUpButtons.Length && i < 3; i++)
            {
                if (charUpButtons[i] == null) continue;
                int index = i; // Capturar para el closure
                charUpButtons[i].onClick.AddListener(() => CycleChar(index, 1));
            }
        }

        if (charDownButtons != null)
        {
            for (int i = 0; i < charDownButtons.Length && i < 3; i++)
            {
                if (charDownButtons[i] == null) continue;
                int index = i;
                charDownButtons[i].onClick.AddListener(() => CycleChar(index, -1));
            }
        }
    }

    /// <summary>Incrementa o decrementa un carácter del nombre (A-Z circular).</summary>
    private void CycleChar(int charIndex, int direction)
    {
        if (charIndex < 0 || charIndex >= 3) return;

        int current = _currentNameChars[charIndex] - 'A';
        current = (current + direction + 26) % 26; // Wrap circular A-Z
        _currentNameChars[charIndex] = (char)('A' + current);

        RefreshNameDisplay();
    }

    /// <summary>Actualiza los textos visuales del name entry.</summary>
    private void RefreshNameDisplay()
    {
        if (nameChars == null) return;
        for (int i = 0; i < nameChars.Length && i < 3; i++)
        {
            if (nameChars[i] != null)
                nameChars[i].text = _currentNameChars[i].ToString();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  SUSCRIPCIONES
    // ═══════════════════════════════════════════════════════════════════════════

    private bool _subscribed = false;

    private void SubscribeToEvents()
    {
        if (_subscribed) return;
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnScoreChanged  += UpdateScore;
        GameManager.Instance.OnOrdersChanged += UpdateOrders;
        GameManager.Instance.OnGameOver      += ShowGameOver;

        _subscribed = true;
    }

    private void UnsubscribeFromEvents()
    {
        if (!_subscribed) return;
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnScoreChanged  -= UpdateScore;
        GameManager.Instance.OnOrdersChanged -= UpdateOrders;
        GameManager.Instance.OnGameOver      -= ShowGameOver;

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
    //  TELETRANSPORTE DEL JUGADOR
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Teletransporta al jugador (XR Origin) a la posición y rotación
    /// del Transform de destino indicado.
    /// </summary>
    private void TeleportPlayer(Transform destination)
    {
        if (xrOrigin == null || destination == null)
        {
            if (xrOrigin == null)
                Debug.LogWarning("[UIManager] xrOrigin no asignado, no se puede teletransportar.");
            if (destination == null)
                Debug.LogWarning("[UIManager] Destino de teletransporte no asignado.");
            return;
        }

        // Desactivar el CharacterController si existe (evita que bloquee el teletransporte)
        CharacterController cc = xrOrigin.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        xrOrigin.position = destination.position;
        xrOrigin.rotation = destination.rotation;

        // Reactivar el CharacterController
        if (cc != null) cc.enabled = true;

        Debug.Log($"[UIManager] Jugador teletransportado a {destination.name}");
    }

    /// <summary>Activa o desactiva los rayos de ambas manos.</summary>
    private void SetHandRays(bool active)
    {
        if (leftHandRay != null) leftHandRay.SetActive(active);
        if (rightHandRay != null) rightHandRay.SetActive(active);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  PANTALLAS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Muestra la pantalla de inicio (menú principal).</summary>
    private void ShowStartScreen()
    {
        if (startScreen != null) startScreen.SetActive(true);
        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        if (nameEntryScreen != null) nameEntryScreen.SetActive(false);
        if (hudContainer != null) hudContainer.SetActive(false);

        // Activar rayos de manos (para interactuar con menús)
        SetHandRays(true);

        // Ocultar MenuBG y ScoreBG al inicio (StartBG se encarga de mostrarlos vía onClick)
        if (menuBG != null) menuBG.SetActive(false);
        if (scoreBG != null) scoreBG.SetActive(false);

        // Actualizar leaderboard text
        RefreshLeaderboardDisplay();

        // Limpiar las tarjetas del tendedero
        if (orderCards != null)
        {
            foreach (OrderCardUI card in orderCards)
            {
                if (card != null) card.ClearOrder();
            }
        }

        // Reproducir música del menú
        PlayMenuMusic();
    }

    /// <summary>Muestra el HUD durante la partida.</summary>
    private void ShowGameHUD()
    {
        if (startScreen    != null) startScreen.SetActive(false);
        if (gameOverScreen != null) gameOverScreen.SetActive(false);
        if (nameEntryScreen!= null) nameEntryScreen.SetActive(false);
        if (menuBG         != null) menuBG.SetActive(false);          // fix ítem 4: MenuBG no se desactivaba al reiniciar
        if (scoreBG        != null) scoreBG.SetActive(false);         // fix ítem 4: ScoreBG no se desactivaba al reiniciar
        if (hudContainer   != null) hudContainer.SetActive(true);

        // Desactivar rayos de manos (el jugador usa las manos para agarrar objetos)
        SetHandRays(false);

        _lastCountdownSecond = -1;
        _previousScore = 0;

        // Detener música de menú al iniciar juego
        StopMenuMusic();
    }

    /// <summary>
    /// Muestra la pantalla de game over con el score final.
    /// Teletransporta al jugador a la zona de Game Over.
    /// </summary>
    private void ShowGameOver(int finalScore)
    {
        // Activar rayos para interactuar con botones de Game Over
        SetHandRays(true);

        // Ocultar TODAS las pantallas antes de mostrar Game Over (fix ítem 3: menús superpuestos)
        if (startScreen    != null) startScreen.SetActive(false);
        if (nameEntryScreen!= null) nameEntryScreen.SetActive(false);
        if (hudContainer   != null) hudContainer.SetActive(false);
        if (menuBG         != null) menuBG.SetActive(false);          // fix ítem 3: menuBG quedaba visible
        if (scoreBG        != null) scoreBG.SetActive(false);         // fix ítem 3: scoreBG quedaba visible

        _pendingScore = finalScore;

        // Teletransportar al jugador a la zona de Game Over
        TeleportPlayer(teleportGameOverPoint);

        // Mostrar score
        if (finalScoreText != null)
            finalScoreText.text = $"${finalScore}";

        // Verificar si es top 10
        bool isTopTen = false;
        if (LeaderboardManager.Instance != null)
            isTopTen = LeaderboardManager.Instance.IsTopTen(finalScore);

        // Mostrar/ocultar elementos de top 10
        if (topTenMessage != null)
            topTenMessage.SetActive(isTopTen);
        if (saveScoreButton != null)
            saveScoreButton.gameObject.SetActive(isTopTen);

        // Activar pantalla de Game Over (ya está en su posición fija en la escena)
        if (gameOverScreen != null)
            gameOverScreen.SetActive(true);

        // Audio de game over
        if (gameOverSound != null && _audioSource != null)
            _audioSource.PlayOneShot(gameOverSound);
    }

    /// <summary>
    /// Muestra el name entry para ingresar 3 caracteres con flechas.
    /// El jugador ya está en la zona de Game Over (misma ubicación).
    /// Muestra los caracteres anteriores como default.
    /// </summary>
    private void ShowNameEntry()
    {
        if (gameOverScreen != null) gameOverScreen.SetActive(false);

        // Refrescar los caracteres (mantiene los anteriores)
        RefreshNameDisplay();

        // Activar pantalla de Name Entry (ya está en su posición fija en la escena)
        if (nameEntryScreen != null)
            nameEntryScreen.SetActive(true);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  LEADERBOARD DISPLAY
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Actualiza el texto del leaderboard en ScoreBG.</summary>
    private void RefreshLeaderboardDisplay()
    {
        if (leaderboardText == null) return;

        if (LeaderboardManager.Instance != null)
            leaderboardText.text = LeaderboardManager.Instance.GetFormattedLeaderboard();
        else
            leaderboardText.text = "No hay puntajes aún.\n¡Sé el primero!";
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  BOTONES — INICIO
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Callback del botón "Iniciar" en el Start Screen.</summary>
    private void OnStartButtonPressed()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[UIManager] No se encontró GameManager en la escena.");
            return;
        }

        // Usar el nombre del name entry (los caracteres guardados)
        string playerName = new string(_currentNameChars);

        // Detener la música de menú
        StopMenuMusic();

        // Teletransportar al jugador a la zona de juego
        TeleportPlayer(teleportStartPoint);

        ShowGameHUD();
        GameManager.Instance.StartGame(playerName);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  BOTONES — GAME OVER
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Callback del botón "Restart rápido" en Game Over.</summary>
    private void OnQuickRestartPressed()
    {
        // Detener la música de menú
        StopMenuMusic();

        // Limpiar objetos de la partida anterior
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGameplayObjects();
        }

        // Teletransportar al jugador a la zona de juego
        TeleportPlayer(teleportStartPoint);

        // Mostrar HUD
        ShowGameHUD();

        // Usar el nombre actual para volver a empezar
        string playerName = new string(_currentNameChars);
        
        // Iniciar el juego directamente
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame(playerName);
        }
    }

    /// <summary>Callback del botón "Guardar" en Game Over (solo si es top 10).</summary>
    private void OnSaveScorePressed()
    {
        ShowNameEntry();
    }

    /// <summary>Callback del botón "Menú" en Game Over.</summary>
    private void OnGameOverMenuPressed()
    {
        shouldStartGameOnLoad = false;
        shouldShowScoresOnLoad = false;

        // Detener la música de menú
        StopMenuMusic();

        // Recargar la escena actual para reiniciar todo
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  BOTONES — NAME ENTRY
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Callback del botón "OK" en Name Entry. Guarda el score y va a Scores.</summary>
    private void OnNameOkPressed()
    {
        string playerName = new string(_currentNameChars);

        // Guardar en el leaderboard
        if (LeaderboardManager.Instance != null)
            LeaderboardManager.Instance.AddScore(playerName, _pendingScore);

        shouldStartGameOnLoad = false;
        shouldShowScoresOnLoad = true;

        // Detener la música de menú
        StopMenuMusic();

        // Recargar la escena actual para reiniciar todo y mostrar scores
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>Callback del botón "Menú" en Name Entry. Descarta y va al menú.</summary>
    private void OnNameMenuPressed()
    {
        shouldStartGameOnLoad = false;
        shouldShowScoresOnLoad = false;

        // Detener la música de menú
        StopMenuMusic();

        // Recargar la escena actual para reiniciar todo
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  BOTONES — MENÚ / SCORES TOGGLE
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Muestra el leaderboard (ScoreBG) y oculta el menú (MenuBG).</summary>
    private void OnViewScoresPressed()
    {
        RefreshLeaderboardDisplay();
        if (menuBG != null) menuBG.SetActive(false);
        if (scoreBG != null) scoreBG.SetActive(true);
    }

    /// <summary>Vuelve al menú (MenuBG) y oculta el leaderboard (ScoreBG).</summary>
    private void OnBackToMenuPressed()
    {
        if (scoreBG != null) scoreBG.SetActive(false);
        if (menuBG != null) menuBG.SetActive(true);
    }

    /// <summary>Cierra la aplicación (o detiene el juego en el Editor).</summary>
    private void OnQuitButtonPressed()
    {
        Debug.Log("[UIManager] Saliendo del juego...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA — Para otros scripts
    // ═══════════════════════════════════════════════════════════════════════════

    private Coroutine _toastCoroutine;

    /// <summary>
    /// Muestra un mensaje temporal en la tarjeta general de puntuación en el mundo.
    /// </summary>
    public void ShowToast(string message, float duration = 3f, bool isLoss = true)
    {
        Debug.Log($"[UIManager] Toast: {message}");

        if (scoreFeedbackText != null)
        {
            scoreFeedbackText.color = isLoss ? lossColor : gainColor;

            if (_toastCoroutine != null)
                StopCoroutine(_toastCoroutine);

            _toastCoroutine = StartCoroutine(ShowToastCoroutine(message, duration));
        }
    }

    private System.Collections.IEnumerator ShowToastCoroutine(string message, float duration)
    {
        scoreFeedbackText.text = message;
        scoreFeedbackText.gameObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        scoreFeedbackText.text = "";
        _toastCoroutine = null;
    }

    /// <summary>
    /// Fuerza la actualización de los pedidos (útil cuando se llama manualmente).
    /// </summary>
    public void RefreshOrders()
    {
        if (GameManager.Instance != null)
            UpdateOrders(GameManager.Instance.ActiveOrders);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA — PREVENTIVE DEACTIVATION FOR WORLD STATE CHANGES
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Desactiva preventivamente las pantallas del menú antes de que se active el
    /// worldInactive, evitando glitches visuales o la ejecución de OnEnable en los menús.
    /// </summary>
    public void PrepareForGameOver()
    {
        if (startScreen    != null) startScreen.SetActive(false);
        if (menuBG         != null) menuBG.SetActive(false);
        if (scoreBG        != null) scoreBG.SetActive(false);
        if (nameEntryScreen!= null) nameEntryScreen.SetActive(false);
        if (hudContainer   != null) hudContainer.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  AUDIO DE MENÚ
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Reproduce la música del menú en bucle.</summary>
    private void PlayMenuMusic()
    {
        if (menuMusic != null && _audioSource != null)
        {
            // Si ya se está reproduciendo el mismo clip, no hacer nada
            if (_audioSource.clip == menuMusic && _audioSource.isPlaying)
                return;

            _audioSource.clip = menuMusic;
            _audioSource.loop = true;
            _audioSource.Play();
            Debug.Log("[UIManager] Música de menú iniciada.");
        }
    }

    /// <summary>Detiene la música del menú.</summary>
    private void StopMenuMusic()
    {
        if (_audioSource != null && _audioSource.clip == menuMusic)
        {
            _audioSource.Stop();
            _audioSource.clip = null;
            Debug.Log("[UIManager] Música de menú detenida.");
        }
    }
}
