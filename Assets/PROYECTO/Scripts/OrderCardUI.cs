using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// OrderCardUI — Componente para cada tarjeta de pedido en el tendedero.
/// Muestra la información de un TacoOrder (tipo de carne, cantidad, recompensa).
///
/// Se coloca en cada slot de tarjeta del tendedero (3 en total).
/// Puede ser un Canvas WorldSpace o parte de un Canvas de HUD.
/// </summary>
public class OrderCardUI : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Textos")]
    [Tooltip("Texto que muestra el tipo de carne.")]
    [SerializeField] private TMP_Text meatTypeText;

    [Tooltip("Texto que muestra la cantidad de tacos.")]
    [SerializeField] private TMP_Text tacoCountText;

    [Tooltip("Texto que muestra la recompensa en pesos.")]
    [SerializeField] private TMP_Text rewardText;

    [Header("Visual")]
    [Tooltip("Fondo de la tarjeta (para cambiar color según estado).")]
    [SerializeField] private Image cardBackground;

    [Tooltip("Color normal de la tarjeta.")]
    [SerializeField] private Color normalColor = new Color(1f, 0.95f, 0.8f, 1f);

    [Tooltip("Color cuando el pedido fue completado (flash verde).")]
    [SerializeField] private Color completedColor = new Color(0.5f, 1f, 0.5f, 1f);

    [Tooltip("Color cuando no hay pedido (vacío / gris).")]
    [SerializeField] private Color emptyColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);

    [Header("Íconos de carne (Opcional)")]
    [Tooltip("Imagen para mostrar un ícono de la carne.")]
    [SerializeField] private Image meatIcon;

    [Tooltip("Sprites de cada tipo de carne. El nombre debe coincidir con MeatType.")]
    [SerializeField] private MeatIconEntry[] meatIcons;

    // ═══════════════════════════════════════════════════════════════════════════
    //  STRUCTS
    // ═══════════════════════════════════════════════════════════════════════════

    [System.Serializable]
    public struct MeatIconEntry
    {
        public string meatType;
        public Sprite icon;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ESTADO PRIVADO
    // ═══════════════════════════════════════════════════════════════════════════

    private TacoOrder _currentOrder = null;
    private bool _isAnimating = false;

    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Muestra un pedido en la tarjeta.
    /// </summary>
    public void SetOrder(TacoOrder order)
    {
        _currentOrder = order;

        if (order == null)
        {
            ClearOrder();
            return;
        }

        if (meatTypeText != null)
            meatTypeText.text = order.MeatType;

        if (tacoCountText != null)
            tacoCountText.text = $"x{order.TacoCount}";

        if (rewardText != null)
            rewardText.text = $"${order.Reward}";

        if (cardBackground != null)
            cardBackground.color = normalColor;

        // Buscar ícono de carne
        if (meatIcon != null && meatIcons != null)
        {
            Sprite icon = FindMeatIcon(order.MeatType);
            if (icon != null)
            {
                meatIcon.sprite = icon;
                meatIcon.enabled = true;
            }
            else
            {
                meatIcon.enabled = false;
            }
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// Limpia la tarjeta (sin pedido).
    /// </summary>
    public void ClearOrder()
    {
        _currentOrder = null;

        if (meatTypeText != null)
            meatTypeText.text = "---";

        if (tacoCountText != null)
            tacoCountText.text = "";

        if (rewardText != null)
            rewardText.text = "";

        if (cardBackground != null)
            cardBackground.color = emptyColor;

        if (meatIcon != null)
            meatIcon.enabled = false;
    }

    /// <summary>
    /// Hace un flash verde para indicar que el pedido fue completado.
    /// </summary>
    public void AnimateComplete()
    {
        if (_isAnimating || cardBackground == null) return;
        _isAnimating = true;

        cardBackground.color = completedColor;
        Invoke(nameof(ResetAnimation), 0.5f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  PRIVADO
    // ═══════════════════════════════════════════════════════════════════════════

    private void ResetAnimation()
    {
        _isAnimating = false;
        if (cardBackground != null)
            cardBackground.color = normalColor;
    }

    private Sprite FindMeatIcon(string meatType)
    {
        if (meatIcons == null) return null;

        foreach (MeatIconEntry entry in meatIcons)
        {
            if (entry.meatType == meatType)
                return entry.icon;
        }
        return null;
    }
}
