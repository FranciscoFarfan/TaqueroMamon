using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Script de "Hacks" para desarrollo. 
/// Spawnea objetos en las manos usando Alt + Teclado Numérico.
/// </summary>
public class Hacks : MonoBehaviour
{
    [Header("Referencias de Manos")]
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Header("Objetos Mano IZQUIERDA (Alt + 1 al 5)")]
    [SerializeField] private List<GameObject> leftPrefabs;

    [Header("Objetos Mano DERECHA (Alt + 6 al 0)")]
    [SerializeField] private List<GameObject> rightPrefabs;

    void Update()
    {
        if (Keyboard.current == null) return;

        // Solo funciona si se mantiene presionado ALT (Cualquiera)
        bool altPressed = Keyboard.current.shiftKey.isPressed;
        if (!altPressed) return;

        // --- MANO IZQUIERDA (1 al 5) ---
        CheckSpawn(Keyboard.current.digit1Key, 0, leftHand, leftPrefabs);
        CheckSpawn(Keyboard.current.digit2Key, 1, leftHand, leftPrefabs);
        CheckSpawn(Keyboard.current.digit3Key, 2, leftHand, leftPrefabs);
        CheckSpawn(Keyboard.current.digit4Key, 3, leftHand, leftPrefabs);
        CheckSpawn(Keyboard.current.digit5Key, 4, leftHand, leftPrefabs);

        // --- MANO DERECHA (6 al 0) ---
        CheckSpawn(Keyboard.current.digit6Key, 0, rightHand, rightPrefabs);
        CheckSpawn(Keyboard.current.digit7Key, 1, rightHand, rightPrefabs);
        CheckSpawn(Keyboard.current.digit8Key, 2, rightHand, rightPrefabs);
        CheckSpawn(Keyboard.current.digit9Key, 3, rightHand, rightPrefabs);
        CheckSpawn(Keyboard.current.digit0Key, 4, rightHand, rightPrefabs);
    }

    private void CheckSpawn(UnityEngine.InputSystem.Controls.KeyControl key, int index, Transform hand, List<GameObject> list)
    {
        if (key.wasPressedThisFrame && hand != null && list != null && index < list.Count)
        {
            if (list[index] != null)
            {
                SpawnObject(list[index], hand);
            }
        }
    }

    private void SpawnObject(GameObject prefab, Transform hand)
    {
        GameObject spawned = Instantiate(prefab, hand.position, hand.rotation);
        
        Rigidbody rb = spawned.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log($"[Hacks] Spawneado {prefab.name} en {hand.name}");
    }
}

