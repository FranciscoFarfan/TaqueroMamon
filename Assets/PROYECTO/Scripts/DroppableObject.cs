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

        bool isGameRunning = GameManager.Instance != null && GameManager.Instance.IsGameRunning;

        // ─── Plato caído ───
        PlateSocket plate = GetComponent<PlateSocket>();
        if (plate != null)
        {
            int tacoCount = plate.TacoCount;
            penaltyPoints = 5 + (tacoCount * 10);
            penaltyReason = $"Plato caído ({tacoCount} tacos)";

            if (isGameRunning)
                GameManager.Instance.ReportPlateDropped(tacoCount, penaltyPoints, penaltyReason);

            Debug.Log($"[DroppableObject] '{gameObject.name}' (Plato) cayó con {tacoCount} tacos. (-{penaltyPoints})");
            Destroy(gameObject);
            return;
        }

        // ─── Tortilla caída ───
        TortillaManager tortilla = GetComponent<TortillaManager>();
        if (tortilla != null)
        {
            // Si ya estaba quemada, la penalización ya se aplicó en TortillaManager
            if (tortilla.CurrentState == TortillaManager.TortillaState.Burnt)
            {
                Debug.Log($"[DroppableObject] '{gameObject.name}' (Tortilla quemada) cayó. Sin penalización adicional.");
            }
            else
            {
                if (isGameRunning)
                    GameManager.Instance.ReportTortillaLost(penaltyPoints, "Tortilla caída");
            }

            Destroy(gameObject);
            return;
        }

        // ─── Pastor caído ───
        if (gameObject.CompareTag("Pastor"))
        {
            if (isGameRunning)
                GameManager.Instance.ReportMeatDropped(penaltyPoints, "Pastor caído");

            Debug.Log($"[DroppableObject] '{gameObject.name}' (Pastor) cayó. (-{penaltyPoints})");
            Destroy(gameObject);
            return;
        }

        // ─── Taco caído ───
        if (gameObject.CompareTag("taco"))
        {
            if (isGameRunning)
                GameManager.Instance.ReportTacosLost(1, penaltyPoints, "Taco caído");

            Debug.Log($"[DroppableObject] '{gameObject.name}' (Taco) cayó. (-{penaltyPoints})");
            Destroy(gameObject);
            return;
        }

        // ─── Genérico (cualquier otro objeto) ───
        if (isGameRunning)
            GameManager.Instance.ApplyPenalty(penaltyPoints, penaltyReason);

        Debug.Log($"[DroppableObject] '{gameObject.name}' cayó. Penalización genérica: (-{penaltyPoints})");
        Destroy(gameObject);
    }
}
