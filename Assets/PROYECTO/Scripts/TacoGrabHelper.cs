using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// TacoGrabHelper — Componente temporal que se agrega al taco recién instanciado.
/// Su única función es ejecutar una coroutine que transfiere el grab del interactor
/// al taco nuevo (ya que la tortilla original se destruye y no puede ejecutar coroutines).
///
/// Se auto-destruye después de completar la transferencia.
/// </summary>
public class TacoGrabHelper : MonoBehaviour
{
    /// <summary>
    /// Inicia la transferencia del grab al taco.
    /// Espera un frame para que XRI procese la deselección anterior.
    /// </summary>
    public void StartGrabTransfer(
        XRInteractionManager manager,
        IXRSelectInteractor interactor,
        XRGrabInteractable tacoGrab)
    {
        StartCoroutine(GrabAfterDelay(manager, interactor, tacoGrab));
    }

    private IEnumerator GrabAfterDelay(
        XRInteractionManager manager,
        IXRSelectInteractor interactor,
        XRGrabInteractable tacoGrab)
    {
        // Esperar dos frames para que XRI procese completamente la deselección
        yield return null;
        yield return null;

        if (manager != null && interactor != null && tacoGrab != null)
        {
            try
            {
                manager.SelectEnter(interactor, tacoGrab);
                Debug.Log("[TacoGrabHelper] Taco transferido a la mano del jugador.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TacoGrabHelper] No se pudo transferir el taco: {e.Message}");
            }
        }

        // Auto-destruir este componente helper (ya no se necesita)
        Destroy(this);
    }
}
