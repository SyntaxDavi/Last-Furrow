using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using System.Linq;

namespace LastFurrow.Visual.Camera
{
    /// <summary>
    /// Controlador de câmera principal que utiliza um Stack de Efeitos (Composer Pattern).
    /// 
    /// ARQUITETURA:
    /// - Base State: Definido por FitBounds (enquadramento do grid).
    /// - Effect Stack: Camadas independentes (Scroll, Shake, Focus) que somam offsets.
    /// - Desacoplado de Hardware: Reage a intents e efeitos.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class GameCameraController : MonoBehaviour
    {
        [Header("Dependências")]
        [SerializeField] private CameraFramingConfig _framingConfig;

        [Header("Movimento")]
        [SerializeField] private float _moveSmoothTime = 0.25f;

        private UnityEngine.Camera _cam;
        private ICameraFitStrategy _fitStrategy;
        private readonly List<ICameraEffect> _effects = new List<ICameraEffect>();

        // Estado base (calculado pelo enquadramento)
        private Vector3 _basePosition;
        private float _baseOrthoSize;

        // Estado atual blending (cache para evitar repetição)
        private Vector3 _totalPositionOffset;
        private Vector2 _totalTilt;
        private float _totalRotation;
        private float _totalSizeOffset;

        private Vector3 _currentVelocity;
        private Coroutine _moveRoutine;
        private bool _isConfigured = false;

        // State monitoring for optimization
        private Vector3 _lastAppliedPosition;
        private float _lastAppliedOrthoSize;
        private float _lastCameraAspect;

        // Cache configuração atual
        private GridConfiguration _lastGridConfig;
        private Vector2 _lastGridSpacing;
        private Bounds _lastGridBounds;
        private Bounds _lastCameraBounds;

        public Vector3 CameraPosition
        {
            get => _basePosition;
            set
            {
                _basePosition = value;
                ApplyFinalState();
            }
        }

        private void Awake()
        {
            _cam = GetComponent<UnityEngine.Camera>();
            _fitStrategy = new PaddedGridFitStrategy();
            ValidateDependencies();
        }

        private void LateUpdate()
        {
            if (!_isConfigured) return;

            // Monitor aspect ratio changes (runtime window resizing)
            if (!Mathf.Approximately(_cam.aspect, _lastCameraAspect))
            {
                _lastCameraAspect = _cam.aspect;
                RebuildFraming();
            }

            // O stack de efeitos é processado todo frame para garantir fluidez
            ProcessEffects();
            ApplyFinalState();
        }

        private void ValidateDependencies()
        {
            if (_framingConfig == null)
            {
                Debug.LogWarning("[GameCamera] CameraFramingConfig AUSENTE! Criando fallback.");
                _framingConfig = ScriptableObject.CreateInstance<CameraFramingConfig>();
                _framingConfig.PaddingLeft = 3f;
                _framingConfig.PaddingRight = 3f;
                _framingConfig.PaddingTop = 3.5f;
                _framingConfig.PaddingBottom = 2f;
            }
        }

        public void RegisterEffect(ICameraEffect effect)
        {
            if (!_effects.Contains(effect))
            {
                _effects.Add(effect);
                // Ordena por prioridade (maior resolve por último/sobrepõe)
                _effects.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }
        }

        public void UnregisterEffect(ICameraEffect effect)
        {
            if (_effects.Contains(effect))
            {
                _effects.Remove(effect);
            }
        }

        private void ProcessEffects()
        {
            _totalPositionOffset = Vector3.zero;
            _totalTilt = Vector2.zero;
            _totalRotation = 0f;
            _totalSizeOffset = 0f;

            foreach (var effect in _effects)
            {
                if (effect != null && effect.IsActive)
                {
                    _totalPositionOffset += effect.GetPositionOffset();
                    _totalTilt += effect.GetTiltOffset();
                    _totalRotation += effect.GetRotationOffset();
                    _totalSizeOffset += effect.GetSizeOffset();
                }
            }
        }

        private void ApplyFinalState()
        {
            Vector3 targetPos = _basePosition + _totalPositionOffset;
            float targetSize = _baseOrthoSize + _totalSizeOffset;

            // 1. Posição
            transform.position = targetPos;

            // 2. Rotação (Tilt 3D + Z Roll)
            transform.localRotation = Quaternion.Euler(_totalTilt.x, _totalTilt.y, _totalRotation);

            // 3. Zoom
            if (_cam != null)
            {
                _cam.orthographicSize = targetSize;
            }

            // Optimization: Only notify if there's a significant change
            if (Vector3.Distance(targetPos, _lastAppliedPosition) > 0.001f || !Mathf.Approximately(targetSize, _lastAppliedOrthoSize))
            {
                _lastAppliedPosition = targetPos;
                _lastAppliedOrthoSize = targetSize;
                NotifyUpdate();
            }
        }

        public void ConfigureFromGrid(GridConfiguration gridConfig, Vector2 gridSpacing)
        {
            if (_cam == null) _cam = GetComponent<UnityEngine.Camera>();
            
            _lastGridConfig = gridConfig;
            _lastGridSpacing = gridSpacing;
            _lastCameraAspect = _cam.aspect;
            _isConfigured = true;

            RebuildFraming();
        }

        [ContextMenu("Rebuild Framing")]
        public void RebuildFraming()
        {
            if (!_isConfigured) return;

            var (width, height) = _fitStrategy.CalculateRequiredBounds(_lastGridConfig, _lastGridSpacing, _framingConfig);

            _lastGridBounds = new Bounds(Vector3.zero, new Vector3(_lastGridConfig.Columns * _lastGridSpacing.x, _lastGridConfig.Rows * _lastGridSpacing.y, 0));
            _lastCameraBounds = new Bounds(Vector3.zero, new Vector3(width, height, 0));

            _basePosition = new Vector3(0, 0, -10f);
            FitBounds(width, height);
        }

        private void FitBounds(float width, float height)
        {
            float targetRatio = width / height;
            float cameraRatio = _cam.aspect;
            float requiredHeightInUnits = (cameraRatio >= targetRatio) ? height : (width / cameraRatio);

            // Removido Snapping de Pixel Perfect para garantir fluidez
            _baseOrthoSize = requiredHeightInUnits / 2f;
            ApplyFinalState();
        }

        public void PanTo(Vector3 targetPosition, float duration = -1f)
        {
            targetPosition.z = transform.position.z;
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            float time = duration > 0 ? duration : _moveSmoothTime;
            _moveRoutine = StartCoroutine(PanRoutine(targetPosition, time));
        }

        private IEnumerator PanRoutine(Vector3 targetPos, float smoothTime)
        {
            while (Vector3.Distance(_basePosition, targetPos) > 0.01f)
            {
                _basePosition = Vector3.SmoothDamp(_basePosition, targetPos, ref _currentVelocity, smoothTime);
                ApplyFinalState();
                yield return null;
            }
            _basePosition = targetPos;
            ApplyFinalState();
            _moveRoutine = null;
        }

        private void NotifyUpdate()
        {
            if (AppCore.Instance != null && AppCore.Instance.Events != null)
                AppCore.Instance.Events.Camera.TriggerCameraUpdated();
        }

        public Bounds GetVisibleWorldBounds()
        {
            if (_cam == null) return new Bounds();
            float height = _cam.orthographicSize * 2f;
            float width = height * _cam.aspect;
            return new Bounds(transform.position + Vector3.forward * 10f, new Vector3(width, height, 0));
        }

        private void OnDrawGizmos()
        {
            if (_framingConfig == null || !_framingConfig.ShowDebugBounds) return;
            if (_lastGridBounds.size.x > 0)
            {
                Gizmos.color = _framingConfig.GridBoundsColor;
                Gizmos.DrawWireCube(_lastGridBounds.center, _lastGridBounds.size);
            }
            var realBounds = GetVisibleWorldBounds();
            if (realBounds.size.x > 0)
            {
                Gizmos.color = _framingConfig.CameraBoundsColor;
                Gizmos.DrawWireCube(realBounds.center, realBounds.size);
            }
        }

        // --- BACKWARD COMPATIBILITY (Temporary) ---
        [System.Obsolete("Use effects stack instead.")]
        public void DoShake(float d, float s) => Debug.LogWarning("DoShake is obsolete. Use a ShakeEffect layer.");
    }
}
