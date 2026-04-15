using UnityEngine;

public class PersonSpawner : MonoBehaviour
{
    public GameObject[] peoplePrefabs;
    public Transform spawnPoint;
    public QueueManager queueManager;
    public GameObject exitPoint;

    public float checkInterval = 2f; // cada cuánto revisa si hay lugar libre
    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            TrySpawnPerson();
        }
    }

    void TrySpawnPerson()
    {
        int freeSpot = queueManager.GetFreeSpotIndex();
        if (freeSpot == -1) return;

        int randomIndex = Random.Range(0, peoplePrefabs.Length);
        GameObject person = Instantiate(peoplePrefabs[randomIndex], spawnPoint.position, Quaternion.Euler(0, 90, 0));

        PersonController controller = person.GetComponent<PersonController>();
        controller.exitPoint = exitPoint;
        controller.SetDestination(queueManager.queueSpots[freeSpot], freeSpot, queueManager);

        // ← agrega esto
        PersonInteraction interaction = person.GetComponent<PersonInteraction>();
        interaction.SetSpotInfo(freeSpot, queueManager);

        queueManager.OccupySpot(freeSpot, person);
    }
}