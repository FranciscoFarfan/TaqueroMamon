using UnityEngine;

public class PersonController : MonoBehaviour
{
    public GameObject exitPoint;
    public float speed = 2f;
    public float stoppingDistance = 0.2f;

    private Transform destination;
    private int spotIndex;
    private QueueManager queueManager;
    private Animator animator;
    private bool arrived = false;
    private bool isLeaving = false; // ← nueva variable para saber si está saliendo

    void Start()
    {
        animator = GetComponent<Animator>();
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
        isLeaving = true; // ← marca que está saliendo
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
            if (isLeaving) // si está saliendo, se destruye
            {
                Destroy(gameObject);
                return;
            }

            // llegó al spot de la fila
            arrived = true;
            transform.position = destination.position;
            transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 87, 0);
            animator.SetBool("isWalking", false);
            animator.SetBool("isWaiting", true);
        }
    }
}