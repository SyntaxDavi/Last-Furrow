using UnityEngine;
using DG.Tweening;

namespace LastFurrow.Visual.Camera
{
    /// <summary>
    /// Adiciona "Juice" ao jogo através de Screen Shake baseado em eventos.
    /// Responde a patterns completados com intensidade variável.
    /// Implementa ICameraEffect para integrar-se ao stack de processamento.
    /// </summary>
    [RequireComponent(typeof(GameCameraController))]
    public class CameraShakeController : MonoBehaviour, ICameraEffect
    {
        [Header("Configuration")]
        [Tooltip("Multiplicador global de força do shake")]
        [SerializeField] private float _shakeStrengthMultiplier = 0.3f;
        [Tooltip("Duração base do shake em segundos")]
        [SerializeField] private float _baseDuration = 0.2f;

        private GameCameraController _camController;
        private Tween _shakeTween;
        private Vector3 _shakeOffset;
        private bool _isShaking;

        // --- ICameraEffect Implementation ---
        public int Priority => 100; // Alta prioridade (processado por último)
        public bool IsActive => _isShaking;

        public Vector3 GetPositionOffset() => _shakeOffset;
        public Vector2 GetTiltOffset() => Vector2.zero;
        public float GetRotationOffset() => 0f;
        public float GetSizeOffset() => 0f;

        private void Awake()
        {
            _camController = GetComponent<GameCameraController>();
        }

        private void Start()
        {
            if (AppCore.Instance != null && AppCore.Instance.Events != null)
            {
                AppCore.Instance.Events.Pattern.OnPatternSlotCompleted += OnPatternCompleted;
            }
            
            _camController.RegisterEffect(this);
        }

        private void OnDestroy()
        {
            if (AppCore.Instance != null && AppCore.Instance.Events != null)
            {
                AppCore.Instance.Events.Pattern.OnPatternSlotCompleted -= OnPatternCompleted;
            }
            _shakeTween?.Kill();
            
            if (_camController != null)
                _camController.UnregisterEffect(this);
        }

        private void OnPatternCompleted(PatternMatch match)
        {
            if (match == null) return;

            float strength = CalculateShakeStrength(match.BaseScore);
            if (strength <= 0.05f) return;

            DoShake(strength);
        }

        private float CalculateShakeStrength(int score)
        {
            if (score < 15) return 0.2f * _shakeStrengthMultiplier;
            if (score < 35) return 0.3f * _shakeStrengthMultiplier;
            if (score < 80) return 0.35f * _shakeStrengthMultiplier;
            return 0.5f * _shakeStrengthMultiplier;
        }

        private void DoShake(float strength)
        {
            _shakeTween?.Kill();
            
            _isShaking = true;
            float currentStrength = strength;

            _shakeTween = DOTween.To(() => currentStrength, x => {
                currentStrength = x;
                _shakeOffset = (Vector3)Random.insideUnitCircle * currentStrength;
            }, 0f, _baseDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                _shakeOffset = Vector3.zero;
                _isShaking = false;
                _shakeTween = null;
            });

            if (AppCore.Instance != null && AppCore.Instance.Events != null)
                AppCore.Instance.Events.Camera.TriggerCameraUpdated();
        }
        
        [ContextMenu("Test Weak Shake")]
        public void TestWeak() => DoShake(0.1f * _shakeStrengthMultiplier);

        [ContextMenu("Test Strong Shake")]
        public void TestStrong() => DoShake(0.8f * _shakeStrengthMultiplier);
    }
}
