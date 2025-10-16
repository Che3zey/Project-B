using UnityEngine;

public class MusicManager : MonoBehaviour
{
    private static MusicManager instance;
    private AudioSource audioSource;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // <-- keeps it across scenes
            audioSource = GetComponent<AudioSource>();
            if (audioSource != null && !audioSource.isPlaying)
                audioSource.Play();
        }
        else
        {
            Destroy(gameObject); // Prevent duplicates when reloading
        }
    }
}
