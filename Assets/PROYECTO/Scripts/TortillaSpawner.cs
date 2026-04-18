using UnityEngine;

/// <summary>
/// TortillaSpawner — Genera tortillas nuevas periódicamente
/// para que el jugador siempre tenga disponibles.
///
/// Configuración:
///   1. Colocar en un GameObject cerca del comal/área de trabajo.
///   2. Asignar el prefab de tortilla y el punto de spawn.
///   3. Solo genera tortillas durante la partida.
/// </summary>
public class TortillaSpawner : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Prefab")]
    [Tooltip("Prefab de la tortilla (debe tener tag 'Tortilla', TortillaManager y TacoAssembler).")]
    [SerializeField] private GameObject tortillaPrefab;

    [Header("Spawn")]
    [Tooltip("Punto donde aparecen las tortillas nuevas.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Máximo de tortillas que pueden existir simultáneamente en la escena.")]
    [SerializeField] private int maxTortillas = 12;

    [Tooltip("Intervalo en segundos entre cada spawn.")]
    [SerializeField] private float spawnInterval = 10f;

    [Header("Configuración")]
    [Tooltip("Tag de la tortilla (para contar las existentes).")]
    [SerializeField] private string tortillaTag = "Tortilla";

    [Tooltip("Si true, genera un lote inicial al empezar la partida.")]
    [SerializeField] private bool spawnInitialBatch = true;

    [Tooltip("Número de tortillas iniciales.")]
    [SerializeField] private int initialBatchSize = 6;

    // ═══════════════════════════════════════════════════════════════════════════
    //  ESTADO PRIVADO
    // ═══════════════════════════════════════════════════════════════════════════

    private float _timer = 0f;
    private bool _initialBatchSpawned = false;

    // ═══════════════════════════════════════════════════════════════════════════
    //  UNITY
    // ═══════════════════════════════════════════════════════════════════════════

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning) 
        {
            _initialBatchSpawned = false;
            return;
        }

        // Spawn inicial al comenzar la partida
        if (spawnInitialBatch && !_initialBatchSpawned)
        {
            _initialBatchSpawned = true;
            SpawnBatch(initialBatchSize);
        }

        // Spawn periódico
        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            _timer = 0f;
            TrySpawnTortilla();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  LÓGICA
    // ═══════════════════════════════════════════════════════════════════════════

    private void TrySpawnTortilla()
    {
        int currentCount = GameObject.FindGameObjectsWithTag(tortillaTag).Length;
        if (currentCount >= maxTortillas) return;

        SpawnOneTortilla();
    }

    private void SpawnBatch(int count)
    {
        for (int i = 0; i < count; i++)
        {
            int currentCount = GameObject.FindGameObjectsWithTag(tortillaTag).Length;
            if (currentCount >= maxTortillas) break;

            SpawnOneTortilla(i * 0.05f); // offset para que no se amontonen exactamente
        }
    }

    private void SpawnOneTortilla(float offsetY = 0f)
    {
        if (tortillaPrefab == null)
        {
            Debug.LogError("[TortillaSpawner] No se asignó el prefab de tortilla.");
            return;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        pos.y += offsetY;

        // Pequeña variación horizontal para que no se apilen exactamente
        pos.x += Random.Range(-0.05f, 0.05f);
        pos.z += Random.Range(-0.05f, 0.05f);

        Instantiate(tortillaPrefab, pos, Quaternion.identity);
        Debug.Log($"[TortillaSpawner] Tortilla generada en {pos}");
    }
}
