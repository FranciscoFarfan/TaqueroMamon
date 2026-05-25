using UnityEngine;

/// <summary>
/// Reproductor de Radio.
/// Reproduce canciones aleatorias y, cada X canciones, reproduce un anuncio comercial aleatorio.
/// </summary>
public class Radio : MonoBehaviour
{
    [Header("Música y Anuncios")]
    [Tooltip("Lista de canciones a reproducir.")]
    public AudioClip[] songs;

    [Tooltip("Lista de comerciales/anuncios.")]
    public AudioClip[] ads;

    [Header("Configuración")]
    [Tooltip("Cantidad de canciones a reproducir consecutivamente antes de meter un comercial.")]
    public int songsBeforeAd = 2;

    private AudioSource audioSource;
    private int currentSongIndex = -1;
    private float lastPlayTime = 0f;
    private int consecutiveSongsPlayed = 0;
    private bool isPlayingAd = false;

    [Header("Mezclador de Volumen")]
    [Tooltip("Volumen para las canciones (0 a 1)")]
    [Range(0f, 1f)]
    public float songVolume = 0.1f;

    [Tooltip("Volumen para los comerciales (0 a 1)")]
    [Range(0f, 1f)]
    public float adVolume = 0.6f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false; // Desactivamos el loop nativo para rotar audios libremente

        if (songs != null && songs.Length > 0)
        {
            PlayNextAudio();
        }
    }

    void Update()
    {
        // Cuando termina el audio (isPlaying es falso), reproducimos el siguiente.
        // Usamos lastPlayTime + 1 segundo como margen de seguridad.
        if (songs != null && songs.Length > 0 && !audioSource.isPlaying && Time.time > lastPlayTime + 1f)
        {
            PlayNextAudio();
        }
    }

    // Se deja público por si un botón en tu UI lo sigue llamando para saltar canción
    public void NextSong()
    {
        // Si el usuario fuerza "siguiente", detenemos el audio y vamos al siguiente flujo lógico
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        PlayNextAudio();
    }

    private void PlayNextAudio()
    {
        // Revisar si toca un comercial (y si hay anuncios disponibles)
        if (!isPlayingAd && consecutiveSongsPlayed >= songsBeforeAd && ads != null && ads.Length > 0)
        {
            PlayRandomAd();
        }
        else
        {
            PlayNextRandomSong();
        }
    }

    private void PlayRandomAd()
    {
        isPlayingAd = true;
        consecutiveSongsPlayed = 0; // Resetear el contador de canciones

        int randomAdIdx = Random.Range(0, ads.Length);
        audioSource.clip = ads[randomAdIdx];
        audioSource.Play();

        audioSource.volume = adVolume;
        lastPlayTime = Time.time;
        Debug.Log($"[Radio] Reproduciendo comercial...");
    }

    private void PlayNextRandomSong()
    {
        isPlayingAd = false;
        audioSource.volume = songVolume;
        int randomIdx = currentSongIndex;
        
        // Evitar repetir canción si hay más de una
        if (songs.Length > 1)
        {
            while (randomIdx == currentSongIndex)
            {
                randomIdx = Random.Range(0, songs.Length);
            }
        }
        else
        {
            randomIdx = 0;
        }

        currentSongIndex = randomIdx;
        
        audioSource.clip = songs[randomIdx];
        audioSource.Play();
        
        consecutiveSongsPlayed++;
        lastPlayTime = Time.time; 
        
        Debug.Log($"[Radio] Reproduciendo canción. (Canciones consecutivas: {consecutiveSongsPlayed})");
    }
}