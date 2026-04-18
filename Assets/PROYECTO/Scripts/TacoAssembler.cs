using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

/// <summary>
/// TacoAssembler — Se coloca en el prefab de Tortilla.
/// Maneja la lógica de convertir una tortilla + carne en un taco listo.
///
/// Flujo:
///   1. La tortilla se cocina (TortillaManager → Cooked)
///   2. Se le asigna carne de dos formas:
///      a) Pastor: el trozo de carne (tag "Pastor") colisiona con la tortilla en la mano
///      b) Otras carnes: la tortilla toca un MeatPileSocket
///   3. El jugador presiona el botón secundario del control → se convierte en taco
///
/// Configuración:
///   1. Se coloca en el prefab Tortilla junto con TortillaManager.
///   2. Asignar los prefabs de taco en el Inspector.
///   3. Asignar la referencia al botón secundario del control XR.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class TacoAssembler : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR — Prefabs de taco
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Prefabs de taco")]
    [Tooltip("Prefab del taco de pastor.")]
    [SerializeField] private GameObject tacoPastorPrefab;

    [Tooltip("Prefab del taco de bistec.")]
    [SerializeField] private GameObject tacoBistecPrefab;

    [Tooltip("Prefab del taco de queso.")]
    [SerializeField] private GameObject tacoQuesoPrefab;

    [Tooltip("Prefab genérico de taco (fallback para carnes sin prefab específico).")]
    [SerializeField] private GameObject tacoGenericPrefab;

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR — Input
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Input XR")]
    [Tooltip("Referencia al InputAction del botón secundario (A o X según la mano).")]
    [SerializeField] private InputActionReference secondaryButtonAction;

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR — Tags
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Tags")]
    [Tooltip("Tag de los trozos de pastor que vuelan del trompo.")]
    [SerializeField] private string pastorTag = "Pastor";

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR — Audio
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Audio (opcional)")]
    [Tooltip("Sonido al convertir en taco.")]
    [SerializeField] private AudioClip assembleSound;

    [Tooltip("Sonido al recibir carne de pastor.")]
    [SerializeField] private AudioClip meatCatchSound;

    // ═══════════════════════════════════════════════════════════════════════════
    //  ESTADO PRIVADO
    // ═══════════════════════════════════════════════════════════════════════════

    private string _meatType = null;
    private bool _hasMeat = false;
    private bool _isInHand = false;
    private XRGrabInteractable _grabInteractable;
    private TortillaManager _tortillaManager;
    private AudioSource _audioSource;

    // ═══════════════════════════════════════════════════════════════════════════
    //  PROPIEDADES PÚBLICAS
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>¿Ya tiene carne asignada?</summary>
    public bool HasMeat => _hasMeat;

    /// <summary>Tipo de carne asignada (null si no tiene).</summary>
    public string MeatType => _meatType;

    // ═══════════════════════════════════════════════════════════════════════════
    //  UNITY
    // ═══════════════════════════════════════════════════════════════════════════

    void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _tortillaManager = GetComponent<TortillaManager>();

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.spatialBlend = 1f;
        _audioSource.playOnAwake = false;
    }

    void OnEnable()
    {
        // Suscribirse a los eventos de grab
        _grabInteractable.selectEntered.AddListener(OnGrabbed);
        _grabInteractable.selectExited.AddListener(OnReleased);

        // Suscribirse al botón secundario
        if (secondaryButtonAction != null && secondaryButtonAction.action != null)
        {
            secondaryButtonAction.action.Enable();
            secondaryButtonAction.action.performed += OnSecondaryButtonPressed;
        }
    }

    void OnDisable()
    {
        _grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        _grabInteractable.selectExited.RemoveListener(OnReleased);

        if (secondaryButtonAction != null && secondaryButtonAction.action != null)
        {
            secondaryButtonAction.action.performed -= OnSecondaryButtonPressed;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Asigna el tipo de carne. Llamado por MeatPileSocket o al recibir pastor.
    /// Solo funciona si la tortilla está cocida.
    /// </summary>
    public void SetMeatType(string type)
    {
        if (_hasMeat)
        {
            Debug.Log($"[TacoAssembler] Ya tiene carne '{_meatType}', ignorando '{type}'.");
            return;
        }

        // Verificar que la tortilla esté cocida
        if (_tortillaManager != null && !_tortillaManager.IsCooked)
        {
            Debug.Log($"[TacoAssembler] La tortilla no está cocida. No se puede agregar carne.");
            return;
        }

        _meatType = type;
        _hasMeat = true;

        Debug.Log($"[TacoAssembler] Carne asignada: '{_meatType}'. Presiona botón secundario para armar taco.");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  DETECCIÓN DE PASTOR (COLISIÓN)
    // ═══════════════════════════════════════════════════════════════════════════

    void OnTriggerEnter(Collider other)
    {
        // Detectar trozos de pastor que caen/vuelan
        if (!other.CompareTag(pastorTag)) return;

        if (_hasMeat)
        {
            Debug.Log("[TacoAssembler] Ya tiene carne, ignorando trozo de pastor.");
            return;
        }

        // Verificar que la tortilla esté cocida
        if (_tortillaManager != null && !_tortillaManager.IsCooked)
        {
            Debug.Log("[TacoAssembler] Tortilla no cocida, el trozo de pastor se ignora.");
            return;
        }

        // Asignar pastor como tipo de carne
        SetMeatType("Pastor");

        // Feedback de audio
        if (meatCatchSound != null && _audioSource != null)
            _audioSource.PlayOneShot(meatCatchSound);

        // Destruir el trozo de pastor (ya fue "atrapado" por la tortilla)
        Destroy(other.gameObject);

        Debug.Log("[TacoAssembler] ¡Trozo de pastor atrapado por la tortilla!");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  EVENTOS XR
    // ═══════════════════════════════════════════════════════════════════════════

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        _isInHand = true;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        _isInHand = false;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  CONVERSIÓN TORTILLA → TACO
    // ═══════════════════════════════════════════════════════════════════════════

    private void OnSecondaryButtonPressed(InputAction.CallbackContext context)
    {
        // Solo convertir si:
        // 1. Tiene carne asignada
        // 2. Está en la mano del jugador
        // 3. El juego está en curso
        if (!_hasMeat || !_isInHand) return;

        if (GameManager.Instance != null && !GameManager.Instance.IsGameRunning) return;

        ConvertToTaco();
    }

    private void ConvertToTaco()
    {
        // Elegir el prefab correcto según el tipo de carne
        GameObject tacoPrefab = GetTacoPrefab(_meatType);

        if (tacoPrefab == null)
        {
            Debug.LogError($"[TacoAssembler] No hay prefab de taco para '{_meatType}'.");
            return;
        }

        // Obtener la referencia al interactor que nos tiene agarrados
        IXRSelectInteractor currentInteractor = null;
        if (_grabInteractable.isSelected)
        {
            currentInteractor = _grabInteractable.firstInteractorSelecting;
        }

        // Guardar posición y rotación
        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        // Soltar la tortilla del interactor antes de destruirla
        if (currentInteractor != null)
        {
            XRInteractionManager interactionManager = _grabInteractable.interactionManager;

            // Forzar deseleccionar la tortilla
            interactionManager.SelectExit(currentInteractor, _grabInteractable);

            // Instanciar el taco en la misma posición
            GameObject tacoObj = Instantiate(tacoPrefab, pos, rot);

            // Asignar el tipo de carne al TacoData
            TacoData tacoData = tacoObj.GetComponent<TacoData>();
            if (tacoData == null)
                tacoData = tacoObj.AddComponent<TacoData>();
            tacoData.meatType = _meatType;

            // Intentar que el interactor agarre el nuevo taco
            XRGrabInteractable tacoGrab = tacoObj.GetComponent<XRGrabInteractable>();
            if (tacoGrab != null && currentInteractor != null)
            {
                // Pequeño delay para que XRI procese la deselección
                StartCoroutine(GrabTacoDelayed(interactionManager, currentInteractor, tacoGrab));
            }

            // Feedback de audio (en el taco nuevo, porque la tortilla se destruye)
            if (assembleSound != null)
            {
                AudioSource.PlayClipAtPoint(assembleSound, pos);
            }

            Debug.Log($"[TacoAssembler] ¡Taco de {_meatType} armado!");

            // Destruir la tortilla
            Destroy(gameObject);
        }
        else
        {
            // Si no hay interactor (raro), solo instanciar y destruir
            GameObject tacoObj = Instantiate(tacoPrefab, pos, rot);

            TacoData tacoData = tacoObj.GetComponent<TacoData>();
            if (tacoData == null)
                tacoData = tacoObj.AddComponent<TacoData>();
            tacoData.meatType = _meatType;

            if (assembleSound != null)
                AudioSource.PlayClipAtPoint(assembleSound, pos);

            Debug.Log($"[TacoAssembler] Taco de {_meatType} armado (sin interactor).");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Espera un frame y luego intenta que el interactor agarre el taco nuevo.
    /// </summary>
    private System.Collections.IEnumerator GrabTacoDelayed(
        XRInteractionManager manager,
        IXRSelectInteractor interactor,
        XRGrabInteractable tacoGrab)
    {
        // Esperar un frame para que XRI procese
        yield return null;

        if (manager != null && interactor != null && tacoGrab != null)
        {
            try
            {
                manager.SelectEnter(interactor, tacoGrab);
                Debug.Log("[TacoAssembler] Taco transferido a la mano del jugador.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TacoAssembler] No se pudo transferir el taco: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Devuelve el prefab de taco correspondiente al tipo de carne.
    /// </summary>
    private GameObject GetTacoPrefab(string type)
    {
        switch (type)
        {
            case "Pastor":   return tacoPastorPrefab  != null ? tacoPastorPrefab  : tacoGenericPrefab;
            case "Bistec":   return tacoBistecPrefab  != null ? tacoBistecPrefab  : tacoGenericPrefab;
            case "Queso":    return tacoQuesoPrefab   != null ? tacoQuesoPrefab   : tacoGenericPrefab;
            default:         return tacoGenericPrefab;
        }
    }
}
