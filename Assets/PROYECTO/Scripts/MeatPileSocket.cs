using UnityEngine;

/// <summary>
/// MeatPileSocket — Se coloca en cada montón de carne estática sobre la plancha
/// (Bistec, Suadero, Chorizo, etc.).
///
/// Expone TryServeMeat() que es llamado por TacoAssembler
/// cuando el jugador presiona el botón del control sobre el montón.
/// Al servir carne exitosamente, TacoAssembler convierte la tortilla en taco automáticamente.
///
/// Configuración:
///   1. Colocar en el GameObject del montón de carne.
///   2. Asignar el tipo de carne en el Inspector.
///   3. El montón debe tener un Collider marcado como Trigger.
///   4. La tortilla debe tener el tag "Tortilla" y el componente TacoAssembler.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MeatPileSocket : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Configuración de carne")]
    [Tooltip("Tipo de carne que representa este montón. Debe coincidir con GameManager.availableMeats.")]
    [SerializeField] private string meatType = "Bistec";

    [Header("Tags")]
    [Tooltip("Tag de la tortilla.")]
    [SerializeField] private string tortillaTag = "Tortilla";

    [Header("Audio (opcional)")]
    [Tooltip("Sonido al servir carne en la tortilla.")]
    [SerializeField] private AudioClip serveMeatSound;

    [Header("Visual (opcional)")]
    [Tooltip("Partículas al servir carne (ej. vapor, salpicón).")]
    [SerializeField] private ParticleSystem serveParticles;

    // ═══════════════════════════════════════════════════════════════════════════
    //  ESTADO PRIVADO
    // ═══════════════════════════════════════════════════════════════════════════

    private AudioSource _audioSource;

    // ═══════════════════════════════════════════════════════════════════════════
    //  UNITY
    // ═══════════════════════════════════════════════════════════════════════════

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.spatialBlend = 1f;
        _audioSource.playOnAwake = false;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Llamado por TacoAssembler cuando el jugador presiona el botón del control
    /// mientras la tortilla está sobre este montón.
    ///
    /// Requisitos para que tenga éxito:
    ///   - La tortilla aún no tiene carne.
    ///   - La tortilla está cocida (Cooked).
    /// </summary>
    /// <param name="assembler">TacoAssembler de la tortilla que intenta recibir carne.</param>
    /// <returns>true si la carne fue asignada, false en caso contrario.</returns>
    public bool TryServeMeat(TacoAssembler assembler)
    {
        if (assembler == null)
            return false;

        if (assembler.HasMeat)
        {
            Debug.Log($"[MeatPileSocket] '{assembler.name}' ya tiene carne '{assembler.MeatType}'.");
            return false;
        }

        // Verificar que la tortilla esté cocida
        TortillaManager tm = assembler.GetComponent<TortillaManager>();
        if (tm != null && !tm.IsCooked)
        {
            Debug.Log($"[MeatPileSocket] Tortilla no está cocida (Estado: {tm.CurrentState}). Cocínala primero.");
            return false;
        }

        // ¡Servir carne! (SetMeatType convierte automáticamente en taco)
        assembler.SetMeatType(meatType);

        if (serveMeatSound != null && _audioSource != null)
            _audioSource.PlayOneShot(serveMeatSound);

        if (serveParticles != null)
            serveParticles.Play();

        Debug.Log($"[MeatPileSocket] Carne '{meatType}' servida a '{assembler.name}' (botón presionado). ✓");
        return true;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  VALIDACIÓN
    // ═══════════════════════════════════════════════════════════════════════════

    void OnValidate()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }
}
