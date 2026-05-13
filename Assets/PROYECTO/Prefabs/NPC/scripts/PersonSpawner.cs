using UnityEngine;

/// <summary>
/// PersonSpawner — Genera NPCs cuando hay lugares libres en la fila.
///
/// Modificado para:
///   - Solo spawnear si GameManager.IsGameRunning
///   - Asignar un pedido (TacoOrder) del GameManager a cada NPC
///   - Destruir todos los NPCs activos al terminar la partida (OnGameOver)
/// </summary>
public class PersonSpawner : MonoBehaviour
{
    public GameObject[] peoplePrefabs;
    public Transform spawnPoint;
    public QueueManager queueManager;
    public GameObject exitPoint;

    [Tooltip("Cada cuánto revisa si hay lugar libre (segundos).")]
    public float checkInterval = 2f;

    private float timer;
    private bool _subscribedToGameOver = false;

    void Update()
    {
        // Suscribirse a OnGameOver cuando GameManager esté disponible
        if (!_subscribedToGameOver && GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += OnGameOver;
            _subscribedToGameOver = true;
        }

        // Solo spawnear si el juego está en curso
        if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning) return;

        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            TrySpawnPerson();
        }
    }

    void OnDisable()
    {
        if (_subscribedToGameOver && GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= OnGameOver;
            _subscribedToGameOver = false;
        }
    }

    /// <summary>
    /// Al terminar la partida, destruye todos los NPCs activos y limpia la cola.
    /// </summary>
    private void OnGameOver(int finalScore)
    {
        if (queueManager == null || queueManager.occupants == null) return;

        for (int i = 0; i < queueManager.occupants.Length; i++)
        {
            if (queueManager.occupants[i] != null)
            {
                Destroy(queueManager.occupants[i]);
                queueManager.FreeSpot(i);
            }
        }

        timer = 0f;
        Debug.Log("[PersonSpawner] Game Over — Todos los NPCs eliminados.");
    }

    void TrySpawnPerson()
    {
        int freeSpot = queueManager.GetFreeSpotIndex();
        if (freeSpot == -1) return;

        // Verificar que hay pedidos disponibles para asignar
        if (GameManager.Instance.ActiveOrders.Count == 0) return;

        int randomIndex = Random.Range(0, peoplePrefabs.Length);
        GameObject person = Instantiate(peoplePrefabs[randomIndex], spawnPoint.position, Quaternion.Euler(0, 90, 0));

        // Configurar movimiento
        PersonController controller = person.GetComponent<PersonController>();
        controller.exitPoint = exitPoint;
        controller.SetDestination(queueManager.queueSpots[freeSpot], freeSpot, queueManager);

        // Configurar interacción y asignar pedido
        PersonInteraction interaction = person.GetComponent<PersonInteraction>();
        interaction.SetSpotInfo(freeSpot, queueManager);

        // Asignar el pedido correspondiente al spot
        // Los pedidos van mapeados 1:1 con los spots de la fila
        if (freeSpot < GameManager.Instance.ActiveOrders.Count)
        {
            TacoOrder order = GameManager.Instance.ActiveOrders[freeSpot];
            interaction.AssignOrder(order);
        }
        else if (GameManager.Instance.ActiveOrders.Count > 0)
        {
            // Si por alguna razón el spot no mapea, asignar el primer pedido disponible
            interaction.AssignOrder(GameManager.Instance.ActiveOrders[0]);
        }

        queueManager.OccupySpot(freeSpot, person);
    }
}