using System.Collections;
using UnityEngine;

/// <summary>
/// AmbientSoundManager — Sistema de audio ambiental dinámico por fase del juego.
///
/// Maneja 3 fases: Mañana (menú), Tarde (juego), Noche (fin).
/// Cada fase tiene:
///   - Un AudioClip de ambiente largo (loop continuo).
///   - Un arreglo de AudioClips cortos/aleatorios (perros, autos, gente, grillos, etc.)
///     que suenan cada 30–50 segundos (aleatorio).
///
/// Configuración:
///   1. Colocar este script en un GameObject de la escena (ej. "AmbientSoundManager").
///   2. Asignar los AudioClips en el Inspector.
///   3. El script se conecta automáticamente al GameManager.
/// </summary>
public class AmbientSoundManager : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  SINGLETON
    // ═══════════════════════════════════════════════════════════════════════════

    public static AmbientSoundManager Instance { get; private set; }

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR — Audio por fase
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("─── Mañana (Menú) ───")]
    [Tooltip("Audio ambiental de fondo para la mañana (loop continuo).")]
    [SerializeField] private AudioClip morningAmbient;

    [Tooltip("Sonidos aleatorios de mañana (pájaros, vendedores, etc.).")]
    [SerializeField] private AudioClip[] morningSFX;

    [Header("─── Tarde (Juego) ───")]
    [Tooltip("Audio ambiental de fondo para la tarde (loop continuo).")]
    [SerializeField] private AudioClip afternoonAmbient;

    [Tooltip("Sonidos aleatorios de tarde (autos, gente, música lejana, etc.).")]
    [SerializeField] private AudioClip[] afternoonSFX;

    [Header("─── Noche (Fin del juego) ───")]
    [Tooltip("Audio ambiental de fondo para la noche (loop continuo).")]
    [SerializeField] private AudioClip nightAmbient;

    [Tooltip("Sonidos aleatorios de noche (grillos, perros, sirenas lejanas, etc.).")]
    [SerializeField] private AudioClip[] nightSFX;

    [Header("Configuración")]
    [Tooltip("Tiempo mínimo entre sonidos aleatorios (segundos).")]
    [SerializeField] private float sfxIntervalMin = 30f;

    [Tooltip("Tiempo máximo entre sonidos aleatorios (segundos).")]
    [SerializeField] private float sfxIntervalMax = 50f;

    [Tooltip("Volumen del audio ambiental de fondo (0–1).")]
    [SerializeField] [Range(0f, 1f)] private float ambientVolume = 0.5f;

    [Tooltip("Volumen de los efectos aleatorios (0–1).")]
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.7f;

    [Tooltip("Duración del crossfade entre ambientes (segundos).")]
    [SerializeField] private float crossfadeDuration = 2f;

    // ═══════════════════════════════════════════════════════════════════════════
    //  ESTADO PRIVADO
    // ═══════════════════════════════════════════════════════════════════════════

    private AudioSource _ambientSource;
    private AudioSource _sfxSource;
    private Coroutine _sfxCoroutine;
    private Coroutine _crossfadeCoroutine;
    private AudioClip[] _currentSFXArray;

    private enum Phase { Morning, Afternoon, Night }
    private Phase _currentPhase = Phase.Morning;

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

        // Crear AudioSources
        _ambientSource = gameObject.AddComponent<AudioSource>();
        _ambientSource.loop = true;
        _ambientSource.playOnAwake = false;
        _ambientSource.spatialBlend = 0f; // 2D (no espacial)
        _ambientSource.volume = ambientVolume;

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.loop = false;
        _sfxSource.playOnAwake = false;
        _sfxSource.spatialBlend = 0f;
        _sfxSource.volume = sfxVolume;
    }

    void Start()
    {
        // Iniciar con fase de mañana
        SetPhase(Phase.Morning);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA — Llamadas desde GameManager
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Cambia al ambiente de mañana (menú principal).</summary>
    public void SetMorningPhase()
    {
        SetPhase(Phase.Morning);
        Debug.Log("[AmbientSoundManager] Fase: Mañana (Menú)");
    }

    /// <summary>Cambia al ambiente de tarde (durante el juego).</summary>
    public void SetAfternoonPhase()
    {
        SetPhase(Phase.Afternoon);
        Debug.Log("[AmbientSoundManager] Fase: Tarde (Juego)");
    }

    /// <summary>Cambia al ambiente de noche (fin del juego).</summary>
    public void SetNightPhase()
    {
        SetPhase(Phase.Night);
        Debug.Log("[AmbientSoundManager] Fase: Noche (Game Over)");
    }

    /// <summary>Detiene todo el audio ambiental.</summary>
    public void StopAll()
    {
        if (_sfxCoroutine != null) StopCoroutine(_sfxCoroutine);
        if (_crossfadeCoroutine != null) StopCoroutine(_crossfadeCoroutine);
        _ambientSource.Stop();
        _sfxSource.Stop();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  PRIVADO
    // ═══════════════════════════════════════════════════════════════════════════

    private void SetPhase(Phase phase)
    {
        _currentPhase = phase;

        AudioClip newAmbient = null;
        AudioClip[] newSFX = null;

        switch (phase)
        {
            case Phase.Morning:
                newAmbient = morningAmbient;
                newSFX = morningSFX;
                break;
            case Phase.Afternoon:
                newAmbient = afternoonAmbient;
                newSFX = afternoonSFX;
                break;
            case Phase.Night:
                newAmbient = nightAmbient;
                newSFX = nightSFX;
                break;
        }

        _currentSFXArray = newSFX;

        // Crossfade al nuevo ambiente
        if (newAmbient != null)
        {
            if (_crossfadeCoroutine != null) StopCoroutine(_crossfadeCoroutine);
            _crossfadeCoroutine = StartCoroutine(CrossfadeAmbient(newAmbient));
        }
        else
        {
            _ambientSource.Stop();
        }

        // Reiniciar la corrutina de SFX aleatorios
        if (_sfxCoroutine != null) StopCoroutine(_sfxCoroutine);
        if (_currentSFXArray != null && _currentSFXArray.Length > 0)
            _sfxCoroutine = StartCoroutine(RandomSFXLoop());
    }

    private IEnumerator CrossfadeAmbient(AudioClip newClip)
    {
        // Si ya está reproduciendo el mismo clip, no hacer nada
        if (_ambientSource.clip == newClip && _ambientSource.isPlaying)
            yield break;

        // Fade out del clip actual
        float startVol = _ambientSource.volume;
        if (_ambientSource.isPlaying)
        {
            float elapsed = 0f;
            while (elapsed < crossfadeDuration / 2f)
            {
                elapsed += Time.deltaTime;
                _ambientSource.volume = Mathf.Lerp(startVol, 0f, elapsed / (crossfadeDuration / 2f));
                yield return null;
            }
        }

        // Cambiar clip
        _ambientSource.clip = newClip;
        _ambientSource.volume = 0f;
        _ambientSource.Play();

        // Fade in del nuevo clip
        float elapsed2 = 0f;
        while (elapsed2 < crossfadeDuration / 2f)
        {
            elapsed2 += Time.deltaTime;
            _ambientSource.volume = Mathf.Lerp(0f, ambientVolume, elapsed2 / (crossfadeDuration / 2f));
            yield return null;
        }

        _ambientSource.volume = ambientVolume;
        _crossfadeCoroutine = null;
    }

    private IEnumerator RandomSFXLoop()
    {
        while (true)
        {
            float waitTime = Random.Range(sfxIntervalMin, sfxIntervalMax);
            yield return new WaitForSeconds(waitTime);

            if (_currentSFXArray != null && _currentSFXArray.Length > 0)
            {
                AudioClip clip = _currentSFXArray[Random.Range(0, _currentSFXArray.Length)];
                if (clip != null)
                {
                    _sfxSource.PlayOneShot(clip, sfxVolume);
                }
            }
        }
    }
}
