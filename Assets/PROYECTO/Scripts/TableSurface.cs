using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// TableSurface — Mantiene a los objetos interactuables sobre la mesa.
/// Si un objeto atraviesa el BoxCollider (Trigger) de la mesa y no está siendo
/// agarrado por el jugador, lo empuja hacia arriba.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class TableSurface : MonoBehaviour
{
    [Tooltip("Distancia adicional hacia arriba desde la superficie del collider para colocar el objeto.")]
    [SerializeField] private float surfaceOffset = 0.02f;

    private BoxCollider _boxCollider;

    void Awake()
    {
        _boxCollider = GetComponent<BoxCollider>();
        _boxCollider.isTrigger = true; // Asegurar que sea trigger
    }

    void OnTriggerStay(Collider other)
    {
        // Buscar si tiene Rigidbody y si es interactuable
        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        XRGrabInteractable grabInteractable = rb.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null) return;

        // Si el jugador lo está sosteniendo, no forzar su posición
        if (grabInteractable.isSelected) return;

        // Calcular la altura de la superficie superior del BoxCollider
        float surfaceY = _boxCollider.bounds.max.y;

        // Obtener el punto más bajo del objeto (aproximado por su collider principal)
        float objectBottomY = other.bounds.min.y;

        // Si el punto más bajo del objeto está por debajo de la superficie
        if (objectBottomY < surfaceY)
        {
            // Calcular cuánto hay que subirlo
            float difference = surfaceY - objectBottomY;
            
            // Subir el objeto, aplicando el offset para que quede un poco arriba
            Vector3 newPosition = rb.position + Vector3.up * (difference + surfaceOffset);
            
            // Teletransportar el objeto hacia arriba
            rb.position = newPosition;
            
            // Detener su velocidad vertical para que no siga cayendo
            Vector3 vel = rb.velocity;
            if (vel.y < 0) vel.y = 0;
            rb.velocity = vel;
        }
    }
}
