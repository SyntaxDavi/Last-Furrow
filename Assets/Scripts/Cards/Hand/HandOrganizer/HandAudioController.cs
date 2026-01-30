using UnityEngine;

/// <summary>
/// Responsável por tocar sons da mão (Draw, Shuffle, etc) de forma desacoplada.
/// Ouve eventos do HandManager.
/// </summary>
[RequireComponent(typeof(HandManager))]
public class HandAudioController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private CardVisualConfig _config;
    [SerializeField] private UnityEngine.Audio.AudioMixerGroup _outputGroup;
    
    [Header("Pitch Variation")]
    [SerializeField] private float _basePitch = 1.0f;
    [SerializeField] private float _pitchStep = 0.05f;
    [SerializeField] private float _maxPitch = 1.5f;

    [Header("Hover Polish")]
    [SerializeField] private float _hoverSoundCooldown = 0.08f;
    [SerializeField] private float _hoverPitchVariance = 0.02f;
    [SerializeField] private float _limitSoundCooldown = 0.5f;
    [SerializeField] private float _reorderSoundCooldown = 0.2f;

    [Header("Volume Settings (Multipliers)")]
    [SerializeField, Range(0, 2)] private float _globalVolume = 1.0f;
    [SerializeField, Range(0, 2)] private float _drawVolume = 1.0f;
    [SerializeField, Range(0, 2)] private float _selectVolume = 1.0f;
    [SerializeField, Range(0, 2)] private float _hoverVolume = 1.0f;
    [SerializeField, Range(0, 2)] private float _limitVolume = 0.8f;
    [SerializeField, Range(0, 2)] private float _reorderVolume = 0.8f;
    [SerializeField, Range(0, 2)] private float _dragVolume = 1.0f;
    [SerializeField, Range(0, 2)] private float _cardOnGridVolume = 1.2f;

    private HandManager _handManager;
    private float _lastHoverSoundTime;
    private float _lastLimitSoundTime;
    private float _lastReorderSoundTime;

    private void Awake()        
    {
        _handManager = GetComponent<HandManager>();
    }

    private void OnEnable()
    {
        if (_handManager != null)
        {
            _handManager.OnCardVisuallySpawned += PlayDrawSound;
            _handManager.OnCardVisuallySelected += PlaySelectSound;
            _handManager.OnCardVisuallyHovered += PlayHoverSound;
            _handManager.OnHandFullyElevated += PlayElevatedSound;
            _handManager.OnHandFullyLowered += PlayLoweredSound;
            _handManager.OnCardVisuallyReordered += PlayReorderSound;
            _handManager.OnCardVisuallyDragged += PlayDragSound;
            _handManager.OnCardVisuallyPlayed += PlayOnGridSound;
        }
    }

    private void OnDisable()
    {
        if (_handManager != null)
        {
            _handManager.OnCardVisuallySpawned -= PlayDrawSound;
            _handManager.OnCardVisuallySelected -= PlaySelectSound;
            _handManager.OnCardVisuallyHovered -= PlayHoverSound;
            _handManager.OnHandFullyElevated -= PlayElevatedSound;
            _handManager.OnHandFullyLowered -= PlayLoweredSound;
            _handManager.OnCardVisuallyReordered -= PlayReorderSound;
            _handManager.OnCardVisuallyDragged -= PlayDragSound;
            _handManager.OnCardVisuallyPlayed -= PlayOnGridSound;
        }
    }

    private void PlayDrawSound(int sequenceIndex)
    {
        SoundEffect fx = GetRandomFX(_config?.CardDrawSounds);
        if (fx == null) return;

        // Calcula pitch baseado na sequência (0, 1, 2...)
        float pitch = Mathf.Min(_basePitch + (sequenceIndex * _pitchStep), _maxPitch);

        PlayLocalSound(fx.Clip, pitch, _drawVolume * fx.Volume);
    }

    private void PlaySelectSound()
    {
        SoundEffect fx = GetRandomFX(_config?.CardSelectSounds);
        if (fx == null) return;
        PlayLocalSound(fx.Clip, 1.0f, _selectVolume * fx.Volume);
    }

    private void PlayHoverSound()
    {
        SoundEffect fx = GetRandomFX(_config?.CardHoverSounds);
        if (fx == null) return;

        // Cooldown para evitar metralhadora de sons ao passar o mouse rápido
        if (Time.time - _lastHoverSoundTime < _hoverSoundCooldown) return;
        _lastHoverSoundTime = Time.time;
        
        // Pitch aleatório bem sutil
        float randomPitch = 1.0f + UnityEngine.Random.Range(-_hoverPitchVariance, _hoverPitchVariance);
        PlayLocalSound(fx.Clip, randomPitch, _hoverVolume * fx.Volume);
    }

    private void PlayElevatedSound()
    {
        SoundEffect fx = GetRandomFX(_config?.HandElevatedSounds);
        if (fx == null) return;
        
        if (Time.time - _lastLimitSoundTime < _limitSoundCooldown) return;
        _lastLimitSoundTime = Time.time;
        
        PlayLocalSound(fx.Clip, 1.0f, _limitVolume * fx.Volume);
    }

    private void PlayLoweredSound()
    {
        SoundEffect fx = GetRandomFX(_config?.HandLoweredSounds);
        if (fx == null) return;
        
        if (Time.time - _lastLimitSoundTime < _limitSoundCooldown) return;
        _lastLimitSoundTime = Time.time;
        
        PlayLocalSound(fx.Clip, 1.0f, _limitVolume * fx.Volume);
    }

    private void PlayReorderSound()
    {
        // Se houver apenas uma carta na mão, usamos o som especial
        bool isOneCard = _handManager != null && _handManager.GetActiveCards().Count == 1;
        SoundEffect fx = isOneCard 
            ? GetRandomFX(_config?.OneCardReorderSounds) 
            : GetRandomFX(_config?.CardReorderSounds);

        if (fx == null) return;
        
        // Cooldown para reorder é importante pois layout dirty pode disparar muito
        if (Time.time - _lastReorderSoundTime < _reorderSoundCooldown) return;
        _lastReorderSoundTime = Time.time;

        float randomPitch = 1.0f + UnityEngine.Random.Range(-0.05f, 0.05f);
        PlayLocalSound(fx.Clip, randomPitch, _reorderVolume * fx.Volume);
    }

    private void PlayDragSound()
    {
        SoundEffect fx = GetRandomFX(_config?.CardDragSounds);
        if (fx == null) return;
        PlayLocalSound(fx.Clip, 1.0f, _dragVolume * fx.Volume);
    }

    private void PlayOnGridSound()
    {
        SoundEffect fx = GetRandomFX(_config?.CardOnGridSounds);
        if (fx == null) return;
        PlayLocalSound(fx.Clip, 1.0f, _cardOnGridVolume * fx.Volume);
    }

    private SoundEffect GetRandomFX(SoundEffect[] effects)
    {
        if (effects == null || effects.Length == 0) return null;
        if (effects.Length == 1) return effects[0];
        return effects[UnityEngine.Random.Range(0, effects.Length)];
    }

    private AudioSource _localSource;
    private void PlayLocalSound(AudioClip clip, float pitch, float volumeMultiplier = 1.0f)
    {
        if (clip == null) return;
        if (_localSource == null)
        {
            _localSource = gameObject.AddComponent<AudioSource>();
            if (_outputGroup != null)
            {
                _localSource.outputAudioMixerGroup = _outputGroup;
            }
        }

        _localSource.pitch = pitch;
        // Volume final = Volume Global * Multiplicador do Tipo (Slider) * Volume do Clip Individual
        float finalVolume = _globalVolume * volumeMultiplier;
        _localSource.PlayOneShot(clip, finalVolume); 
    }
}
