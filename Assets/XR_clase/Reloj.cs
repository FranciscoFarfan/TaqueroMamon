using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Reloj : MonoBehaviour
{
    public Transform hora;
    public Transform minuto;
    public Transform segundo;

    void Update()
    {
        DateTime tiempo = DateTime.Now;

        float segundos = tiempo.Second;
        float minutos = tiempo.Minute;
        float horas = tiempo.Hour % 12;

        // Rotaciones
        float rotSeg = segundos * 6f;
        float rotMin = minutos * 6f;
        float rotHora = (horas * 30f) + (minutos * 0.5f);

        segundo.localRotation = Quaternion.Euler(0, rotSeg, 0);
        minuto.localRotation  = Quaternion.Euler(0, rotMin, 0);
        hora.localRotation    = Quaternion.Euler(0, rotHora, 0);
    }
}