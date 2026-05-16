using UnityEngine;

public class Radio : MonoBehaviour
{
    public AudioClip[] songs;

    private AudioSource audioSource;
    private int currentSongIndex = -1;
    private float lastPlayTime = 0f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false; // Desactivamos el loop nativo para poder rotar canciones

        if (songs.Length > 0)
        {
            PlayNextRandomSong();
        }
    }

    void Update()
    {
        // Cuando termina la canción (isPlaying es falso), reproducimos la siguiente.
        // Usamos lastPlayTime + 1 segundo como margen de seguridad por si Unity 
        // tarda unos fotogramas en procesar el audio y evitar que salte de golpe.
        if (songs.Length > 0 && !audioSource.isPlaying && Time.time > lastPlayTime + 1f)
        {
            PlayNextRandomSong();
        }
    }

    // Se deja vacío por si un botón en tu UI lo sigue llamando
    public void NextSong()
    {
    }

    private void PlayNextRandomSong()
    {
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
        
        // Registramos en qué momento le dimos "Play" para el margen de seguridad
        lastPlayTime = Time.time; 
    }
}