using System;

/// <summary>
/// Representa un pedido de tacos hecho por un NPC.
/// No es MonoBehaviour, solo es un contenedor de datos.
/// </summary>
[Serializable]
public class TacoOrder
{
    // ─── Identificación ───────────────────────────────────────────────────────
    public int OrderId { get; private set; }

    // ─── Contenido del pedido ─────────────────────────────────────────────────
    /// <summary>Cantidad de tacos pedidos (1–5).</summary>
    public int TacoCount { get; private set; }

    /// <summary>
    /// Tipo de carne pedida. Usa el string que definas en tu sistema de carnes
    /// (ej. "Pastor", "Bistec", "Chorizo", etc.)
    /// </summary>
    public string MeatType { get; private set; }

    // ─── Economía ─────────────────────────────────────────────────────────────
    /// <summary>Puntos que paga el NPC si el pedido fue correcto.</summary>
    public int Reward { get; private set; }

    // ─── Estado ───────────────────────────────────────────────────────────────
    public bool IsCompleted { get; private set; }

    // ─── Constructor ──────────────────────────────────────────────────────────
    public TacoOrder(int id, int tacoCount, string meatType, int reward)
    {
        OrderId   = id;
        TacoCount = tacoCount;
        MeatType  = meatType;
        Reward    = reward;
        IsCompleted = false;
    }

    /// <summary>Marca el pedido como completado.</summary>
    public void Complete()
    {
        IsCompleted = true;
    }
}
