using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// DeliveryArea — Zona donde se entregan los platos terminados.
/// Cuando un plato entra en esta zona, se evalúa contra los pedidos de todos
/// los clientes en la fila y se le entrega al cliente cuyo pedido coincida más.
/// </summary>
public class DeliveryArea : MonoBehaviour
{
    [Tooltip("Tag del plato.")]
    [SerializeField] private string plateTag = "Plato";

    private QueueManager _queueManager;

    void Start()
    {
        _queueManager = FindObjectOfType<QueueManager>();
        if (_queueManager == null)
        {
            Debug.LogError("[DeliveryArea] No se encontró el QueueManager en la escena.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(plateTag))
        {
            PlateSocket plate = other.GetComponentInParent<PlateSocket>();
            if (plate != null && plate.TacoCount > 0)
            {
                // Si el jugador lo tiene agarrado, forzar que lo suelte
                XRGrabInteractable grab = plate.GetComponent<XRGrabInteractable>();
                if (grab != null && grab.isSelected)
                {
                    XRInteractionManager manager = grab.interactionManager;
                    IXRSelectInteractor interactor = grab.firstInteractorSelecting;
                    if (manager != null && interactor != null)
                    {
                        manager.SelectExit(interactor, grab);
                    }
                }
                
                EvaluateAndDeliverPlate(plate);
            }
        }
    }

    private void EvaluateAndDeliverPlate(PlateSocket plate)
    {
        if (_queueManager == null) return;

        List<PersonInteraction> activeCustomers = _queueManager.GetActiveCustomers();
        if (activeCustomers.Count == 0)
        {
            Debug.Log("[DeliveryArea] No hay clientes en la fila.");
            return;
        }

        PersonInteraction bestMatch = null;
        int highestScore = -9999;

        foreach (PersonInteraction customer in activeCustomers)
        {
            if (customer == null || customer.AssignedOrder == null || customer.HasReceivedPlate) continue;

            TacoOrder order = customer.AssignedOrder;
            
            // Calculamos un puntaje de coincidencia.
            // 1. Puntos por cada taco que coincide con el tipo de carne del pedido
            int matchingTacos = plate.CountMatchingTacos(order.MeatType);
            
            // 2. Penalización o bono por la cantidad de tacos
            int countDiff = Mathf.Abs(order.TacoCount - plate.TacoCount);
            
            // Fórmula: Prioriza quien tenga más coincidencias. Desempata quien haya pedido exactamente esa cantidad de tacos.
            int score = (matchingTacos * 10) - countDiff;

            if (score > highestScore)
            {
                highestScore = score;
                bestMatch = customer;
            }
        }

        // Entregar al mejor match, incluso si no coinciden (la lógica de fallo se maneja en PersonInteraction)
        // Opcionalmente, se podría validar si el score es mayor a cierto mínimo para evitar entregas sin sentido
        if (bestMatch != null)
        {
            Debug.Log($"[DeliveryArea] Entregando plato al NPC '{bestMatch.gameObject.name}' con puntaje {highestScore}");
            bestMatch.DeliverPlate(plate);
        }
        else
        {
            Debug.Log("[DeliveryArea] No hay clientes válidos para recibir el plato.");
        }
    }
}
