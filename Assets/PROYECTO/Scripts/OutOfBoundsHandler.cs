using UnityEngine;

/// <summary>
/// Destruye el objeto si sale de los límites permitidos (caída al vacío).
/// </summary>
public class OutOfBoundsHandler : MonoBehaviour
{
    [Header("Límites")]
    [Tooltip("Altura mínima (Eje Y). Si el objeto baja de aquí, se destruye.")]
    [SerializeField] private float minY = -1.0f;

    void Update()
    {
        if (transform.position.y < minY)
        {
            Destroy(gameObject);
        }
    }
}
