using UnityEngine;

/// <summary>
/// PersonController — Controla el movimiento del NPC hacia su spot en la fila
/// y hacia la salida cuando termina.
///
/// Modificado para notificar a PersonInteraction cuando llega al spot.
/// </summary>
public class PersonController : MonoBehaviour
{
    public GameObject exitPoint;
    public float speed = 2f;
    public float stoppingDistance = 0.2f;

    private Transform destination;
    private int spotIndex;
    private QueueManager queueManager;
    private Animator animator;
    private PersonInteraction interaction;
    private bool arrived = false;
    private bool isLeaving = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        interaction = GetComponent<PersonInteraction>();
        animator.SetBool("isWalking", true);
    }

    public void SetDestination(Transform dest, int index, QueueManager manager)
    {
        destination = dest;
        spotIndex = index;
        queueManager = manager;
    }

    public void LeaveScene()
    {
        arrived = false;
        isLeaving = true;
        destination = exitPoint.transform;
    }

    void Update()
    {
        if (arrived || destination == null) return;

        Vector3 dir = (destination.position - transform.position).normalized;

        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }

        transform.position += dir * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, destination.position) <= stoppingDistance)
        {
            if (isLeaving)
            {
                Destroy(gameObject);
                return;
            }

            // Llegó al spot de la fila
            arrived = true;
            transform.position = destination.position;
            transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 87, 0);
            animator.SetBool("isWalking", false);
            animator.SetBool("isWaiting", true);

            // Notificar a PersonInteraction que llegó
            if (interaction != null)
                interaction.NotifyArrived();
        }
    }
}