using UnityEngine;

public class ReiniciarCuchillo : MonoBehaviour
{
    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;
    private Rigidbody rb;

    void Start()
    {
        // El cuchillo memoriza exactamente dónde está puesto al darle Play
        posicionInicial = transform.position;
        rotacionInicial = transform.rotation;
        rb = GetComponent<Rigidbody>();
    }

    // Esta función teletransporta el cuchillo de regreso
    public void RegresarACasa()
    {
        transform.position = posicionInicial;
        transform.rotation = rotacionInicial;

        // Le quitamos la fuerza de caída para que no salga volando
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        Debug.Log("[Cuchillo] Regresé a mi lugar en la tabla.");
    }
}