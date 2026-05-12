using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// TacoAssembler — Se coloca en el prefab de Tortilla.
/// Maneja la lógica de convertir una tortilla + carne en un taco listo.
///
/// Flujo:
///   1. La tortilla se cocina (TortillaManager → Cooked)
///   2. Se le asigna carne de dos formas:
///      a) Pastor: el trozo de carne (tag "Pastor") colisiona con la tortilla en la mano
///      b) Otras carnes: la tortilla toca un MeatPileSocket y el jugador presiona el trigger
///   3. Al tener carne, se convierte automáticamente en taco (cambio de prefab)
///
/// Controles:
///   - Grip: Agarrar la tortilla
///   - Trigger (mientras sostienes la tortilla): Servir carne del montón
///
/// Configuración:
///   1. Se coloca en el prefab Tortilla junto con TortillaManager.
///   2. Asignar los prefabs de taco en el Inspector.
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
    private XRGrabInteractable _grabInteractable;
    private TortillaManager _tortillaManager;
    private AudioSource _audioSource;

    /// <summary>Montón de carne sobre el que está la tortilla en este momento (null si ninguno).</summary>
    private MeatPileSocket _currentMeatPile = null;

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
        // Suscribirse al evento "activated" del XRGrabInteractable.
        // Este evento se dispara cuando el jugador presiona el TRIGGER
        // mientras sostiene el objeto con el GRIP.
        _grabInteractable.activated.AddListener(OnTriggerActivated);
    }

    void OnDisable()
    {
        _grabInteractable.activated.RemoveListener(OnTriggerActivated);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Asigna el tipo de carne. Llamado por MeatPileSocket o al recibir pastor.
    /// Solo funciona si la tortilla está cocida.
    /// Al recibir carne, la tortilla se convierte automáticamente en taco.
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

        Debug.Log($"[TacoAssembler] Carne asignada: '{_meatType}'. Convirtiendo en taco...");

        // Convertir automáticamente en taco al recibir carne
        ConvertToTaco();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  DETECCIÓN DE PASTOR (COLISIÓN)
    // ═══════════════════════════════════════════════════════════════════════════

    void OnTriggerEnter(Collider other)
    {
        // ── Pastor: trozo volando del trompo ────────────────────────────────
        if (other.CompareTag(pastorTag))
        {
            if (_hasMeat)
            {
                Debug.Log("[TacoAssembler] Ya tiene carne, ignorando trozo de pastor.");
                return;
            }

            if (_tortillaManager != null && !_tortillaManager.IsCooked)
            {
                Debug.Log("[TacoAssembler] Tortilla no cocida, el trozo de pastor se ignora.");
                return;
            }

            if (meatCatchSound != null && _audioSource != null)
                _audioSource.PlayOneShot(meatCatchSound);

            Destroy(other.gameObject);
            Debug.Log("[TacoAssembler] ¡Trozo de pastor atrapado por la tortilla!");

            // SetMeatType llamará a ConvertToTaco automáticamente
            SetMeatType("Pastor");
            return;
        }

        // ── Montón de carne: registrar para servirla con trigger ──────────────
        MeatPileSocket pile = other.GetComponentInParent<MeatPileSocket>();
        if (pile != null)
        {
            _currentMeatPile = pile;
            Debug.Log($"[TacoAssembler] Tortilla sobre montón '{other.transform.root.name}'. Presiona el trigger para servir.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Limpiar referencia al montón cuando la tortilla sale del trigger
        MeatPileSocket pile = other.GetComponentInParent<MeatPileSocket>();
        if (pile != null && pile == _currentMeatPile)
        {
            _currentMeatPile = null;
            Debug.Log("[TacoAssembler] Tortilla salió del montón de carne.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  TRIGGER — Servir carne desde montón
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Se llama cuando el jugador presiona el TRIGGER mientras sostiene la tortilla.
    /// Evento nativo de XRGrabInteractable.activated (Grip = sostener, Trigger = activar).
    /// </summary>
    private void OnTriggerActivated(ActivateEventArgs args)
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameRunning) return;

        // Si ya tiene carne, no hacer nada (ya se convirtió o se está convirtiendo)
        if (_hasMeat) return;

        // Intentar servir desde el montón bajo la tortilla
        if (_currentMeatPile != null)
        {
            // TryServeMeat verifica que esté cocida y llama SetMeatType → ConvertToTaco
            _currentMeatPile.TryServeMeat(this);
        }
        else
        {
            Debug.Log("[TacoAssembler] Trigger presionado, pero no hay montón de carne bajo la tortilla.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  CONVERSIÓN TORTILLA → TACO
    // ═══════════════════════════════════════════════════════════════════════════

    private void ConvertToTaco()
    {
        // Elegir el prefab correcto según el tipo de carne
        GameObject tacoPrefab = GetTacoPrefab(_meatType);

        if (tacoPrefab == null)
        {
            Debug.LogError($"[TacoAssembler] No hay prefab de taco para '{_meatType}'.");
            return;
        }

        // Guardar posición y rotación
        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        // Obtener la referencia al interactor que nos tiene agarrados
        IXRSelectInteractor currentInteractor = null;
        XRInteractionManager interactionManager = null;

        if (_grabInteractable.isSelected)
        {
            currentInteractor = _grabInteractable.firstInteractorSelecting;
            interactionManager = _grabInteractable.interactionManager;
        }

        // Forzar deseleccionar la tortilla si está siendo sostenida
        if (currentInteractor != null && interactionManager != null)
        {
            interactionManager.SelectExit(currentInteractor, _grabInteractable);
        }

        // Instanciar el taco en la misma posición
        GameObject tacoObj = Instantiate(tacoPrefab, pos, rot);

        // Asignar el tipo de carne al TacoData
        TacoData tacoData = tacoObj.GetComponent<TacoData>();
        if (tacoData == null)
            tacoData = tacoObj.AddComponent<TacoData>();
        tacoData.meatType = _meatType;

        // Intentar que el interactor agarre el nuevo taco.
        // La coroutine se ejecuta en el TACO nuevo (no en la tortilla que se destruye).
        if (currentInteractor != null && interactionManager != null)
        {
            XRGrabInteractable tacoGrab = tacoObj.GetComponent<XRGrabInteractable>();
            if (tacoGrab != null)
            {
                TacoGrabHelper helper = tacoObj.AddComponent<TacoGrabHelper>();
                helper.StartGrabTransfer(interactionManager, currentInteractor, tacoGrab);
            }
        }

        // Feedback de audio (en la posición, porque la tortilla se destruye)
        if (assembleSound != null)
        {
            AudioSource.PlayClipAtPoint(assembleSound, pos);
        }

        Debug.Log($"[TacoAssembler] ¡Taco de {_meatType} armado!");

        // Destruir la tortilla
        Destroy(gameObject);
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
