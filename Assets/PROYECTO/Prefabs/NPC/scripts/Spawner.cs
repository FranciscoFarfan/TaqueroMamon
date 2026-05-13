using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawner — Genera peatones (walkers) que caminan por la calle.
/// Solo spawnea mientras el juego está en curso y los destruye al terminar.
/// </summary>
public class Spawner : MonoBehaviour
{
    public GameObject[] peoplePrefabs; // Arrastra aquí tus prefabs de personajes

    public float minTime = 1f;   // Tiempo mínimo entre spawns
    public float maxTime = 4f;   // Tiempo máximo entre spawns

    public Transform spawnPoint; // Desde dónde aparecen (opcional)

    private float timer;
    private float nextSpawnTime;
    private bool _subscribedToGameOver = false;

    /// <summary>Lista de walkers activos para poder destruirlos al terminar.</summary>
    private readonly List<GameObject> _activeWalkers = new List<GameObject>();

    void Start()
    {
        SetNextSpawnTime();
    }

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

        if (timer >= nextSpawnTime)
        {
            SpawnPerson();
            timer = 0f;
            SetNextSpawnTime();
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
    /// Al terminar la partida, destruye todos los walkers activos.
    /// </summary>
    private void OnGameOver(int finalScore)
    {
        // Limpiar referencias nulas (ya destruidos por otras razones)
        _activeWalkers.RemoveAll(w => w == null);

        foreach (GameObject walker in _activeWalkers)
        {
            if (walker != null)
                Destroy(walker);
        }

        _activeWalkers.Clear();
        timer = 0f;
        Debug.Log("[Spawner] Game Over — Todos los walkers eliminados.");
    }

    void SpawnPerson()
    {
        if (peoplePrefabs.Length == 0) return;

        // Elige un prefab random del array
        int randomIndex = Random.Range(0, peoplePrefabs.Length);
        GameObject prefab = peoplePrefabs[randomIndex];

        // Posición de spawn: usa spawnPoint si existe, si no usa la posición del objeto
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;

        GameObject walker = Instantiate(prefab, pos, Quaternion.Euler(0, -90, 0));
        _activeWalkers.Add(walker);
    }

    void SetNextSpawnTime()
    {
        nextSpawnTime = Random.Range(minTime, maxTime);
    }
}
