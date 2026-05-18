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
            if (plate != null && plate.TacoCount > 0 && !plate.IsDelivered)
            {
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
        int minWaste = 9999;

        foreach (PersonInteraction customer in activeCustomers)
        {
            if (customer == null || customer.AssignedOrder == null || customer.HasReceivedPlate) continue;

            TacoOrder order = customer.AssignedOrder;
            
            int matchingTacos = plate.CountMatchingTacos(order.MeatType);
            
            // Requisito estricto: El plato debe tener al menos la cantidad de tacos de ESA carne que pide el cliente
            // Si el cliente pide 3 Bistec y solo llevas 2, no lo acepta.
            if (matchingTacos < order.TacoCount) continue;
            
            // Calculamos la merma: Cualquier taco en el plato que no sea parte de lo que pidió el cliente es merma
            // (Ya sean tacos extra de la misma carne o tacos de otra carne)
            int waste = plate.TacoCount - order.TacoCount;

            // Buscamos el match que deje la menor cantidad de desperdicio
            if (waste < minWaste)
            {
                minWaste = waste;
                bestMatch = customer;
            }
        }

        // Si encontramos un cliente que acepte el plato
        if (bestMatch != null)
        {
            // Forzar que el jugador suelte el plato (ahora sí se lo van a quitar)
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

            plate.IsDelivered = true;
            Debug.Log($"[DeliveryArea] Entregando plato al NPC '{bestMatch.gameObject.name}'. Merma total: {minWaste}");
            
            // Descontar pérdida por tacos extra o equivocados
            if (minWaste > 0 && GameManager.Instance != null)
            {
                GameManager.Instance.ApplyPenalty(minWaste * 10, $"Merma de tacos ({minWaste})");
            }
            
            bestMatch.DeliverPlate(plate);
        }
        else
        {
            // Nadie acepta el plato (no cumple cantidad mínima o tiene carne equivocada)
            // No se quita de la mano, no se cobra penalización y no se destruye.
            Debug.Log("[DeliveryArea] Plato rechazado (No cumple cantidad o carne de ningún pedido). El jugador lo conserva.");
        }
    }
}
