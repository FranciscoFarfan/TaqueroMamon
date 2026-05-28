using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Carrusel de instrucciones para el taco stand.
/// Asigna los campos desde el Inspector y arrastra los GameObjects
/// de cada instruccion al arreglo correspondiente.
/// </summary>
public class InstructionCarousel : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Clase interna que representa cada slide
    // ─────────────────────────────────────────────
    [System.Serializable]
    public class InstructionSlide
    {
        [Tooltip("Titulo que aparece en el TextMesh de titulo")]
        public string titulo;

        [Tooltip("Descripcion que aparece en el TextMesh de descripcion")]
        [TextArea(2, 6)]
        public string descripcion;

        [Tooltip("GameObjects que se activan SOLO durante esta instruccion (pueden ser 3D o UI)")]
        public GameObject[] gameObjects;
    }

    // ─────────────────────────────────────────────
    //  Referencias al Canvas
    // ─────────────────────────────────────────────
    [Header("Referencias UI")]
    [Tooltip("El GameObject raiz de toda la UI de instrucciones (ej. el panel de fondo Canvas Image). " +
             "Se activara/desactivara por completo para evitar que queden fondos visibles.")]
    public GameObject panelRoot;

    [Tooltip("TextMeshPro donde se muestra el titulo de la instruccion")]
    public TextMeshProUGUI tituloText;

    [Tooltip("TextMeshPro donde se muestra la descripcion de la instruccion")]
    public TextMeshProUGUI descripcionText;

    [Tooltip("Boton para avanzar al siguiente slide")]
    public Button botonSiguiente;

    [Tooltip("Boton para retroceder al slide anterior")]
    public Button botonAtras;

    [Tooltip("CanvasGroup que envuelve el contenido del panel (para el fade). " +
             "Agrega un CanvasGroup al GameObject padre de los textos.")]
    public CanvasGroup contentCanvasGroup;

    // ─────────────────────────────────────────────
    //  Slides (configuralos en el Inspector)
    // ─────────────────────────────────────────────
    [Header("Instrucciones")]
    [Tooltip("Lista de instrucciones en orden. Arrastra los GameObjects de cada paso.")]
    public InstructionSlide[] slides;

    // ─────────────────────────────────────────────
    //  Animacion
    // ─────────────────────────────────────────────
    [Header("Animacion")]
    [Tooltip("Duracion del fade al cambiar de slide (segundos)")]
    [Range(0.05f, 1f)]
    public float fadeDuration = 0.25f;

    // ─────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────
    private int currentIndex = 0;
    private bool isAnimating = false;

    private bool _initialized = false;

    // ─────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────
    private void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// Inicializa las referencias y eventos de los botones si no se ha hecho ya.
    /// </summary>
    private void Initialize()
    {
        if (_initialized) return;
        if (!ValidateReferences()) return;

        // Conectar botones
        botonSiguiente.onClick.AddListener(NextSlide);
        botonAtras.onClick.AddListener(PreviousSlide);

        _initialized = true;
    }

    // ─────────────────────────────────────────────
    //  API publica (UIManager + botones)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Muestra el carrusel y lo reinicia al primer slide.
    /// Llamar desde UIManager cuando MenuBG se activa.
    /// </summary>
    public void Show()
    {
        // Activar el panel raiz (o el GameObject de este script) por si estaba desactivado
        if (panelRoot != null)
            panelRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        Initialize();

        if (slides == null || slides.Length == 0) return;

        // Cancelar cualquier transicion en curso
        StopAllCoroutines();
        isAnimating = false;

        // Ocultar objetos de todos los slides y resetear al 0
        HideAllSlideObjects();
        currentIndex = 0;
        ApplySlide(0);
        UpdateButtonStates();

        // Activar los GameObjects del primer slide
        // (aunque esten desactivados por defecto en la escena)
        SetSlideObjects(0, true);

        // Hacer visible el panel
        if (contentCanvasGroup != null)
        {
            contentCanvasGroup.alpha          = 1f;
            contentCanvasGroup.interactable   = true;
            contentCanvasGroup.blocksRaycasts = true;
        }
    }

    /// <summary>
    /// Oculta el carrusel y desactiva todos los GameObjects de slides.
    /// Llamar desde UIManager cuando se sale del menu principal.
    /// </summary>
    public void Hide()
    {
        StopAllCoroutines();
        isAnimating = false;

        HideAllSlideObjects();

        if (contentCanvasGroup != null)
        {
            contentCanvasGroup.alpha          = 0f;
            contentCanvasGroup.interactable   = false;
            contentCanvasGroup.blocksRaycasts = false;
        }

        // Desactivar el panel raiz para ocultar la imagen de fondo y demas componentes
        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void NextSlide()
    {
        if (isAnimating || currentIndex >= slides.Length - 1) return;
        StartCoroutine(TransitionTo(currentIndex + 1));
    }

    public void PreviousSlide()
    {
        if (isAnimating || currentIndex <= 0) return;
        StartCoroutine(TransitionTo(currentIndex - 1));
    }

    // ─────────────────────────────────────────────
    //  Transicion con fade
    // ─────────────────────────────────────────────
    private IEnumerator TransitionTo(int newIndex)
    {
        isAnimating = true;

        // --- FADE OUT ---
        yield return StartCoroutine(FadeTo(0f));

        // Desactivar GameObjects del slide actual
        SetSlideObjects(currentIndex, false);

        // Cambiar datos
        currentIndex = newIndex;
        ApplySlide(currentIndex);
        UpdateButtonStates();

        // Activar GameObjects del nuevo slide
        SetSlideObjects(currentIndex, true);

        // --- FADE IN ---
        yield return StartCoroutine(FadeTo(1f));

        isAnimating = false;
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    /// <summary>Aplica los textos del slide indicado al UI.</summary>
    private void ApplySlide(int index)
    {
        if (index < 0 || index >= slides.Length) return;

        tituloText.text  = slides[index].titulo;
        descripcionText.text = slides[index].descripcion;
    }

    /// <summary>Activa o desactiva los GameObjects del slide indicado.</summary>
    private void SetSlideObjects(int index, bool active)
    {
        if (index < 0 || index >= slides.Length) return;
        if (slides[index].gameObjects == null) return;

        foreach (var go in slides[index].gameObjects)
        {
            if (go != null) go.SetActive(active);
        }
    }

    /// <summary>Al inicio, desactiva todos los GameObjects de todos los slides.</summary>
    private void HideAllSlideObjects()
    {
        foreach (var slide in slides)
        {
            if (slide.gameObjects == null) continue;
            foreach (var go in slide.gameObjects)
            {
                if (go != null) go.SetActive(false);
            }
        }
    }

    /// <summary>Activa/desactiva los botones segun el slide actual.</summary>
    private void UpdateButtonStates()
    {
        botonAtras.interactable    = currentIndex > 0;
        botonSiguiente.interactable = currentIndex < slides.Length - 1;
    }

    /// <summary>Interpola el alpha del CanvasGroup durante 'fadeDuration' segundos.</summary>
    private IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = contentCanvasGroup.alpha;
        float elapsed    = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            contentCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        contentCanvasGroup.alpha = targetAlpha;
    }

    /// <summary>Valida que los campos obligatorios esten asignados.</summary>
    private bool ValidateReferences()
    {
        bool ok = true;

        if (tituloText == null)
        {
            Debug.LogError("[InstructionCarousel] Falta asignar 'tituloText'.", this);
            ok = false;
        }
        if (descripcionText == null)
        {
            Debug.LogError("[InstructionCarousel] Falta asignar 'descripcionText'.", this);
            ok = false;
        }
        if (botonSiguiente == null)
        {
            Debug.LogError("[InstructionCarousel] Falta asignar 'botonSiguiente'.", this);
            ok = false;
        }
        if (botonAtras == null)
        {
            Debug.LogError("[InstructionCarousel] Falta asignar 'botonAtras'.", this);
            ok = false;
        }
        if (contentCanvasGroup == null)
        {
            Debug.LogError("[InstructionCarousel] Falta asignar 'contentCanvasGroup'. " +
                           "Agrega un componente CanvasGroup al panel de contenido.", this);
            ok = false;
        }
        if (slides == null || slides.Length == 0)
        {
            Debug.LogError("[InstructionCarousel] El arreglo 'slides' esta vacio.", this);
            ok = false;
        }

        return ok;
    }
}
