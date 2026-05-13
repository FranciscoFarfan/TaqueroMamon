using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ComalSocket — Se coloca en el comal (como un solo trigger gigante).
/// Detecta múltiples tortillas que entran o salen del trigger y les avisa a sus TortillaManager.
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

    // ═══════════════════════════════════════════════════════════════════════════
    //  ESTADO PRIVADO
    // ═══════════════════════════════════════════════════════════════════════════

    // Lista para rastrear TODAS las tortillas que están actualmente tocando el comal
    private List<TortillaManager> _tortillasEnComal = new List<TortillaManager>();

    // ═══════════════════════════════════════════════════════════════════════════
    //  PROPIEDADES PÚBLICAS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Cuántas tortillas hay cocinándose ahora mismo.</summary>
    public int TortillasCount => _tortillasEnComal.Count;

    // ═══════════════════════════════════════════════════════════════════════════
    //  TRIGGERS
    // ═══════════════════════════════════════════════════════════════════════════

    void OnTriggerEnter(Collider other)
    {
        // Solo aceptar objetos con tag de tortilla
        if (!other.CompareTag(tortillaTag)) return;

        TortillaManager tortilla = other.GetComponentInParent<TortillaManager>();
        if (tortilla == null) return;

        // Si por alguna razón la tortilla ya está en la lista, la ignoramos para no duplicar
        if (_tortillasEnComal.Contains(tortilla)) return;

        // Si la tortilla ya está quemada, no hacemos nada
        if (tortilla.CurrentState == TortillaManager.TortillaState.Burnt) return;

        // Agregarla a la lista y empezar a cocinarla
        _tortillasEnComal.Add(tortilla);
        tortilla.StartCooking();

        Debug.Log($"[ComalSocket] Tortilla '{other.gameObject.name}' entró al comal. Total cocinándose: {_tortillasEnComal.Count}");
    }

    void OnTriggerExit(Collider other)
    {
        // Ignorar si no es una tortilla
        if (!other.CompareTag(tortillaTag)) return;

        TortillaManager tortilla = other.GetComponentInParent<TortillaManager>();
        if (tortilla == null) return;

        // Verificamos si la teníamos registrada en nuestra lista
        if (_tortillasEnComal.Contains(tortilla))
        {
            tortilla.StopCooking();
            _tortillasEnComal.Remove(tortilla);
            Debug.Log($"[ComalSocket] Tortilla '{other.gameObject.name}' salió del comal. Total cocinándose: {_tortillasEnComal.Count}");
        }
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