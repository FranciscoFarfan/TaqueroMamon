using UnityEngine;

/// <summary>
/// PersonInteraction — Reescrito para evaluar platos entregados por el jugador.
///
/// El NPC espera en su spot. Cuando el jugador acerca un plato (tag "Plato"),
/// el NPC lo evalúa contra su pedido (TacoOrder) y paga solo los tacos que coinciden.
///
/// Se coloca en los prefabs de NPC junto con PersonController.
/// </summary>
public class PersonInteraction : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Economía")]
    [Tooltip("Puntos por cada taco que coincide con el pedido.")]
    [SerializeField] private int pointsPerMatchingTaco = 10;

    [Header("Visual del pedido (Opcional)")]
    [Tooltip("Canvas WorldSpace sobre la cabeza del NPC para mostrar su pedido.")]
    [SerializeField] private GameObject orderBubble;

    [Tooltip("Texto del pedido sobre la cabeza del NPC (TextMeshPro).")]
    [SerializeField] private TMPro.TMP_Text orderBubbleText;

    // ═══════════════════════════════════════════════════════════════════════════
    //  ESTADO PRIVADO
    // ═══════════════════════════════════════════════════════════════════════════

    private Animator _animator;
    private PersonController _controller;
    private QueueManager _queueManager;
    private int _spotIndex;

    private TacoOrder _assignedOrder = null;
    private bool _hasReceivedPlate = false;
    private bool _isLeaving = false;
    private bool _hasArrived = false;

    // ═══════════════════════════════════════════════════════════════════════════
    //  PROPIEDADES PÚBLICAS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>El pedido asignado a este NPC.</summary>
    public TacoOrder AssignedOrder => _assignedOrder;

    /// <summary>¿Ya recibió un plato?</summary>
    public bool HasReceivedPlate => _hasReceivedPlate;

    // ═══════════════════════════════════════════════════════════════════════════
    //  UNITY
    // ═══════════════════════════════════════════════════════════════════════════

    void Start()
    {
        _animator = GetComponent<Animator>();
        _controller = GetComponent<PersonController>();
    }



    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Configura la información del spot y queue manager.
    /// Llamado por PersonSpawner al crear el NPC.
    /// </summary>
    public void SetSpotInfo(int index, QueueManager manager)
    {
        _spotIndex = index;
        _queueManager = manager;
    }

    /// <summary>
    /// Asigna un pedido al NPC.
    /// Llamado por PersonSpawner al crear el NPC.
    /// </summary>
    public void AssignOrder(TacoOrder order)
    {
        _assignedOrder = order;
        UpdateOrderBubble();
        Debug.Log($"[PersonInteraction] NPC '{gameObject.name}' → Pedido #{order.OrderId}: {order.TacoCount}x {order.MeatType}");
    }

    /// <summary>
    /// Notifica que el NPC llegó a su spot.
    /// Llamado por PersonController cuando llega al destino.
    /// </summary>
    public void NotifyArrived()
    {
        _hasArrived = true;
    }

    /// <summary>
    /// Retorna el pedido de este NPC (para UI u otros sistemas).
    /// </summary>
    public TacoOrder GetOrder()
    {
        return _assignedOrder;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  EVALUACIÓN DEL PLATO
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Evalúa el plato entregado contra el pedido del NPC.
    /// Solo paga los tacos que coinciden con el tipo de carne pedido.
    /// Llamado desde DeliveryArea.
    /// </summary>
    public void DeliverPlate(PlateSocket plate)
    {
        _hasReceivedPlate = true;

        if (_assignedOrder == null)
        {
            Debug.LogWarning($"[PersonInteraction] NPC '{gameObject.name}' no tiene pedido asignado.");
            DestroyPlateAndLeave(plate);
            return;
        }

        // Contar tacos que coinciden con el pedido
        int matchingTacos = plate.CountMatchingTacos(_assignedOrder.MeatType);
        int totalTacos = plate.TacoCount;

        Debug.Log($"[PersonInteraction] NPC '{gameObject.name}' evaluó plato: " +
                  $"{matchingTacos}/{totalTacos} coinciden con '{_assignedOrder.MeatType}' " +
                  $"(pedido: {_assignedOrder.TacoCount})");

        if (matchingTacos > 0)
        {
            // Pagar solo por los tacos que coinciden
            int reward = matchingTacos * pointsPerMatchingTaco;

            if (GameManager.Instance != null)
                GameManager.Instance.OrderCompleted(_assignedOrder.OrderId, reward);

            Debug.Log($"[PersonInteraction] NPC pagó ${reward} por {matchingTacos} tacos de {_assignedOrder.MeatType}.");
        }
        else
        {
            // No coincide ningún taco — pedido fallido
            if (GameManager.Instance != null)
                GameManager.Instance.OrderFailed(_assignedOrder.OrderId);

            Debug.Log($"[PersonInteraction] NPC rechazó el plato. Ningún taco era de {_assignedOrder.MeatType}.");
        }

        DestroyPlateAndLeave(plate);
    }

    /// <summary>
    /// Destruye el plato (con sus tacos) y hace que el NPC se vaya.
    /// </summary>
    private void DestroyPlateAndLeave(PlateSocket plate)
    {
        // Destruir el plato completo (incluye los tacos como hijos)
        Destroy(plate.gameObject);

        // Ocultar burbuja de pedido
        if (orderBubble != null)
            orderBubble.SetActive(false);

        // Iniciar secuencia de salida
        _isLeaving = true;

        // Animación
        if (_animator != null)
        {
            _animator.SetBool("isWaiting", false);
            _animator.SetBool("isGrabbing", true);
        }

        // Liberar el spot en la fila
        if (_queueManager != null)
            _queueManager.FreeSpot(_spotIndex);

        // Esperar y luego caminar hacia la salida
        Invoke(nameof(Leave), 1.5f);
    }

    private void Leave()
    {
        if (_animator != null)
        {
            _animator.SetBool("isGrabbing", false);
            _animator.SetBool("isWalking", true);
        }

        if (_controller != null)
            _controller.LeaveScene();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  VISUAL — BURBUJA DE PEDIDO
    // ═══════════════════════════════════════════════════════════════════════════

    private void UpdateOrderBubble()
    {
        if (orderBubble != null && _assignedOrder != null)
        {
            orderBubble.SetActive(true);
            if (orderBubbleText != null)
                orderBubbleText.text = $"{_assignedOrder.TacoCount}x {_assignedOrder.MeatType}";
        }
    }

}