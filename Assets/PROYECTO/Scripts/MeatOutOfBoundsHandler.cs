using UnityEngine;

/// <summary>
/// Controla la caída al vacío para trozos de carne sin colisión (no detectados por DroppableObject).
/// Si cae por debajo de minY, le cobra 1 peso de penalización al jugador y se destruye.
/// </summary>
public class MeatOutOfBoundsHandler : MonoBehaviour
{
    [Header("Límites")]
    [Tooltip("Altura mínima (Eje Y). Si el trozo de carne baja de aquí, se cobra penalización y se destruye.")]
    [SerializeField] private float minY = -1.0f;

    [Header("Penalización")]
    [Tooltip("Puntos/pesos que se restan al jugador cuando este trozo de carne cae.")]
    [SerializeField] private int penaltyPoints = 1;

    [Tooltip("Razón de la penalización.")]
    [SerializeField] private string penaltyReason = "Carne caída";

    private bool _hasFallen = false;

    void Update()
    {
        if (_hasFallen) return;

        if (transform.position.y < minY)
        {
            _hasFallen = true;
            TriggerFall();
        }
    }

    private void TriggerFall()
    {
        // Solo penalizar si hay partida en curso
        if (GameManager.Instance != null && GameManager.Instance.IsGameRunning)
        {
            GameManager.Instance.ApplyPenalty(penaltyPoints, penaltyReason);
        }

        Debug.Log($"[MeatOutOfBoundsHandler] '{gameObject.name}' cayó al vacío. Penalización: -{penaltyPoints}");
        
        Destroy(gameObject);
    }
}
