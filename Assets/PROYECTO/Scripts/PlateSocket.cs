using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// PlateSocket — Se coloca en el prefab "Plato".
/// Permite colocar tacos sobre el plato. Lleva la cuenta de los tacos
/// y permite que PersonInteraction evalúe el contenido del plato.
///
/// Configuración:
///   1. Agregar al prefab Plato.
///   2. El plato debe tener un Collider adicional marcado como Trigger
///      (más grande que el collider sólido, para detectar tacos que se acercan).
///   3. Opcionalmente, crear GameObjects vacíos como hijos del plato para
///      las posiciones donde se colocan los tacos (tacoSnapPoints).
///   4. Los tacos deben tener tag "taco" y componente TacoData.
/// </summary>
public class PlateSocket : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Configuración")]
    [Tooltip("Máximo de tacos que caben en un plato.")]
    [SerializeField] private int maxTacos = 5;

    [Tooltip("Tag de los tacos.")]
    [SerializeField] private string tacoTag = "taco";

    [Header("Posiciones de snap")]
    [Tooltip("Puntos donde los tacos se colocan visualmente sobre el plato. Si está vacío, se apilan.")]
    [SerializeField] private Transform[] tacoSnapPoints;

    [Tooltip("Offset de rotación a aplicar a los tacos para corregir su orientación.")]
    [SerializeField] private Vector3 tacoRotationOffset = Vector3.zero;

    [Tooltip("Offset de posición a aplicar a los tacos para corregir su ubicación.")]
    [SerializeField] private Vector3 tacoPositionOffset = Vector3.zero;

    [Header("Audio (opcional)")]
    [Tooltip("Sonido al colocar un taco en el plato.")]
    [SerializeField] private AudioClip placeTacoSound;

    // ═══════════════════════════════════════════════════════════════════════════
    //  ESTADO PRIVADO
    // ═══════════════════════════════════════════════════════════════════════════

    private readonly List<TacoData> _tacosOnPlate = new List<TacoData>();
    private AudioSource _audioSource;

    // ═══════════════════════════════════════════════════════════════════════════
    //  PROPIEDADES PÚBLICAS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Cuántos tacos hay en el plato.</summary>
    public int TacoCount => _tacosOnPlate.Count;

    /// <summary>¿Está lleno el plato?</summary>
    public bool IsFull => _tacosOnPlate.Count >= maxTacos;

    /// <summary>Indica si el plato ya fue entregado a un cliente para evitar entregas duplicadas.</summary>
    public bool IsDelivered { get; set; } = false;

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
        if (!other.CompareTag(tacoTag)) return;
        if (_tacosOnPlate.Count >= maxTacos) return;

        TacoData tacoData = other.GetComponentInParent<TacoData>();
        if (tacoData == null)
        {
            Debug.LogWarning($"[PlateSocket] Objeto con tag 'taco' sin TacoData: {other.name}");
            return;
        }

        // Verificar que no sea un taco que ya está en el plato
        if (_tacosOnPlate.Contains(tacoData)) return;

        // Desactivar el grab del taco (ya no se puede agarrar individualmente)
        XRGrabInteractable tacoGrab = other.GetComponentInParent<XRGrabInteractable>();
        if (tacoGrab != null)
        {
            // Si alguien lo tiene agarrado, forzar soltar
            if (tacoGrab.isSelected)
            {
                XRInteractionManager manager = tacoGrab.interactionManager;
                IXRSelectInteractor interactor = tacoGrab.firstInteractorSelecting;
                if (manager != null && interactor != null)
                {
                    manager.SelectExit(interactor, tacoGrab);
                }
            }
            tacoGrab.enabled = false;
        }

        // Desactivar Rigidbody del taco
        Rigidbody tacoRb = other.GetComponentInParent<Rigidbody>();
        if (tacoRb != null)
        {
            tacoRb.isKinematic = true;
            tacoRb.velocity = Vector3.zero;
            tacoRb.angularVelocity = Vector3.zero;
        }

        // Desactivar colisionadores para evitar interacciones físicas inestables con el plato
        Collider[] tacoColliders = tacoData.GetComponentsInChildren<Collider>();
        foreach (Collider col in tacoColliders)
        {
            col.enabled = false;
        }

        // Hacer el taco hijo del plato
        Transform tacoRoot = tacoData.transform;
        tacoRoot.SetParent(transform);

        // Posicionar en el snap point correspondiente
        int index = _tacosOnPlate.Count;
        if (tacoSnapPoints != null && index < tacoSnapPoints.Length)
        {
            tacoRoot.localPosition = tacoSnapPoints[index].localPosition + tacoPositionOffset;
            tacoRoot.localRotation = tacoSnapPoints[index].localRotation * Quaternion.Euler(tacoRotationOffset);
        }
        else
        {
            // Si no hay snap points, apilar verticalmente
            tacoRoot.localPosition = (Vector3.up * (0.02f * index)) + tacoPositionOffset;
            tacoRoot.localRotation = Quaternion.Euler(tacoRotationOffset);
        }

        // Agregar a la lista
        _tacosOnPlate.Add(tacoData);

        // Audio
        if (placeTacoSound != null && _audioSource != null)
            _audioSource.PlayOneShot(placeTacoSound);

        Debug.Log($"[PlateSocket] Taco de '{tacoData.meatType}' colocado en el plato. Total: {_tacosOnPlate.Count}");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA — Para PersonInteraction
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Retorna una copia de la lista de tacos en el plato.</summary>
    public List<TacoData> GetTacos()
    {
        return new List<TacoData>(_tacosOnPlate);
    }

    /// <summary>
    /// Cuenta cuántos tacos del plato coinciden con un tipo de carne.
    /// </summary>
    /// <param name="meatType">Tipo de carne a buscar.</param>
    /// <returns>Número de tacos que coinciden.</returns>
    public int CountMatchingTacos(string meatType)
    {
        int count = 0;
        foreach (TacoData taco in _tacosOnPlate)
        {
            if (taco != null && taco.meatType == meatType)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Devuelve un resumen del tipo de carne predominante.
    /// </summary>
    public string GetMeatSummary()
    {
        if (_tacosOnPlate.Count == 0) return "Vacío";

        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (TacoData taco in _tacosOnPlate)
        {
            if (taco == null) continue;
            if (!counts.ContainsKey(taco.meatType))
                counts[taco.meatType] = 0;
            counts[taco.meatType]++;
        }

        string predominant = "";
        int max = 0;
        foreach (var kvp in counts)
        {
            if (kvp.Value > max)
            {
                max = kvp.Value;
                predominant = kvp.Key;
            }
        }

        return counts.Count > 1 ? $"{predominant} (mixto)" : predominant;
    }
}
