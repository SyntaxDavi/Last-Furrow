using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _sfxSource;

    [Header("Settings")]
    [Range(0f, 1f)] public float MasterVolume = 1f;

    public void Initialize()
    {
        // Garante que as fontes existam se não forem atribuídas no Inspector
        if (_musicSource == null) _musicSource = gameObject.AddComponent<AudioSource>();
        if (_sfxSource == null) _sfxSource = gameObject.AddComponent<AudioSource>();

        _musicSource.loop = true;
    }

    public void PlayMusic(AudioClip clip, bool fade = false)
    {
        if (clip == null) return;

        // Lógica simples: se já está tocando o mesmo, não reinicia
        if (_musicSource.clip == clip && _musicSource.isPlaying) return;

        _musicSource.clip = clip;
        _musicSource.volume = MasterVolume; // Todo: Implementar volume separado
        _musicSource.Play();
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitchVariance = 0f)
    {
        if (clip == null) return;

        // Variação de pitch para evitar som repetitivo ("Machine Gun Effect")
        if (pitchVariance > 0)
        {
            _sfxSource.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        }
        else
        {
            _sfxSource.pitch = 1f;
        }

        _sfxSource.PlayOneShot(clip, MasterVolume * volumeScale);
    }

    public void StopMusic()
    {
        _musicSource.Stop();
    }
}