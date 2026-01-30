using UnityEngine;

namespace LastFurrow.Visual.Camera
{
    /// <summary>
    /// Efeito de câmera que gerencia o scroll de borda e "Juice" (Tilt/Zoom).
    /// Implementa ICameraEffect para integrar-se ao stack de processamento.
    /// Desacoplado de hardware via ICameraInputProvider.
    /// </summary>
    [RequireComponent(typeof(GameCameraController))]
    public class CameraEdgeScroll : MonoBehaviour, ICameraEffect
    {
        [Header("Basic Scroll")]
        [SerializeField] private Vector2 _maxOffset = new Vector2(1.5f, 2.5f);
        
        [Tooltip("Sensibilidade para o topo (Y+).")]
        [Range(0.7f, 1f)] [SerializeField] private float _topThreshold = 0.95f;
        
        [Tooltip("Sensibilidade para a base (Y-).")]
        [Range(0.7f, 1f)] [SerializeField] private float _bottomThreshold = 0.90f;

        [Tooltip("Sensibilidade para as laterais (X).")]
        [Range(0.7f, 1f)] [SerializeField] private float _sideThreshold = 0.93f;
        
        [SerializeField] private float _smoothTime = 0.2f;

        [Header("Juice Settings")]
        [SerializeField] private float _tiltAmount = 0.8f;
        [SerializeField] private float _zoomOnScroll = -0.2f;
        [SerializeField] private float _juiceSmoothTime = 0.3f;

        private GameCameraController _controller;
        private ICameraInputProvider _input;

        private Vector2 _currentOffset;
        private Vector2 _targetOffset;
        private Vector2 _velocity;
        
        private float _currentRoll;
        private float _rollVelocity;
        
        private float _currentZoom;
        private float _zoomVelocity;
        
        private bool _isForced;
        private Vector2 _currentTilt3D;

        // --- ICameraEffect Implementation ---
        public int Priority => 10;
        public bool IsActive => enabled;

        public Vector3 GetPositionOffset() => (Vector3)_currentOffset;
        public Vector2 GetTiltOffset() => _currentTilt3D;
        public float GetRotationOffset() => _currentRoll;
        public float GetSizeOffset() => _currentZoom;

        private void Awake()
        {
            _controller = GetComponent<GameCameraController>();
        }

        private void Start()
        {
            // Tenta obter o provider via AppCore (Injeção de dependência via Service Locator)
            if (AppCore.Instance != null)
            {
                _input = AppCore.Instance.InputManager;
            }
            
            _controller.RegisterEffect(this);
        }

        private void OnEnable()
        {
            if (AppCore.Instance != null && AppCore.Instance.Events != null)
            {
                AppCore.Instance.Events.Time.OnResolutionStarted += HandleResolutionStarted;
                AppCore.Instance.Events.Time.OnResolutionEnded += HandleResolutionEnded;
            }
        }

        private void OnDisable()
        {
            if (AppCore.Instance != null && AppCore.Instance.Events != null)
            {
                AppCore.Instance.Events.Time.OnResolutionStarted -= HandleResolutionStarted;
                AppCore.Instance.Events.Time.OnResolutionEnded -= HandleResolutionEnded;
            }
        }

        private void OnDestroy()
        {
            if (_controller != null)
                _controller.UnregisterEffect(this);
        }

        private void HandleResolutionStarted() => SetForced(true);
        private void HandleResolutionEnded() => SetForced(false);

        private void Update()
        {
            CalculateTargetOffset();
            SmoothProcess();
        }

        private void CalculateTargetOffset()
        {
            _targetOffset = Vector2.zero;

            // 1. Prioridade: Forced Scroll (Focus na resolução)
            if (_isForced)
            {
                _targetOffset.y = _maxOffset.y;
                return;
            }

            // 2. Input via Intent (Desacoplado)
            if (_input == null || _input.IsInputLocked) return; // Se bloqueado (Shop), o alvo vira Zero, voltando suavemente via SmoothDamp

            Vector2 lookIntent = _input.LookIntent;

            // Intensidade Horizontal (X)
            float xIntensity = 0f;
            float absX = Mathf.Abs(lookIntent.x);
            if (absX > _sideThreshold)
            {
                xIntensity = Mathf.Clamp01((absX - _sideThreshold) / (1f - _sideThreshold)) * Mathf.Sign(lookIntent.x);
            }

            // Intensidade Vertical (Y)
            float yIntensity = 0f;
            float absY = Mathf.Abs(lookIntent.y);
            float vThreshold = (lookIntent.y >= 0) ? _topThreshold : _bottomThreshold;
            if (absY > vThreshold)
            {
                yIntensity = Mathf.Clamp01((absY - vThreshold) / (1f - vThreshold)) * Mathf.Sign(lookIntent.y);
            }
            
            _targetOffset = new Vector2(xIntensity * _maxOffset.x, yIntensity * _maxOffset.y);
        }

        private void SmoothProcess()
        {
            // 1. Movimento Base
            _currentOffset = Vector2.SmoothDamp(_currentOffset, _targetOffset, ref _velocity, _smoothTime);

            // 2. JUICE: Tilt 3D (Perspective)
            _currentTilt3D = new Vector2(
                -(_currentOffset.y / _maxOffset.y) * 2.5f,
                (_currentOffset.x / _maxOffset.x) * 2.5f
            );
            
            // 3. JUICE: Roll (Inércia lateral)
            float targetRoll = -(_velocity.x / 10f) * _tiltAmount;
            _currentRoll = Mathf.SmoothDamp(_currentRoll, targetRoll, ref _rollVelocity, _juiceSmoothTime);

            // 4. JUICE: Dynamic Zoom
            float distFromCenter = _currentOffset.magnitude / _maxOffset.magnitude;
            float targetZoom = distFromCenter * _zoomOnScroll;
            _currentZoom = Mathf.SmoothDamp(_currentZoom, targetZoom, ref _zoomVelocity, _juiceSmoothTime);
        }

        public void SetForced(bool forced) => _isForced = forced;

        [ContextMenu("Toggle Forced Scroll")]
        private void ToggleForced() => SetForced(!_isForced);
    }
}
