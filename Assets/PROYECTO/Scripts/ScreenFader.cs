using System.Collections;
using UnityEngine;

/// <summary>
/// ScreenFader — Efecto de fundido a negro para transiciones en VR.
///
/// Configuración:
///   1. Crear un Canvas (World Space) como hijo de la cámara VR.
///   2. Agregar un Panel/Image negro que cubra toda la vista.
///   3. Agregar un CanvasGroup al Canvas o al Panel.
///   4. Asignar el CanvasGroup en el Inspector de este script.
///   5. El CanvasGroup debe empezar con alpha = 0 (transparente).
/// </summary>
public class ScreenFader : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  SINGLETON
    // ═══════════════════════════════════════════════════════════════════════════

    public static ScreenFader Instance { get; private set; }

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Referencia")]
    [Tooltip("CanvasGroup del panel negro de fade. Debe estar en un Canvas hijo de la cámara VR.")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Configuración")]
    [Tooltip("Duración del fade en segundos.")]
    [SerializeField] private float fadeDuration = 0.4f;

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

        // Asegurar que empiece transparente
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.interactable = false;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Fundido a negro (pantalla se oscurece).</summary>
    public IEnumerator FadeOut()
    {
        if (fadeCanvasGroup == null) yield break;
        fadeCanvasGroup.blocksRaycasts = true;
        yield return StartCoroutine(Fade(0f, 1f));
    }

    /// <summary>Fundido desde negro (pantalla se aclara).</summary>
    public IEnumerator FadeIn()
    {
        if (fadeCanvasGroup == null) yield break;
        yield return StartCoroutine(Fade(1f, 0f));
        fadeCanvasGroup.blocksRaycasts = false;
    }

    /// <summary>Fade out → espera → fade in. Útil para teletransportes.</summary>
    public IEnumerator FadeOutAndIn(float holdDuration = 0.1f)
    {
        yield return FadeOut();
        yield return new WaitForSeconds(holdDuration);
        yield return FadeIn();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  PRIVADO
    // ═══════════════════════════════════════════════════════════════════════════

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        fadeCanvasGroup.alpha = from;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // unscaled para que funcione en pausa
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = to;
    }
}
