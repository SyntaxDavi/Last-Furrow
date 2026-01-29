using UnityEngine;
using DG.Tweening;

/// <summary>
/// Sistema Central de Áudio (Single Source of Truth).
/// Gerencia transições, fades, pausa e persistência de clipes.
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _sfxSource;

    [Header("Mixing")]
    public UnityEngine.Audio.AudioMixerGroup MusicGroup;
    public UnityEngine.Audio.AudioMixerGroup SFXGroup;

    [Header("Settings")]
    [Range(0f, 1f)] public float MasterVolume = 1f;
    [SerializeField] private float _defaultFadeDuration = 1.0f;

    private Tween _fadeTween;
    private AudioClip _targetMusicClip;

    public void Initialize()
    {
        if (_musicSource == null) _musicSource = gameObject.AddComponent<AudioSource>();
        if (_sfxSource == null) _sfxSource = gameObject.AddComponent<AudioSource>();

        // Routing
        if (MusicGroup != null) _musicSource.outputAudioMixerGroup = MusicGroup;
        if (SFXGroup != null) _sfxSource.outputAudioMixerGroup = SFXGroup;

        _musicSource.loop = true;

        // Auto-subscribe para gerenciar pausa global sem depender de controllers externos
        if (AppCore.Instance != null && AppCore.Instance.Events?.GameState != null)
        {
            AppCore.Instance.Events.GameState.OnStateChanged += HandleGameStateChanged;
        }
    }

    private void OnDestroy()
    {
        if (AppCore.Instance != null && AppCore.Instance.Events?.GameState != null)
        {
            AppCore.Instance.Events.GameState.OnStateChanged -= HandleGameStateChanged;
        }
    }

    // --- API PÚBLICA (SSOT) ---

    /// <summary>
    /// Define qual música deve estar ativa. O sistema cuida de Fades e Restart.
    /// </summary>
    public void SetMusicContext(AudioClip clip, bool shouldBePlaying, bool forceRestart = false)
    {
        _targetMusicClip = shouldBePlaying ? clip : null;

        // Se não deve tocar, faz fade-out (se estiver tocando)
        if (!shouldBePlaying || clip == null)
        {
            StopMusicWithFade();
            return;
        }

        // Se deve tocar
        PlayMusicWithFade(clip, forceRestart);
    }

    // --- LÓGICA INTERNA DE ESTADO ---

    private void HandleGameStateChanged(GameState newState)
    {
        // Gerenciamento automático de pausa técnica
        if (newState == GameState.Paused)
        {
            if (_musicSource.isPlaying) _musicSource.Pause();
        }
        else if (newState == GameState.Playing)
        {
            // Só dá Resume se o que está no Source é o que realmente queremos tocar (Policy)
            if (_targetMusicClip != null && _musicSource.clip == _targetMusicClip && !_musicSource.isPlaying)
            {
                _musicSource.UnPause();
                // Emergência: se UnPause não bastar (clipe não estava nem parado direito)
                if (!_musicSource.isPlaying) _musicSource.Play();
            }
        }
    }

    private void PlayMusicWithFade(AudioClip clip, bool forceRestart)
    {
        // Se já está tocando exatamente isso, ignora (Idempotência)
        if (!forceRestart && _musicSource.clip == clip && _musicSource.isPlaying) return;

        _fadeTween?.Kill();

        if (_musicSource.clip != clip || forceRestart)
        {
            // Troca de clipe ou reinício forçado
            if (forceRestart) _musicSource.Stop();
            
            _musicSource.clip = clip;
            _musicSource.volume = 0f;
            _musicSource.Play();
        }

        _fadeTween = _musicSource.DOFade(MasterVolume, _defaultFadeDuration).SetUpdate(true);
    }

    private void StopMusicWithFade()
    {
        _fadeTween?.Kill();
        
        if (_musicSource.isPlaying)
        {
            _fadeTween = _musicSource.DOFade(0f, _defaultFadeDuration)
                .SetUpdate(true)
                .OnComplete(() => {
                    _musicSource.Stop();
                    _musicSource.clip = null; // Limpa para garantir clean state
                });
        }
        else
        {
            _musicSource.Stop();
            _musicSource.clip = null;
        }
    }

    // --- SFX ---

    public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitchVariance = 0f)
    {
        if (clip == null) return;

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

    // --- LEGACY COMPATIBILITY (Wrappers for old API if needed) ---

    public AudioClip CurrentClip => _musicSource.clip;
    public bool IsMusicPlaying => _musicSource.isPlaying;
    
    // Antigos métodos agora chamam a nova lógica ou são internos
    [System.Obsolete("Use SetMusicContext instead")]
    public void PlayMusic(AudioClip clip, bool fade = false, bool forceRestart = false) => SetMusicContext(clip, true, forceRestart);
    
    [System.Obsolete("Internal handling now, logic moved to SetMusicContext")]
    public void StopMusic(bool fade = false) => SetMusicContext(null, false);
    
    [System.Obsolete("Handled automatically via GameState now")]
    public void PauseMusic() { if (_musicSource.isPlaying) _musicSource.Pause(); }
    
    [System.Obsolete("Handled automatically via GameState now")]
    public void ResumeMusic() { if (!_musicSource.isPlaying && _musicSource.clip != null) _musicSource.UnPause(); }
}