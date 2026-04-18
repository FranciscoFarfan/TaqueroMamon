using UnityEngine;

/// <summary>
/// MeatPileSocket — Se coloca en cada montón de carne estática sobre la plancha
/// (Bistec, Queso, Chorizo, etc.).
///
/// Cuando la tortilla (en la mano del jugador, orientada hacia abajo) toca el montón,
/// marca el tipo de carne en el TacoAssembler de la tortilla.
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

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(tortillaTag)) return;

        // Obtener el TacoAssembler de la tortilla
        TacoAssembler assembler = other.GetComponentInParent<TacoAssembler>();
        if (assembler == null)
        {
            Debug.LogWarning($"[MeatPileSocket] Tortilla '{other.name}' no tiene TacoAssembler.");
            return;
        }

        // Solo aceptar tortillas cocidas que no tengan carne aún
        if (assembler.HasMeat)
        {
            Debug.Log($"[MeatPileSocket] Tortilla '{other.name}' ya tiene carne.");
            return;
        }

        // Verificar que la tortilla está cocida
        TortillaManager tortillaManager = other.GetComponentInParent<TortillaManager>();
        if (tortillaManager != null && !tortillaManager.IsCooked)
        {
            Debug.Log($"[MeatPileSocket] Tortilla '{other.name}' no está cocida aún. Estado: {tortillaManager.CurrentState}");
            return;
        }

        // Asignar la carne
        assembler.SetMeatType(meatType);

        // Feedback visual y de audio
        if (serveMeatSound != null && _audioSource != null)
            _audioSource.PlayOneShot(serveMeatSound);

        if (serveParticles != null)
            serveParticles.Play();

        Debug.Log($"[MeatPileSocket] Carne '{meatType}' asignada a tortilla '{other.name}'.");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  VALIDACIÓN
    // ═══════════════════════════════════════════════════════════════════════════

    void OnValidate()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }
}
