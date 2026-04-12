using UnityEngine;

public class Radio : MonoBehaviour
{
    public AudioClip[] songs;
    private AudioSource audioSource;
    private int currentSong = 0;
    private bool isOn = true;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.loop = true;

        if (songs.Length > 0)
        {
            audioSource.clip = songs[0];
            audioSource.Play();
        }
    }

    public void NextSong()
    {
        if (songs.Length == 0) return;

        // Si está apagada, enciende con la primera canción
        if (!isOn)
        {
            isOn = true;
            currentSong = 0;
            audioSource.clip = songs[currentSong];
            audioSource.Play();
            return;
        }

        // Si es la última canción, apaga
        if (currentSong == songs.Length - 1)
        {
            isOn = false;
            audioSource.Stop();
            return;
        }

        // Avanza a la siguiente canción
        currentSong++;
        audioSource.clip = songs[currentSong];
        audioSource.Play();
    }
}