using UnityEngine;

public class PersonInteraction : MonoBehaviour
{
    public float detectionRadius = 1.5f;  // distancia para detectar el taco
    private Animator animator;
    private PersonController controller;
    private QueueManager queueManager;
    private int spotIndex;
    private bool isLeaving = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<PersonController>();
    }

    // Llama esto desde PersonController cuando llegue al spot
    public void SetSpotInfo(int index, QueueManager manager)
    {
        spotIndex = index;
        queueManager = manager;
    }

    void Update()
    {
        if (isLeaving) return;

        // Busca objetos con tag "taco" cercanos
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("taco"))
            {
                GrabAndLeave(hit.gameObject);
                break;
            }
        }
    }

    void GrabAndLeave(GameObject taco)
    {
        isLeaving = true;

        // Animación de agarrar
        animator.SetBool("isWaiting", false);
        animator.SetBool("isGrabbing", true);

        // Destruye el taco
        Destroy(taco);

        // Libera el lugar en la fila
        queueManager.FreeSpot(spotIndex);

        // Espera que termine la animación de agarrar y luego se va
        Invoke("Leave", 1.5f); // ajusta el tiempo según dure tu animación
    }

    void Leave()
    {
        animator.SetBool("isGrabbing", false);
        animator.SetBool("isWalking", true);

        // Le dice al PersonController que camine hacia afuera de la escena
        controller.LeaveScene();
    }
}