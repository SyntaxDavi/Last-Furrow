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

    private HandManager _handManager;

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
        }
    }

    private void OnDisable()
    {
        if (_handManager != null)
        {
            _handManager.OnCardVisuallySpawned -= PlayDrawSound;
            _handManager.OnCardVisuallySelected -= PlaySelectSound;
            _handManager.OnCardVisuallyHovered -= PlayHoverSound;
        }
    }

    private void PlayDrawSound(int sequenceIndex)
    {
        if (_config == null || _config.CardDrawSound == null) return;

        // Calcula pitch baseado na sequência (0, 1, 2...)
        float pitch = Mathf.Min(_basePitch + (sequenceIndex * _pitchStep), _maxPitch);

        // Toca SOMENTE o SFX com pitch especifico
        PlayLocalSound(_config.CardDrawSound, pitch);
    }

    private void PlaySelectSound()
    {
        if (_config == null || _config.CardSelectSound == null) return;
        PlayLocalSound(_config.CardSelectSound, 1.0f);
    }

    private void PlayHoverSound()
    {
        if (_config == null || _config.CardHoverSound == null) return;
        
        // Pitch aleatório bem sutil para o hover não ficar repetitivo
        float randomPitch = 1.0f + UnityEngine.Random.Range(-0.05f, 0.05f);
        PlayLocalSound(_config.CardHoverSound, randomPitch);
    }

    private AudioSource _localSource;
    private void PlayLocalSound(AudioClip clip, float pitch)
    {
        if (_localSource == null)
        {
            _localSource = gameObject.AddComponent<AudioSource>();
            if (_outputGroup != null)
            {
                _localSource.outputAudioMixerGroup = _outputGroup;
            }
        }

        _localSource.pitch = pitch;
        _localSource.PlayOneShot(clip, 1.0f); // Volume 1.0 (controlado pelo Mixer)
    }
}
