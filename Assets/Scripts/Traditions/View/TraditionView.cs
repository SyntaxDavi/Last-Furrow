using UnityEngine;
using DG.Tweening;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// Componente visual de uma Tradição.
    /// Implementa física idle (levitação) e hover suave similar ao CardView.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class TraditionView : MonoBehaviour
    {
        [Header("Visual References")]
        [SerializeField] private SpriteRenderer _iconRenderer;
        [SerializeField] private SpriteRenderer _frameRenderer;
        [SerializeField] private SpriteRenderer _glowRenderer;
        
        [Header("Config")]
        [SerializeField] private TraditionLayoutConfig _layoutConfig;
        
        // Estado interno
        private TraditionInstance _instance;
        private TraditionData _data;
        private int _slotIndex;
        private float _randomSeed;
        
        // Posição alvo (slot do layout)
        private Vector3 _basePosition;
        
        // Estado de hover
        private bool _isHovered;
        
        // Física suave (SmoothDamp)
        private Vector3 _currentPosition;
        private Vector3 _positionVelocity;
        private Vector3 _currentScale;
        private Vector3 _scaleVelocity;
        private float _currentGlowAlpha;
        private float _glowVelocity;
        
        // Evento de clique (para ViewManager)
        public event System.Action<TraditionView> OnClicked;
        
        /// <summary>
        /// Instância da tradição que este view representa.
        /// </summary>
        public TraditionInstance Instance => _instance;
        
        /// <summary>
        /// Índice do slot na barra de tradições.
        /// </summary>
        public int SlotIndex => _slotIndex;
        
        /// <summary>
        /// Inicializa o view com uma instância de tradição.
        /// </summary>
        public void Initialize(TraditionInstance instance, TraditionData data, int slotIndex, TraditionLayoutConfig config)
        {
            _instance = instance;
            _data = data;
            _slotIndex = slotIndex;
            _layoutConfig = config ?? _layoutConfig;
            _randomSeed = Random.Range(0f, 100f);
            
            UpdateVisuals();
            
            // Inicializa posição imediata
            if (_layoutConfig != null)
            {
                _currentScale = Vector3.one * _layoutConfig.scale;
                transform.localScale = _currentScale;
            }
        }
        
        /// <summary>
        /// Atualiza os sprites baseado nos dados da tradição.
        /// </summary>
        public void UpdateVisuals()
        {
            if (_data == null) return;
            
            if (_iconRenderer != null && _data.Icon != null)
            {
                _iconRenderer.sprite = _data.Icon;
            }
            
            if (_frameRenderer != null && _data.CardFrame != null)
            {
                _frameRenderer.sprite = _data.CardFrame;
            }
            
            if (_glowRenderer != null)
            {
                _glowRenderer.color = new Color(
                    _data.GlowColor.r,
                    _data.GlowColor.g,
                    _data.GlowColor.b,
                    0f // Começa invisível
                );
            }
        }
        
        private void Update()
        {
            if (_layoutConfig == null) return;
            
            // Calcula visuals e aplica física suave
            CalculateAndApplyVisuals();
        }
        
        private void CalculateAndApplyVisuals()
        {
            float time = Time.time + _randomSeed;
            float dt = Time.deltaTime;
            
            // 1. POSIÇÃO BASE + IDLE FLOAT
            Vector3 targetPos = _basePosition;
            
            // Flutuação Idle (senoide)
            float floatY = Mathf.Sin(time * _layoutConfig.idleFloatSpeed) * _layoutConfig.idleFloatAmount;
            targetPos.y += floatY;
            
            // 2. HOVER OFFSET (desce ao invés de subir)
            if (_isHovered)
            {
                targetPos.y += _layoutConfig.hoverElevation;
            }
            
            // 3. ESCALA
            Vector3 targetScale = Vector3.one * _layoutConfig.scale;
            if (_isHovered)
            {
                targetScale = Vector3.one * _layoutConfig.scale * _layoutConfig.hoverScale;
            }
            
            // 4. ROTAÇÃO IDLE (sutil)
            float rotZ = Mathf.Cos(time * _layoutConfig.idleFloatSpeed * 0.7f) * _layoutConfig.idleRotationAmount;
            Quaternion targetRotation = Quaternion.Euler(0, 0, rotZ);
            
            // 5. GLOW
            float targetGlow = _isHovered ? _layoutConfig.hoverGlowIntensity : 0f;
            
            // APLICAR COM SMOOTH DAMP
            _currentPosition = Vector3.SmoothDamp(_currentPosition, targetPos, ref _positionVelocity, _layoutConfig.hoverSmoothTime, Mathf.Infinity, dt);
            _currentScale = Vector3.SmoothDamp(_currentScale, targetScale, ref _scaleVelocity, _layoutConfig.hoverSmoothTime, Mathf.Infinity, dt);
            _currentGlowAlpha = Mathf.SmoothDamp(_currentGlowAlpha, targetGlow, ref _glowVelocity, _layoutConfig.hoverSmoothTime, Mathf.Infinity, dt);
            
            // APLICAR AO TRANSFORM
            transform.localPosition = _currentPosition;
            transform.localScale = _currentScale;
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, dt * 10f);
            
            // APLICAR GLOW
            if (_glowRenderer != null)
            {
                var c = _glowRenderer.color;
                _glowRenderer.color = new Color(c.r, c.g, c.b, _currentGlowAlpha);
            }
        }
        
        /// <summary>
        /// Move para uma nova posição local (define a base position).
        /// </summary>
        public void MoveTo(Vector3 localPosition, bool animate = true)
        {
            _basePosition = localPosition;
            
            if (!animate)
            {
                _currentPosition = localPosition;
                transform.localPosition = localPosition;
            }
        }
        
        /// <summary>
        /// Animação de spawn (entrada dramática).
        /// </summary>
        public void PlaySpawnAnimation(Vector3 targetLocalPosition)
        {
            _basePosition = targetLocalPosition;
            
            if (_layoutConfig == null)
            {
                _currentPosition = targetLocalPosition;
                transform.localPosition = targetLocalPosition;
                return;
            }
            
            // Começa acima e invisível (relativo ao target)
            Vector3 startPos = targetLocalPosition + Vector3.up * 2f;
            _currentPosition = startPos;
            transform.localPosition = startPos;
            transform.localScale = Vector3.zero;
            _currentScale = Vector3.zero;
            
            // Anima escala com DOTween (apenas para o spawn)
            transform.DOScale(Vector3.one * _layoutConfig.scale, _layoutConfig.spawnDuration)
                .SetEase(Ease.OutBack)
                .OnUpdate(() => _currentScale = transform.localScale);
            
            // Pulso de glow no spawn
            if (_glowRenderer != null)
            {
                var seq = DOTween.Sequence();
                seq.Append(_glowRenderer.DOFade(0.8f, _layoutConfig.spawnDuration * 0.5f));
                seq.Append(_glowRenderer.DOFade(0f, _layoutConfig.spawnDuration * 0.5f));
            }
        }
        
        /// <summary>
        /// Atualiza o índice do slot.
        /// </summary>
        public void SetSlotIndex(int index)
        {
            _slotIndex = index;
        }
        
        // --- Hover ---
        
        private void OnMouseEnter()
        {
            _isHovered = true;
        }
        
        private void OnMouseExit()
        {
            _isHovered = false;
        }
        
        private void OnMouseDown()
        {
            OnClicked?.Invoke(this);
        }
        
        private void OnDestroy()
        {
            DOTween.Kill(transform);
            if (_glowRenderer != null)
            {
                DOTween.Kill(_glowRenderer);
            }
        }
    }
}
