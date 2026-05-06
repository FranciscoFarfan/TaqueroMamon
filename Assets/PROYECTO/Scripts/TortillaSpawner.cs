using UnityEngine;

/// <summary>
/// TortillaSpawner — Mantiene siempre una tortilla disponible en el punto de spawn.
/// Cuando el jugador retira la tortilla actual, se genera una nueva instantáneamente.
/// </summary>
public class TortillaSpawner : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Prefab")]
    [Tooltip("Prefab de la tortilla.")]
    [SerializeField] private GameObject tortillaPrefab;

    [Header("Spawn")]
    [Tooltip("Punto donde aparecen las tortillas nuevas.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Máximo de tortillas que pueden existir simultáneamente en la escena.")]
    [SerializeField] private int maxTortillas = 12;

    [Header("Configuración")]
    [Tooltip("Tag de la tortilla (para detectar presencia y contar).")]
    [SerializeField] private string tortillaTag = "Tortilla";

    [Tooltip("Rotación inicial en el eje X para la tortilla.")]
    [SerializeField] private float spawnRotationX = 0f;

    [Header("Detección")]
    [Tooltip("Radio para detectar si el punto de spawn está despejado.")]
    [SerializeField] private float detectionRadius = 0.1f;

    // ═══════════════════════════════════════════════════════════════════════════
    //  UNITY
    // ═══════════════════════════════════════════════════════════════════════════

    void Update()
    {
        // Solo funciona si el juego está en marcha
        if (GameManager.Instance != null && !GameManager.Instance.IsGameRunning) return;

        TrySpawnTortilla();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  LÓGICA
    // ═══════════════════════════════════════════════════════════════════════════

    private void TrySpawnTortilla()
    {
        // 1. Verificar si el punto de aparición está despejado
        if (!IsSpawnPointClear()) return;

        // 2. Verificar límite máximo de la escena
        int currentCount = GameObject.FindGameObjectsWithTag(tortillaTag).Length;
        if (currentCount >= maxTortillas) return;

        // 3. Generar la tortilla
        SpawnOneTortilla();
    }

    private bool IsSpawnPointClear()
    {
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        
        // Buscamos cualquier objeto con el tag de tortilla en el radio de detección
        Collider[] colliders = Physics.OverlapSphere(pos, detectionRadius);
        foreach (var col in colliders)
        {
            if (col.CompareTag(tortillaTag)) return false;
        }
        return true;
    }

    private void SpawnOneTortilla()
    {
        if (tortillaPrefab == null)
        {
            Debug.LogError("[TortillaSpawner] No se asignó el prefab de tortilla.");
            return;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rotation = Quaternion.Euler(spawnRotationX, 0f, 0f);

        Instantiate(tortillaPrefab, pos, rotation);
        Debug.Log($"[TortillaSpawner] Tortilla repuesta en {pos}");
    }
}
