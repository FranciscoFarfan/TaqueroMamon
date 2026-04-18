using UnityEngine;

/// <summary>
/// ComalSocket — Se coloca en cada slot del comal (6 en total).
/// Detecta cuando una tortilla entra o sale del trigger y le avisa a TortillaManager.
///
/// Configuración:
///   1. Crear un GameObject vacío por cada slot del comal.
///   2. Agregar un Collider (Box o Sphere) marcado como Trigger.
///   3. Agregar este script.
///   4. Las tortillas deben tener Rigidbody + Collider + tag "Tortilla".
/// </summary>
[RequireComponent(typeof(Collider))]
public class ComalSocket : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Configuración")]
    [Tooltip("Tag que deben tener las tortillas.")]
    [SerializeField] private string tortillaTag = "Tortilla";

    [Header("Visual (Opcional)")]
    [Tooltip("Indicador visual de que el slot está ocupado (ej. un sprite de círculo).")]
    [SerializeField] private GameObject occupiedIndicator;

    // ═══════════════════════════════════════════════════════════════════════════
    //  ESTADO PRIVADO
    // ═══════════════════════════════════════════════════════════════════════════

    private TortillaManager _currentTortilla = null;

    // ═══════════════════════════════════════════════════════════════════════════
    //  PROPIEDADES PÚBLICAS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>¿Hay una tortilla en este slot?</summary>
    public bool IsOccupied => _currentTortilla != null;

    /// <summary>Referencia a la tortilla actual (null si vacío).</summary>
    public TortillaManager CurrentTortilla => _currentTortilla;

    // ═══════════════════════════════════════════════════════════════════════════
    //  TRIGGERS
    // ═══════════════════════════════════════════════════════════════════════════

    void OnTriggerEnter(Collider other)
    {
        // Ignorar si ya hay una tortilla en este slot
        if (_currentTortilla != null) return;

        // Solo aceptar objetos con tag de tortilla
        if (!other.CompareTag(tortillaTag)) return;

        TortillaManager tortilla = other.GetComponentInParent<TortillaManager>();
        if (tortilla == null) return;

        // Si la tortilla ya está quemada, no aceptarla
        if (tortilla.CurrentState == TortillaManager.TortillaState.Burnt) return;

        _currentTortilla = tortilla;
        _currentTortilla.StartCooking();

        if (occupiedIndicator != null)
            occupiedIndicator.SetActive(true);

        Debug.Log($"[ComalSocket] Tortilla '{other.gameObject.name}' entró al slot '{gameObject.name}'.");
    }

    void OnTriggerExit(Collider other)
    {
        if (_currentTortilla == null) return;

        // Verificar que el objeto que sale es la tortilla que tenemos registrada
        TortillaManager tortilla = other.GetComponentInParent<TortillaManager>();
        if (tortilla == null || tortilla != _currentTortilla) return;

        _currentTortilla.StopCooking();
        _currentTortilla = null;

        if (occupiedIndicator != null)
            occupiedIndicator.SetActive(false);

        Debug.Log($"[ComalSocket] Tortilla '{other.gameObject.name}' salió del slot '{gameObject.name}'.");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  VALIDACIÓN
    // ═══════════════════════════════════════════════════════════════════════════

    void OnValidate()
    {
        // Asegurar que el collider sea trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"[ComalSocket] Collider en '{gameObject.name}' fue marcado como Trigger automáticamente.");
        }
    }
}
