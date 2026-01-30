using UnityEngine;
using DG.Tweening;

/// <summary>
/// Adiciona "Juice" ao jogo através de Screen Shake baseado em eventos.
/// Responde a patterns completados com intensidade variável.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraShakeController : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Multiplicador global de força do shake")]
    [SerializeField] private float _shakeStrengthMultiplier = 0.3f;
    [Tooltip("Duração base do shake em segundos")]
    [SerializeField] private float _baseDuration = 0.2f;

    private Camera _cam;
    private GameCameraController _camController;
    private Tween _shakeTween;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        _camController = GetComponent<GameCameraController>();
    }

    private void Start()
    {
        // Espera AppCore estar pronto
        if (AppCore.Instance != null && AppCore.Instance.Events != null)
        {
            AppCore.Instance.Events.Pattern.OnPatternSlotCompleted += OnPatternCompleted;
        }
    }

    private void OnDestroy()
    {
        if (AppCore.Instance != null && AppCore.Instance.Events != null)
        {
            AppCore.Instance.Events.Pattern.OnPatternSlotCompleted -= OnPatternCompleted;
        }
        _shakeTween?.Kill();
    }

    private void OnPatternCompleted(PatternMatch match)
    {
        if (match == null) return;

        // Calcula força baseada no Tier/Score do pattern
        float strength = CalculateShakeStrength(match.BaseScore);
        
        // Se a força for muito pequena, ignora (evita tremedeira constante em tier 1 fraco)
        if (strength <= 0.05f) return;

        DoShake(strength);
    }

    private float CalculateShakeStrength(int score)
    {
        // Tier 1 (<15): Shake muito leve ou nulo
        if (score < 15) return 0.2f * _shakeStrengthMultiplier;

        // Tier 2 (15-34): Shake leve
        if (score < 35) return 0.3f * _shakeStrengthMultiplier;

        // Tier 3 (35-79): Shake médio
        if (score < 80) return 0.35f * _shakeStrengthMultiplier;

        // Tier 4 (80+): Shake forte (Lendário)
        return 0.5f * _shakeStrengthMultiplier;
    }

    private void DoShake(float strength)
    {
        if (_camController != null)
        {
            _camController.DoShake(_baseDuration, strength);
        }

        // Trigger manual para sincronizar anchors se necessário
        if (AppCore.Instance != null && AppCore.Instance.Events != null)
            AppCore.Instance.Events.Camera.TriggerCameraUpdated();
    }
    
    // Test Cheat (Chame via Inspector context menu)
    [ContextMenu("Test Weak Shake")]
    public void TestWeak() => DoShake(0.1f * _shakeStrengthMultiplier);

    [ContextMenu("Test Strong Shake")]
    public void TestStrong() => DoShake(0.8f * _shakeStrengthMultiplier);
}
