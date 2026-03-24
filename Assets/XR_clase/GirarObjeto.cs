using UnityEngine;

public class GirarObjeto : MonoBehaviour
{
    [Tooltip("Velocidad de giro en grados por segundo")]
    public float velocidadDeGiro = 100f;

    void Update()
    {
        // Gira el objeto sobre el eje Y
        transform.Rotate(0f, velocidadDeGiro * Time.deltaTime, 0f, Space.Self);
    }
}
