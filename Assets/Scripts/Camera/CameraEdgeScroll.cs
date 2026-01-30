using UnityEngine;

namespace LastFurrow.Visual.Camera
{
    /// <summary>
    /// Gerencia o deslocamento dinâmico da câmera para cima.
    /// Útil para visualizar tradições "coringas" no topo do grid.
    /// Integra-se ao GameCameraController via DynamicOffset.
    /// </summary>
    [RequireComponent(typeof(GameCameraController))]
    public class CameraEdgeScroll : MonoBehaviour
    {
        [Header("Basic Scroll")]
        [SerializeField] private Vector2 _maxOffset = new Vector2(1.5f, 2.5f);
        
        [Tooltip("Sensibilidade para o topo (Y+). Ex: 0.95")]
        [Range(0.7f, 1f)] [SerializeField] private float _topThreshold = 0.95f;
        
        [Tooltip("Sensibilidade para a base (Y-). Ex: 0.85")]
        [Range(0.7f, 1f)] [SerializeField] private float _bottomThreshold = 0.90f;
        
        [SerializeField] private float _smoothTime = 0.2f;

        [Header("Juice Settings")]
        [Tooltip("Intensidade da inclinação lateral ao mover.")]
        [SerializeField] private float _tiltAmount = 0.8f;
        [Tooltip("O quanto a câmera aproxima/afasta ao scrolar (Efeito Parallax).")]
        [SerializeField] private float _zoomOnScroll = -0.2f;
        [Tooltip("Suavização extra para os efeitos secundários.")]
        [SerializeField] private float _juiceSmoothTime = 0.3f;

        private GameCameraController _controller;
        private Vector2 _currentOffset;
        private Vector2 _targetOffset;
        private Vector2 _velocity;
        
        private float _currentTilt;
        private float _tiltVelocity;
        
        private float _currentZoom;
        private float _zoomVelocity;
        
        private bool _isForced;

        private void Awake()
        {
            _controller = GetComponent<GameCameraController>();
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

        private void HandleResolutionStarted() => SetForced(true);
        private void HandleResolutionEnded() => SetForced(false);

        private void Update()
        {
            bool isShopOpen = AppCore.Instance != null && AppCore.Instance.ShopService != null && AppCore.Instance.ShopService.CurrentSession != null;
            
            _targetOffset = Vector2.zero;

            // 1. Logica de Direção 360 (Circular)
            if (_isForced)
            {
                _targetOffset.y = _maxOffset.y;
            }
            else if (!isShopOpen)
            {
                // Normaliza posição do mouse (-1 a 1)
                Vector2 mouseNorm = new Vector2(
                    (Input.mousePosition.x / Screen.width - 0.5f) * 2f,
                    (Input.mousePosition.y / Screen.height - 0.5f) * 2f
                );

                // Define qual threshold usar baseado no quadrante vertical
                float currentThreshold = (mouseNorm.y >= 0) ? _topThreshold : _bottomThreshold;
                float dist = mouseNorm.magnitude;
                
                // Se o mouse estiver fora da "zona morta" central (respeitando o threshold dinâmico)
                if (dist > currentThreshold)
                {
                    // Intensidade baseada em quão perto da borda está (0 a 1)
                    float intensity = Mathf.Clamp01((dist - currentThreshold) / (1f - currentThreshold));
                    
                    // Direção exata do mouse (360 graus)
                    _targetOffset = mouseNorm.normalized * _maxOffset * intensity;
                }
            }

            // 2. Movimento Suave (360°)
            _currentOffset = Vector2.SmoothDamp(_currentOffset, _targetOffset, ref _velocity, _smoothTime);

            // 3. JUICE: Tilt 3D (Efeito de "Espiar" os cantos)
            // Se o scroll sobe (Y+), inclinamos o X negativo para a câmera olhar pra cima
            // Se o scroll vai pra direita (X+), inclinamos o Y positivo para a câmera olhar pra direita
            Vector2 targetTilt3D = new Vector2(
                -(_currentOffset.y / _maxOffset.y) * 2.5f, // Esforço X (inclina pra ver vertical)
                (_currentOffset.x / _maxOffset.x) * 2.5f   // Esforço Y (inclina pra ver horizontal)
            );
            
            // JUICE: Tilt Z (Inércia lateral baseada na velocidade do mouse)
            float targetTiltZ = -(_velocity.x / 10f) * _tiltAmount;

            _currentTilt = Mathf.SmoothDamp(_currentTilt, targetTiltZ, ref _tiltVelocity, _juiceSmoothTime);

            // 4. JUICE: Zoom Dinâmico
            float distFromCenter = _currentOffset.magnitude / _maxOffset.magnitude;
            float targetZoom = distFromCenter * _zoomOnScroll;
            _currentZoom = Mathf.SmoothDamp(_currentZoom, targetZoom, ref _zoomVelocity, _juiceSmoothTime);

            // 5. Aplica no Controller
            _controller.DynamicOffset = (Vector3)_currentOffset;
            _controller.DynamicRotation = _currentTilt;
            _controller.DynamicTilt = targetTilt3D; // Aplica o Tilt 3D (X e Y)
            _controller.DynamicSizeOffset = _currentZoom;
        }

        public void SetForced(bool forced) => _isForced = forced;

        [ContextMenu("Toggle Forced Scroll")]
        private void ToggleForced() => SetForced(!_isForced);
    }
}
