using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

/// <summary>
/// Script para cortar carne al pastor.
/// Se coloca en el objeto Meat_Shepherd-material (que ya tiene un CapsuleCollider trigger).
/// Cuando un objeto con el tag "Cuchillo" entra en el trigger,
/// busca la mejor tortilla (prioridad: en mano > más cercana)
/// y el trozo de Pastor vuela en curva hacia ella.
/// </summary>
public class MeatCutter : MonoBehaviour
{
    [Header("Prefab del trozo de carne")]
    [Tooltip("Arrastra aquí el prefab Pastor desde Assets/PROYECTO/Prefabs/Interactables/")]
    public GameObject pastorPrefab;

    [Header("Configuración de la curva")]
    [Tooltip("Qué tan alto sube el trozo en el punto más alto del arco (metros)")]
    public float alturaCurva = 1.0f;

    [Tooltip("Tiempo que tarda el trozo en viajar de la carne a la tortilla (segundos)")]
    public float tiempoDeVuelo = 0.8f;

    [Header("Configuración de corte")]
    [Tooltip("Tiempo de espera entre cortes (segundos) para no spamear trozos")]
    public float cooldownCorte = 0.5f;

    [Header("Punto de spawn (opcional)")]
    [Tooltip("Si se deja vacío, el trozo aparece en la posición del contacto con el cuchillo")]
    public Transform puntoDeSpawn;

    // Control de cooldown
    private float ultimoCorte = -Mathf.Infinity;

