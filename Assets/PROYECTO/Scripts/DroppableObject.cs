using UnityEngine;

/// <summary>
/// DroppableObject — Se adjunta a cualquier objeto que pueda caerse y penalizar
/// al jugador (platos, tacos, carne, tortillas, etc.).
///
/// Cuando detecta que cayó al suelo (o a una zona de penalización),
/// le avisa al GameManager, resta los puntos y se autodestruye.
///
/// Configuración mínima:
///  1. Agrega este componente al objeto.
///  2. Asigna el valor de penaltyPoints en el Inspector.
///  3. Asegúrate de que el suelo (o la zona de caída) tenga el tag "Floor".
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DroppableObject : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Penalización")]
    [Tooltip("Puntos que se le restan al jugador cuando este objeto cae.")]
    [SerializeField] private int penaltyPoints = 5;

    [Tooltip("Razón que aparecerá en el log (para debug).")]
    [SerializeField] private string penaltyReason = "Objeto caído";

    [Header("Detección de caída")]
    [Tooltip("Tag del objeto que representa el suelo / zona de penalización.")]
    [SerializeField] private string floorTag = "Floor";

    [Tooltip("(Opcional) Si está activado, también penaliza cuando la velocidad\n" +
             "supera el umbral Y negativo (útil si el suelo no tiene collider).")]
    [SerializeField] private bool useVelocityFallback = false;

    [Tooltip("Velocidad mínima hacia abajo (m/s) para considerar que el objeto cayó.")]
    [SerializeField] private float velocityThreshold = -3f;

    // ═══════════════════════════════════════════════════════════════════════════
    //  ESTADO PRIVADO
    // ═══════════════════════════════════════════════════════════════════════════

    private bool _hasFallen = false;
    private Rigidbody _rb;

    // ═══════════════════════════════════════════════════════════════════════════
    //  UNITY LOOP
    // ═══════════════════════════════════════════════════════════════════════════

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (_hasFallen || !useVelocityFallback) return;

        // Fallback: detectar caída libre por velocidad
        if (_rb.velocity.y < velocityThreshold)
            TriggerFall();
    }

    /// <summary>
    /// Se llama cuando el objeto colisiona con algo.
    /// Si el tag del objeto colisionado es "Floor", considera que cayó.
    /// </summary>
    void OnCollisionEnter(Collision collision)
    {
        if (_hasFallen) return;

        if (collision.gameObject.CompareTag(floorTag))
            TriggerFall();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  LÓGICA
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Notifica al GameManager y destruye este objeto.
    /// Puedes llamar este método manualmente desde otros scripts si es necesario.
    /// </summary>
    public void TriggerFall()
    {
        if (_hasFallen) return;
        _hasFallen = true;

        // Solo penalizar si hay partida en curso
        if (GameManager.Instance != null && GameManager.Instance.IsGameRunning)
            GameManager.Instance.ApplyPenalty(penaltyPoints, penaltyReason);

        Debug.Log($"[DroppableObject] '{gameObject.name}' cayó. Penalización: -{penaltyPoints}");

        Destroy(gameObject);
    }
}
