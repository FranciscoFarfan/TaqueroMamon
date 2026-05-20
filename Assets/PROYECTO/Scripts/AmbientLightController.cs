using System.Collections;
using UnityEngine;

/// <summary>
/// AmbientLightController — Controla la iluminación ambiental de la escena
/// según el estado del juego.
///
/// • Menú / antes de iniciar  →  Ambiente de MAÑANA  (luz cálida, sol bajo, cielo rosado)
/// • Juego en curso           →  Ambiente de DÍA     (luz blanca brillante, sol alto, cielo azul)
///
/// Cómo configurar en el Inspector:
///   1. Asignar la Directional Light principal de la escena en "Sun Light".
///   2. Ajustar los colores y valores de las dos configuraciones.
///   3. (Opcional) Asignar los Skybox Materials de mañana y día.
///   4. Ajustar "Transition Duration" para controlar la velocidad del fundido.
///
/// El script se suscribe a los eventos del GameManager automáticamente.
/// </summary>
public class AmbientLightController : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR ─ Sol / Directional Light
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Referencia al Sol")]
    [Tooltip("La Directional Light principal de la escena (el 'sol').")]
    [SerializeField] private Light sunLight;

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR ─ Configuración de MAÑANA (menú)
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("─── Ambiente de MAÑANA (menú) ───")]
    [Tooltip("Color de la luz solar en la mañana (naranja/dorado cálido).")]
    [SerializeField] private Color morningSunColor = new Color(1f, 0.75f, 0.45f); // naranja cálido

    [Tooltip("Intensidad de la luz solar en la mañana.")]
    [SerializeField] [Range(0f, 8f)] private float morningSunIntensity = 0.8f;

    [Tooltip("Rotación del sol en la mañana (X = ángulo de elevación).")]
    [SerializeField] private Vector3 morningSunRotation = new Vector3(20f, -30f, 0f); // sol bajo al este

    [Tooltip("Color de la luz ambiental en la mañana.")]
    [SerializeField] private Color morningAmbientColor = new Color(0.85f, 0.65f, 0.55f); // rosado suave

    [Tooltip("Intensidad del skybox/ambiente en la mañana.")]
    [SerializeField] [Range(0f, 8f)] private float morningAmbientIntensity = 0.6f;

    [Tooltip("(Opcional) Material del Skybox para la mañana. Si es null, solo se cambia el color ambiental.")]
    [SerializeField] private Material morningSkybox;

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR ─ Configuración de DÍA (juego activo)
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("─── Ambiente de DÍA (juego) ───")]
    [Tooltip("Color de la luz solar de día (blanco-amarillo brillante).")]
    [SerializeField] private Color daySunColor = new Color(1f, 0.97f, 0.88f); // blanco cálido

    [Tooltip("Intensidad de la luz solar de día.")]
    [SerializeField] [Range(0f, 8f)] private float daySunIntensity = 1.5f;

    [Tooltip("Rotación del sol de día (X = ángulo de elevación, sol más alto).")]
    [SerializeField] private Vector3 daySunRotation = new Vector3(65f, 10f, 0f); // sol alto al mediodía

    [Tooltip("Color de la luz ambiental de día.")]
    [SerializeField] private Color dayAmbientColor = new Color(0.7f, 0.8f, 1f); // azul cielo suave

    [Tooltip("Intensidad del skybox/ambiente de día.")]
    [SerializeField] [Range(0f, 8f)] private float dayAmbientIntensity = 1.0f;

    [Tooltip("(Opcional) Material del Skybox para el día. Si es null, solo se cambia el color ambiental.")]
    [SerializeField] private Material daySkybox;

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR ─ Configuración de NOCHE (fin del juego)
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("─── Ambiente de NOCHE (fin del juego) ───")]
    [Tooltip("Color de la luz solar/lunar de noche (azul oscuro frío).")]
    [SerializeField] private Color nightSunColor = new Color(0.2f, 0.25f, 0.45f); // azul oscuro

    [Tooltip("Intensidad de la luz de noche.")]
    [SerializeField] [Range(0f, 8f)] private float nightSunIntensity = 0.3f;

    [Tooltip("Rotación del sol de noche (X = ángulo bajo, atardecer/noche).")]
    [SerializeField] private Vector3 nightSunRotation = new Vector3(5f, 170f, 0f); // sol muy bajo, casi oculto

    [Tooltip("Color de la luz ambiental de noche.")]
    [SerializeField] private Color nightAmbientColor = new Color(0.1f, 0.1f, 0.25f); // azul muy oscuro

    [Tooltip("Intensidad del skybox/ambiente de noche.")]
    [SerializeField] [Range(0f, 8f)] private float nightAmbientIntensity = 0.2f;

    [Tooltip("(Opcional) Material del Skybox para la noche. Si es null, solo se cambia el color ambiental.")]
    [SerializeField] private Material nightSkybox;

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR ─ Transición
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Transición")]
    [Tooltip("Duración en segundos del fundido entre las dos configuraciones de luz.")]
    [SerializeField] [Range(0.5f, 10f)] private float transitionDuration = 3f;

    // ═══════════════════════════════════════════════════════════════════════════
    //  ESTADO PRIVADO
    // ═══════════════════════════════════════════════════════════════════════════

    private Coroutine _transitionCoroutine;
    private bool      _subscribed = false;

    // ═══════════════════════════════════════════════════════════════════════════
    //  UNITY LOOP
    // ═══════════════════════════════════════════════════════════════════════════

    void Start()
    {
        // Aplicar la ambientación de MAÑANA inmediatamente (sin transición) al arrancar
        ApplyLightSettings(
            morningSunColor, morningSunIntensity, morningSunRotation,
            morningAmbientColor, morningAmbientIntensity, morningSkybox
        );

        SubscribeToEvents();
    }

    void Update()
    {
        // Reintentar suscripción si el GameManager aún no estaba listo
        if (!_subscribed)
            SubscribeToEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  SUSCRIPCIONES AL GAMEMANAGER
    // ═══════════════════════════════════════════════════════════════════════════

    private void SubscribeToEvents()
    {
        if (_subscribed || GameManager.Instance == null) return;

        GameManager.Instance.OnGameOver += OnGameOverHandler;
        _subscribed = true;

        Debug.Log("[AmbientLightController] Suscrito a eventos del GameManager.");
    }

    private void UnsubscribeFromEvents()
    {
        if (!_subscribed || GameManager.Instance == null) return;

        GameManager.Instance.OnGameOver -= OnGameOverHandler;
        _subscribed = false;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA — Llamada desde GameManager (o botones de UI)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Llama a este método cuando el juego INICIA para transicionar a la luz de DÍA.
    /// Conéctalo desde UIManager.OnStartButtonPressed() o desde GameManager.StartGame().
    /// </summary>
    public void TransitionToDay()
    {
        StartTransition(
            daySunColor, daySunIntensity, daySunRotation,
            dayAmbientColor, dayAmbientIntensity, daySkybox
        );
        Debug.Log("[AmbientLightController] Transicionando a ambiente de DÍA.");
    }

    /// <summary>
    /// Llama a este método cuando el juego TERMINA o se regresa al menú,
    /// para transicionar de vuelta a la luz de MAÑANA.
    /// </summary>
    public void TransitionToMorning()
    {
        StartTransition(
            morningSunColor, morningSunIntensity, morningSunRotation,
            morningAmbientColor, morningAmbientIntensity, morningSkybox
        );
        Debug.Log("[AmbientLightController] Transicionando a ambiente de MAÑANA.");
    }

    /// <summary>
    /// Llama a este método cuando el juego TERMINA para transicionar a ambiente NOCTURNO.
    /// </summary>
    public void TransitionToNight()
    {
        StartTransition(
            nightSunColor, nightSunIntensity, nightSunRotation,
            nightAmbientColor, nightAmbientIntensity, nightSkybox
        );
        Debug.Log("[AmbientLightController] Transicionando a ambiente de NOCHE.");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  MANEJADORES DE EVENTOS
    // ═══════════════════════════════════════════════════════════════════════════

    private void OnGameOverHandler(int finalScore)
    {
        // Cuando el juego termina, la transición a noche la maneja GameManager.EndGameSequence()
        // ya no se hace aquí para evitar duplicados
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  LÓGICA INTERNA
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Inicia o reinicia la coroutine de transición.</summary>
    private void StartTransition(
        Color targetSunColor, float targetSunIntensity, Vector3 targetSunRotation,
        Color targetAmbientColor, float targetAmbientIntensity, Material targetSkybox)
    {
        if (_transitionCoroutine != null)
            StopCoroutine(_transitionCoroutine);

        _transitionCoroutine = StartCoroutine(TransitionCoroutine(
            targetSunColor, targetSunIntensity, targetSunRotation,
            targetAmbientColor, targetAmbientIntensity, targetSkybox
        ));
    }

    /// <summary>
    /// Coroutine que interpola suavemente entre la configuración actual
    /// y la configuración objetivo durante <see cref="transitionDuration"/> segundos.
    /// </summary>
    private IEnumerator TransitionCoroutine(
        Color targetSunColor, float targetSunIntensity, Vector3 targetSunRotation,
        Color targetAmbientColor, float targetAmbientIntensity, Material targetSkybox)
    {
        // Capturar valores iniciales
        Color   startSunColor       = sunLight != null ? sunLight.color          : Color.white;
        float   startSunIntensity   = sunLight != null ? sunLight.intensity       : 1f;
        Vector3 startSunRotation    = sunLight != null ? sunLight.transform.eulerAngles : Vector3.zero;
        Color   startAmbientColor   = RenderSettings.ambientLight;
        float   startAmbientIntensity = RenderSettings.ambientIntensity;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration); // suavizado cúbico

            // Interpolar luz solar
            if (sunLight != null)
            {
                sunLight.color     = Color.Lerp(startSunColor, targetSunColor, t);
                sunLight.intensity = Mathf.Lerp(startSunIntensity, targetSunIntensity, t);
                sunLight.transform.eulerAngles = Vector3.Lerp(startSunRotation, targetSunRotation, t);
            }

            // Interpolar luz ambiental
            RenderSettings.ambientLight     = Color.Lerp(startAmbientColor, targetAmbientColor, t);
            RenderSettings.ambientIntensity = Mathf.Lerp(startAmbientIntensity, targetAmbientIntensity, t);

            yield return null;
        }

        // Asegurarse de que queden exactamente en el valor final
        ApplyLightSettings(
            targetSunColor, targetSunIntensity, targetSunRotation,
            targetAmbientColor, targetAmbientIntensity, targetSkybox
        );

        _transitionCoroutine = null;
    }

    /// <summary>Aplica los valores de iluminación instantáneamente (sin transición).</summary>
    private void ApplyLightSettings(
        Color sunColor, float sunIntensity, Vector3 sunRotation,
        Color ambientColor, float ambientIntensity, Material skybox)
    {
        if (sunLight != null)
        {
            sunLight.color                 = sunColor;
            sunLight.intensity             = sunIntensity;
            sunLight.transform.eulerAngles = sunRotation;
        }

        RenderSettings.ambientLight     = ambientColor;
        RenderSettings.ambientIntensity = ambientIntensity;

        if (skybox != null)
            RenderSettings.skybox = skybox;

        // Actualizar la iluminación de los probes de GI en tiempo real
        DynamicGI.UpdateEnvironment();
    }
}
