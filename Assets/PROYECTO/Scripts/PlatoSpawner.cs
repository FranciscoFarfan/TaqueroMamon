using UnityEngine;

/// <summary>
/// PlatoSpawner — Mantiene siempre un plato disponible en el punto de spawn.
/// Cuando el jugador retira el plato actual, se genera uno nuevo instantáneamente.
/// </summary>
public class PlatoSpawner : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Prefab")]
    [Tooltip("Prefab del plato.")]
    [SerializeField] private GameObject platoPrefab;

    [Header("Spawn")]
    [Tooltip("Punto donde aparecen los platos nuevos.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Máximo de platos que pueden existir simultáneamente en la escena.")]
    [SerializeField] private int maxPlatos = 12;

    [Header("Configuración")]
    [Tooltip("Tag del plato (para detectar presencia y contar).")]
    [SerializeField] private string platoTag = "Plato";

    [Tooltip("Rotación inicial en el eje X para el plato.")]
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

        TrySpawnPlato();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  LÓGICA
    // ═══════════════════════════════════════════════════════════════════════════

    private void TrySpawnPlato()
    {
        // 1. Verificar si el punto de aparición está despejado
        if (!IsSpawnPointClear()) return;

        // 2. Verificar límite máximo de la escena
        int currentCount = GameObject.FindGameObjectsWithTag(platoTag).Length;
        if (currentCount >= maxPlatos) return;

        // 3. Generar el plato
        SpawnOnePlato();
    }

    private bool IsSpawnPointClear()
    {
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        
        // Buscamos cualquier objeto con el tag de plato en el radio de detección
        Collider[] colliders = Physics.OverlapSphere(pos, detectionRadius);
        foreach (var col in colliders)
        {
            if (col.CompareTag(platoTag)) return false;
        }
        return true;
    }

    private void SpawnOnePlato()
    {
        if (platoPrefab == null)
        {
            Debug.LogError("[PlatoSpawner] No se asignó el prefab de plato.");
            return;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rotation = Quaternion.Euler(spawnRotationX, 0f, 0f);

        Instantiate(platoPrefab, pos, rotation);
        Debug.Log($"[PlatoSpawner] Plato repuesto en {pos}");
    }
}
