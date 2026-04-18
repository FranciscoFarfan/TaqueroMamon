using UnityEngine;

/// <summary>
/// TacoData — Componente simple que identifica el tipo de carne de un taco.
/// Se coloca en los prefabs de taco listo (TacoPastor, TacoBistec, TacoQueso, TacoListo).
/// </summary>
public class TacoData : MonoBehaviour
{
    [Header("Tipo de carne")]
    [Tooltip("Debe coincidir con los strings de GameManager.availableMeats (Pastor, Bistec, Queso, etc.)")]
    public string meatType = "Pastor";
}
