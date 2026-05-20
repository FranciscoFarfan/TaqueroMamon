using System;
using UnityEngine;

/// <summary>
/// TortillaManager — Controla el ciclo de vida de una tortilla:
///   Raw → Cooking → Cooked → Burnt
///
/// Se coloca en el prefab de Tortilla.
/// El ComalSocket llama StartCooking()/StopCooking() cuando entra/sale del comal.
/// </summary>
public class TortillaManager : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  ENUM
    // ═══════════════════════════════════════════════════════════════════════════

    public enum TortillaState { Raw, Cooking, Cooked, Burnt }

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Tiempos de cocción")]
    [Tooltip("Segundos que tarda en calentarse (pasar de Raw a Cooked).")]
    [SerializeField] private float cookTime = 5f;

    [Tooltip("Segundos totales en el comal para que se queme (desde que empezó a calentar).")]
    [SerializeField] private float burnTime = 45f;

    [Header("Materiales visuales")]
    [Tooltip("Material de tortilla cruda.")]
    [SerializeField] private Material rawMaterial;

    [Tooltip("Material de tortilla caliente / lista.")]
    [SerializeField] private Material cookedMaterial;

    [Tooltip("Material de tortilla quemada.")]
    [SerializeField] private Material burntMaterial;

    [Header("Penalización")]
    [Tooltip("Puntos que se restan si la tortilla se quema.")]
    [SerializeField] private int burnPenalty = 1;

    [Header("Audio (opcional)")]
    [Tooltip("Sonido de cocción (loop).")]
    [SerializeField] private AudioClip cookingSound;

    [Tooltip("Sonido al quemarse.")]
    [SerializeField] private AudioClip burntSound;

    // ═══════════════════════════════════════════════════════════════════════════
    //  ESTADO PRIVADO
    // ═══════════════════════════════════════════════════════════════════════════

    private TortillaState _state = TortillaState.Raw;
    private float _cookTimer = 0f;
    private bool _isOnComal = false;
    private Renderer _renderer;
    private AudioSource _audioSource;

    // ═══════════════════════════════════════════════════════════════════════════
    //  PROPIEDADES PÚBLICAS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Estado actual de la tortilla.</summary>
    public TortillaState CurrentState => _state;

    /// <summary>¿Está sobre el comal?</summary>
    public bool IsOnComal => _isOnComal;

    /// <summary>¿Está lista para usarse (cooked)?</summary>
    public bool IsCooked => _state == TortillaState.Cooked;

    /// <summary>Progreso de cocción normalizado 0–1 (solo durante Cooking).</summary>
    public float CookProgress => (_state == TortillaState.Cooking)
        ? Mathf.Clamp01(_cookTimer / cookTime)
        : (_state == TortillaState.Cooked || _state == TortillaState.Burnt) ? 1f : 0f;

    // ═══════════════════════════════════════════════════════════════════════════
    //  EVENTOS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Se dispara cuando cambia el estado de la tortilla.</summary>
    public event Action<TortillaState> OnStateChanged;

    // ═══════════════════════════════════════════════════════════════════════════
    //  UNITY LOOP
    // ═══════════════════════════════════════════════════════════════════════════

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.spatialBlend = 1f;
        _audioSource.loop = true;
        _audioSource.playOnAwake = false;

        ApplyMaterial(rawMaterial);
    }

    void Update()
    {
        if (!_isOnComal) return;
        if (_state == TortillaState.Burnt || _state == TortillaState.Cooked && _cookTimer >= burnTime) return;

        // Solo avanzar el timer si está en el comal y no quemada
        if (_state == TortillaState.Cooking || _state == TortillaState.Cooked)
        {
            _cookTimer += Time.deltaTime;

            // Transición Cooking → Cooked
            if (_state == TortillaState.Cooking && _cookTimer >= cookTime)
            {
                SetState(TortillaState.Cooked);
                ApplyMaterial(cookedMaterial);
                StopCookingAudio();
                Debug.Log($"[TortillaManager] '{gameObject.name}' está lista (Cooked).");
            }

            // Transición Cooked → Burnt
            if (_state == TortillaState.Cooked && _cookTimer >= burnTime)
            {
                SetState(TortillaState.Burnt);
                ApplyMaterial(burntMaterial);
                PlayBurntAudio();

                if (GameManager.Instance != null && GameManager.Instance.IsGameRunning)
                    GameManager.Instance.ReportTortillaLost(burnPenalty, "Tortilla quemada");

                Debug.Log($"[TortillaManager] '{gameObject.name}' se quemó!");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Llamado por ComalSocket cuando la tortilla entra al comal.
    /// Inicia la cocción si la tortilla está cruda.
    /// </summary>
    public void StartCooking()
    {
        _isOnComal = true;

        if (_state == TortillaState.Raw)
        {
            SetState(TortillaState.Cooking);
            PlayCookingAudio();
            Debug.Log($"[TortillaManager] '{gameObject.name}' empezó a calentarse.");
        }
        // Si ya estaba Cooked, sigue contando hacia Burnt
    }

    /// <summary>
    /// Llamado por ComalSocket cuando la tortilla sale del comal.
    /// Pausa la cocción (el timer se mantiene).
    /// </summary>
    public void StopCooking()
    {
        _isOnComal = false;
        StopCookingAudio();
        Debug.Log($"[TortillaManager] '{gameObject.name}' retirada del comal. Estado: {_state}");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  PRIVADO
    // ═══════════════════════════════════════════════════════════════════════════

    private void SetState(TortillaState newState)
    {
        _state = newState;
        OnStateChanged?.Invoke(_state);
    }

    private void ApplyMaterial(Material mat)
    {
        if (_renderer != null && mat != null)
            _renderer.material = mat;
    }

    private void PlayCookingAudio()
    {
        if (_audioSource != null && cookingSound != null)
        {
            _audioSource.clip = cookingSound;
            _audioSource.Play();
        }
    }

    private void StopCookingAudio()
    {
        if (_audioSource != null && _audioSource.isPlaying)
            _audioSource.Stop();
    }

    private void PlayBurntAudio()
    {
        if (_audioSource != null && burntSound != null)
        {
            _audioSource.PlayOneShot(burntSound);
        }
    }
}
