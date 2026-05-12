using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Facilita la configuración de offsets de posición y rotación para el agarre VR (XRGrabInteractable).
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class GrabOffsetHelper : MonoBehaviour
{
    [Header("Configuración de Offset")]
    [Tooltip("Posición relativa del agarre.")]
    public Vector3 positionOffset;
    
    [Tooltip("Rotación relativa del agarre (Grados).")]
    public Vector3 rotationOffset;

    private XRGrabInteractable _interactable;
    private GameObject _attachPoint;

    void Awake()
    {
        _interactable = GetComponent<XRGrabInteractable>();
        SetupAttachPoint();
    }

    private void SetupAttachPoint()
    {
        // Creamos un hijo que servirá como AttachTransform
        _attachPoint = new GameObject("AttachPoint_Generated");
        _attachPoint.transform.SetParent(this.transform, false);

        // Aplicamos los valores del inspector
        _attachPoint.transform.localPosition = positionOffset;
        _attachPoint.transform.localRotation = Quaternion.Euler(rotationOffset);

        // Se lo asignamos al componente de XR
        _interactable.attachTransform = _attachPoint.transform;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualización en el Editor
        Gizmos.color = Color.cyan;
        Vector3 worldPos = transform.TransformPoint(positionOffset);
        Gizmos.DrawWireSphere(worldPos, 0.02f);
        
        Gizmos.color = Color.blue;
        Vector3 worldForward = transform.TransformDirection(Quaternion.Euler(rotationOffset) * Vector3.forward);
        Gizmos.DrawRay(worldPos, worldForward * 0.05f);
    }
}
