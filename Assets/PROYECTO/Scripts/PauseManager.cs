using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PauseManager — Permite pausar o terminar la partida durante el juego.
///
/// Configuración:
///   1. Crear un Canvas (World Space) para el menú de pausa.
///   2. Agregar botones "Continuar" y "Terminar Partida".
///   3. Asignar las referencias en el Inspector.
///   4. Configurar el botón del control VR (ej. Botón de Menú/Start).
///
/// El canvas de pausa debe estar posicionado frente al jugador.
/// Se puede poner como hijo de la cámara VR para que siempre esté visible.
/// </summary>
public class PauseManager : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  SINGLETON
    // ═══════════════════════════════════════════════════════════════════════════

    public static PauseManager Instance { get; private set; }

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("UI de Pausa")]
    [Tooltip("Canvas/GameObject del menú de pausa (debe estar en World Space, idealmente hijo de la cámara).")]
    [SerializeField] private GameObject pauseCanvas;

    [Tooltip("Botón para continuar la partida.")]
    [SerializeField] private Button continueButton;

    [Tooltip("Botón para terminar la partida.")]
    [SerializeField] private Button endGameButton;

    [Tooltip("(Opcional) Texto que muestra el tiempo restante en el menú de pausa.")]
    [SerializeField] private TMP_Text pauseTimerText;

    [Header("Input")]
    [Tooltip("InputActionReference para el botón de menú/pausa del control VR.")]
    [SerializeField] private InputActionReference pauseAction;

    // ═══════════════════════════════════════════════════════════════════════════
    //  ESTADO
    // ═══════════════════════════════════════════════════════════════════════════

    private bool _isPaused = false;

    /// <summary>¿Está el juego pausado?</summary>
    public bool IsPaused => _isPaused;

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
    }

    void Start()
    {
        // Ocultar menú de pausa al inicio
        if (pauseCanvas != null) pauseCanvas.SetActive(false);

        // Configurar botones
        if (continueButton != null)
            continueButton.onClick.AddListener(ResumeGame);
        if (endGameButton != null)
            endGameButton.onClick.AddListener(EndGameFromPause);

        // Configurar input action
        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.Enable();
            pauseAction.action.performed += OnPausePressed;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (pauseAction != null && pauseAction.action != null)
            pauseAction.action.performed -= OnPausePressed;
    }

    void Update()
    {
        // Fallback: también escuchar la tecla Escape en PC
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  INPUT
    // ═══════════════════════════════════════════════════════════════════════════

    private void OnPausePressed(InputAction.CallbackContext ctx)
    {
        TogglePause();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Alterna entre pausar y reanudar.</summary>
    public void TogglePause()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning) return;

        if (_isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    /// <summary>Pausa el juego y muestra el menú de pausa.</summary>
    public void PauseGame()
    {
        if (!GameManager.Instance.IsGameRunning) return;

        _isPaused = true;
        Time.timeScale = 0f;

        // Actualizar timer en el menú de pausa
        if (pauseTimerText != null && GameManager.Instance != null)
        {
            float time = GameManager.Instance.TimeRemaining;
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            pauseTimerText.text = $"Tiempo restante: {minutes:00}:{seconds:00}";
        }

        if (pauseCanvas != null) pauseCanvas.SetActive(true);
        Debug.Log("[PauseManager] Juego pausado.");
    }

    /// <summary>Reanuda el juego y oculta el menú de pausa.</summary>
    public void ResumeGame()
    {
        _isPaused = false;
        Time.timeScale = 1f;

        if (pauseCanvas != null) pauseCanvas.SetActive(false);
        Debug.Log("[PauseManager] Juego reanudado.");
    }

    /// <summary>Termina la partida desde el menú de pausa.</summary>
    public void EndGameFromPause()
    {
        // Asegurar que el timeScale se restaure
        Time.timeScale = 1f;
        _isPaused = false;

        if (pauseCanvas != null) pauseCanvas.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.EndGame();

        Debug.Log("[PauseManager] Partida terminada desde pausa.");
    }
}
