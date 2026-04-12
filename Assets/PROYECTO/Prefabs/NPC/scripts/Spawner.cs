using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject[] peoplePrefabs; // Arrastra aquí tus prefabs de personajes

    public float minTime = 1f;   // Tiempo mínimo entre spawns
    public float maxTime = 4f;   // Tiempo máximo entre spawns

    public Transform spawnPoint; // Desde dónde aparecen (opcional)

    private float timer;
    private float nextSpawnTime;
    void Start()
    {
        SetNextSpawnTime();
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= nextSpawnTime)
        {
            SpawnPerson();
            timer = 0f;
            SetNextSpawnTime();
        }
    }
    void SpawnPerson()
    {
        if (peoplePrefabs.Length == 0) return;

        // Elige un prefab random del array
        int randomIndex = Random.Range(0, peoplePrefabs.Length);
        GameObject prefab = peoplePrefabs[randomIndex];

        // Posición de spawn: usa spawnPoint si existe, si no usa la posición del objeto
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;

        Instantiate(prefab, pos, Quaternion.Euler(0, -90, 0));
    }

    void SetNextSpawnTime()
    {
        nextSpawnTime = Random.Range(minTime, maxTime);
    }
}