    private void Start()
    {
        if (pastorPrefab == null)
        {
            Debug.LogError("[MeatCutter] ¡No se asignó el prefab Pastor! Arrástralo en el Inspector.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("[MeatCutter] OnTriggerEnter detectó: " + other.gameObject.name + " (tag: " + other.tag + ")");

        // Solo reaccionar a objetos con el tag "Cuchillo"
        if (!other.CompareTag("Cuchillo"))
        {
            Debug.Log("[MeatCutter] Objeto ignorado, no tiene tag 'Cuchillo'.");
            return;
        }

        Debug.Log("[MeatCutter] >>> CUCHILLO DETECTADO! <<<");

        // Verificar cooldown
        if (Time.time - ultimoCorte < cooldownCorte)
        {
            Debug.Log("[MeatCutter] Cooldown activo, faltan " + (cooldownCorte - (Time.time - ultimoCorte)).ToString("F2") + "s");
            return;
        }

        if (pastorPrefab == null)
        {
            Debug.LogError("[MeatCutter] pastorPrefab es NULL, no se puede cortar.");
            return;
        }

        ultimoCorte = Time.time;

        // Determinar posición de spawn del trozo
        Vector3 posicionSpawn;
        if (puntoDeSpawn != null)
        {
            posicionSpawn = puntoDeSpawn.position;
            Debug.Log("[MeatCutter] Spawn en punto personalizado: " + posicionSpawn);
        }
        else
        {
            posicionSpawn = other.ClosestPoint(transform.position);
            Debug.Log("[MeatCutter] Spawn en punto de contacto: " + posicionSpawn);
        }

        // Buscar la mejor tortilla
        Debug.Log("[MeatCutter] Buscando mejor tortilla...");
        Transform tortilla = BuscarMejorTortilla();

        if (tortilla == null)
        {
            Debug.LogWarning("[MeatCutter] No se encontró ninguna Tortilla con tag 'Tortilla' en la escena.");
            return;
        }

        // Instanciar el trozo de carne
        GameObject trozo = Instantiate(pastorPrefab, posicionSpawn, Random.rotation);
        Debug.Log("[MeatCutter] Trozo de pastor instanciado en: " + posicionSpawn);
        Debug.Log("[MeatCutter] >>> LANZANDO trozo hacia tortilla: " + tortilla.name + " en posición: " + tortilla.position + " <<<");

        // Volar en curva hacia la tortilla
        StartCoroutine(VolarEnCurva(trozo, posicionSpawn, tortilla));
    }

    /// <summary>
    /// Busca la mejor tortilla disponible.
    /// Prioridad 1: Una tortilla que el jugador tenga en la mano (XR Grab isSelected).
    /// Prioridad 2: La tortilla más cercana a la carne.
    /// </summary>
    private Transform BuscarMejorTortilla()
    {
        GameObject[] tortillas = GameObject.FindGameObjectsWithTag("Tortilla");
        Debug.Log("[MeatCutter] Tortillas encontradas en escena: " + tortillas.Length);

        if (tortillas.Length == 0)
        {
            Debug.LogWarning("[MeatCutter] No hay ningún objeto con tag 'Tortilla' en la escena.");
            return null;
        }

        // Prioridad 1: buscar una tortilla que esté agarrada por el jugador
        foreach (GameObject t in tortillas)
        {
            XRGrabInteractable grab = t.GetComponent<XRGrabInteractable>();
            if (grab != null && grab.isSelected)
            {
                Debug.Log("[MeatCutter] >>> Tortilla EN MANO encontrada: " + t.name + " (prioridad 1) <<<");
                return t.transform;
            }
        }

        Debug.Log("[MeatCutter] Ninguna tortilla en mano, buscando la más cercana...");

        // Prioridad 2: la tortilla más cercana a la carne
        Transform masCercana = null;
        float distanciaMinima = Mathf.Infinity;

        foreach (GameObject t in tortillas)
        {
            float distancia = Vector3.Distance(transform.position, t.transform.position);
            Debug.Log("[MeatCutter]   - " + t.name + " a distancia: " + distancia.ToString("F2") + "m");
            if (distancia < distanciaMinima)
            {
                distanciaMinima = distancia;
                masCercana = t.transform;
            }
        }

        Debug.Log("[MeatCutter] >>> Tortilla más cercana: " + masCercana.name + " a " + distanciaMinima.ToString("F2") + "m (prioridad 2) <<<");
        return masCercana;
    }

    /// <summary>
    /// Mueve el trozo en un arco Bézier cuadrático desde la carne hasta la tortilla.
    /// Sigue la posición actual de la tortilla (por si está en la mano y se mueve).
    /// </summary>
    private IEnumerator VolarEnCurva(GameObject trozo, Vector3 inicio, Transform tortillaTarget)
    {
        Debug.Log("[MeatCutter] Iniciando vuelo en curva. Altura: " + alturaCurva + "m, Duración: " + tiempoDeVuelo + "s");
        float tiempoTranscurrido = 0f;
        bool logMitad = false;

        while (tiempoTranscurrido < tiempoDeVuelo)
        {
            if (trozo == null)
            {
                Debug.LogWarning("[MeatCutter] El trozo fue destruido durante el vuelo.");
                yield break;
            }

            // Si la tortilla fue destruida durante el vuelo, destruir el trozo
            if (tortillaTarget == null)
            {
                Debug.LogWarning("[MeatCutter] La tortilla destino fue destruida durante el vuelo. Destruyendo trozo.");
                Destroy(trozo);
                yield break;
            }

            tiempoTranscurrido += Time.deltaTime;
            float t = Mathf.Clamp01(tiempoTranscurrido / tiempoDeVuelo);

            // Log a la mitad del vuelo
            if (!logMitad && t >= 0.5f)
            {
                Debug.Log("[MeatCutter] Trozo a mitad de vuelo (punto más alto del arco)");
                logMitad = true;
            }

            // Destino actualizado en tiempo real (la tortilla puede moverse si está en la mano)
            Vector3 destino = tortillaTarget.position;

            // Punto de control del arco (punto medio elevado)
            Vector3 puntoMedio = (inicio + destino) / 2f;
            puntoMedio.y += alturaCurva;

            // Bezier cuadrático: (1-t)²·A + 2·(1-t)·t·B + t²·C
            Vector3 posicion =
                Mathf.Pow(1f - t, 2f) * inicio +
                2f * (1f - t) * t * puntoMedio +
                Mathf.Pow(t, 2f) * destino;

            trozo.transform.position = posicion;

            // Rotar durante el vuelo para efecto visual
            trozo.transform.Rotate(Vector3.forward, 360f * Time.deltaTime, Space.Self);

            yield return null;
        }

        // Posición final: sobre la tortilla
        if (trozo != null && tortillaTarget != null)
        {
            trozo.transform.position = tortillaTarget.position;

            // Hacer que el trozo sea hijo de la tortilla para que se mueva con ella
            trozo.transform.SetParent(tortillaTarget);

            Debug.Log("[MeatCutter] >>> TROZO LLEGÓ A LA TORTILLA: " + tortillaTarget.name + " <<<");
        }
    }
}
