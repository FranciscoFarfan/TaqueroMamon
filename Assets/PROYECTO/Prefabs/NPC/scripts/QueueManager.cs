using UnityEngine;

public class QueueManager : MonoBehaviour
{
    public Transform[] queueSpots;      // 3 GameObjects vacíos = posiciones de la fila
    public GameObject[] occupants;      // quién ocupa cada lugar

    void Start()
    {
        occupants = new GameObject[queueSpots.Length];
    }

    // Devuelve el índice del primer lugar libre, -1 si no hay
    public int GetFreeSpotIndex()
    {
        for (int i = 0; i < occupants.Length; i++)
        {
            if (occupants[i] == null)
                return i;
        }
        return -1;
    }

    public void OccupySpot(int index, GameObject person)
    {
        occupants[index] = person;
    }

    public void FreeSpot(int index)
    {
        occupants[index] = null;
    }

    // Retorna todos los NPCs activos en la fila
    public System.Collections.Generic.List<PersonInteraction> GetActiveCustomers()
    {
        System.Collections.Generic.List<PersonInteraction> customers = new System.Collections.Generic.List<PersonInteraction>();
        foreach (GameObject occupant in occupants)
        {
            if (occupant != null)
            {
                PersonInteraction pi = occupant.GetComponent<PersonInteraction>();
                if (pi != null)
                {
                    customers.Add(pi);
                }
            }
        }
        return customers;
    }
}