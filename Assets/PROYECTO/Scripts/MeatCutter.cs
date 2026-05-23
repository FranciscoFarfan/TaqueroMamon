using UnityEngine;

/// <summary>
/// MeatCutter — Script para cortar carne al pastor del trompo.
/// Se coloca en el objeto Meat_Shepherd-material (que ya tiene un CapsuleCollider trigger).
///
/// Cuando un objeto con el tag "Cuchillo" entra en el trigger,
/// instancia un trozo de pastor y lo lanza en la dirección del corte
/// (basada en la normal/forward del cuchillo) para que el jugador lo atrape.
/// </summary>
public class MeatCutter : MonoBehaviour
{
    [Header("Prefab del trozo de carne")]
    [Tooltip("Arrastra aquí el prefab Pastor desde Assets/PROYECTO/Prefabs/Interactables/")]
    public GameObject pastorPrefab;

    [Header("Configuración de lanzamiento")]
    [Tooltip("Fuerza con la que se lanza el trozo de carne (impulso).")]
    public float fuerzaLanzamiento = 3f;

    [Tooltip("Si true, lanza en la dirección forward del cuchillo. Si false, lanza hacia arriba con variación aleatoria.")]
    public bool useKnifeDirection = true;

    [Tooltip("Cantidad de variación aleatoria en la dirección de lanzamiento.")]
    [Range(0f, 1f)]
    public float randomSpread = 0.3f;

    [Header("Configuración de corte")]
    [Tooltip("Tiempo de espera entre cortes (segundos) para no spamear trozos.")]
    public float cooldownCorte = 0.5f;

    [Header("Punto de spawn")]
    [Tooltip("Transform opcional para definir el punto de spawn. Si se asigna, se usa su posición.")]
    public Transform puntoDeSpawn;

    [Tooltip("Offset local respecto al trompo para el punto de spawn (se usa si puntoDeSpawn es null). Ajústalo en el Inspector.")]
    public Vector3 spawnOffset = new Vector3(0f, -0.2f, 0.3f);

    [Header("Audio (opcional)")]
    [Tooltip("Sonido al cortar.")]
    public AudioClip cutSound;

    // Control de cooldown
    private float ultimoCorte = -Mathf.Infinity;
    private AudioSource _audioSource;

    private void Start()
    {
        if (pastorPrefab == null)
        {
            Debug.LogError("[MeatCutter] ¡No se asignó el prefab Pastor! Arrástralo en el Inspector.");
        }

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.spatialBlend = 1f;
        _audioSource.playOnAwake = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Solo reaccionar a objetos con el tag "Cuchillo"
        if (!other.CompareTag("Cuchillo")) return;

        // Verificar cooldown
        if (Time.time - ultimoCorte < cooldownCorte) return;

        if (pastorPrefab == null)
        {
            Debug.LogError("[MeatCutter] pastorPrefab es NULL, no se puede cortar.");
            return;
        }

        // Solo cortar si hay partida en curso
        if (GameManager.Instance != null && !GameManager.Instance.IsGameRunning) return;

        ultimoCorte = Time.time;

        // Determinar posición de spawn del trozo — SIEMPRE FIJA
        Vector3 posicionSpawn;
        if (puntoDeSpawn != null)
        {
            posicionSpawn = puntoDeSpawn.position;
        }
        else
        {
            // Usar offset local relativo al trompo (configurable en el Inspector)
            posicionSpawn = transform.position + transform.TransformDirection(spawnOffset);
        }

        // Instanciar el trozo de carne
        GameObject trozo = Instantiate(pastorPrefab, posicionSpawn, Random.rotation);

        // Calcular la dirección de lanzamiento
        Vector3 direccion = CalcularDireccionLanzamiento(other.transform);

        // Asegurar que el trozo tiene Rigidbody
        Rigidbody rb = trozo.GetComponent<Rigidbody>();
        if (rb == null)
            rb = trozo.AddComponent<Rigidbody>();

        // Asegurar que no es kinematic para que vuele con física
        rb.isKinematic = false;
        rb.useGravity = true;

        // Lanzar el trozo
        rb.AddForce(direccion * fuerzaLanzamiento, ForceMode.Impulse);

        // Agregar un poco de rotación para efecto visual
        rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);

        // Audio
        if (cutSound != null && _audioSource != null)
            _audioSource.PlayOneShot(cutSound);

        Debug.Log($"[MeatCutter] Trozo de pastor cortado y lanzado. Dirección: {direccion}");
    }

    /// <summary>
    /// Calcula la dirección de lanzamiento basándose en la orientación del cuchillo.
    /// </summary>
    private Vector3 CalcularDireccionLanzamiento(Transform cuchillo)
    {
        Vector3 direccion;

        if (useKnifeDirection)
        {
            // Usar la dirección opuesta al forward del cuchillo
            direccion = -cuchillo.forward;

            // Asegurar que tenga componente hacia ABAJO (-y) para que caiga a la tortilla
            if (direccion.y > -0.1f)
                direccion.y = -0.3f;

            direccion.Normalize();
        }
        else
        {
            // Dirección por defecto: hacia arriba y un poco hacia adelante
            direccion = (Vector3.up + cuchillo.right * 0.5f).normalized;
        }

        // Agregar variación aleatoria
        direccion += Random.insideUnitSphere * randomSpread;
        direccion.Normalize();

        return direccion;
    }

    /// <summary>
    /// Visualiza el punto de spawn en el Editor para facilitar la configuración.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Vector3 spawnPos;
        if (puntoDeSpawn != null)
        {
            spawnPos = puntoDeSpawn.position;
        }
        else
        {
            spawnPos = transform.position + transform.TransformDirection(spawnOffset);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(spawnPos, 0.05f);
        Gizmos.DrawLine(transform.position, spawnPos);
    }
}
