using UnityEngine;
using DG.Tweening;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// Componente visual de uma Tradição.
    /// Similar ao CardView mas sem interação de drag/drop.
    /// Apenas hover para mostrar tooltip.
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
        private Vector3 _targetPosition;
        private bool _isHovered;
        
        // Animações
        private Tween _moveTween;
        private Tween _glowTween;
        
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
            
            UpdateVisuals();
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
            
            // Aplica escala do config
            if (_layoutConfig != null)
            {
                transform.localScale = Vector3.one * _layoutConfig.scale;
            }
        }
        
        /// <summary>
        /// Move para uma nova posição com animação.
        /// </summary>
        public void MoveTo(Vector3 position, bool animate = true)
        {
            _targetPosition = position;
            
            _moveTween?.Kill();
            
            if (animate && _layoutConfig != null)
            {
                _moveTween = transform.DOMove(position, _layoutConfig.rearrangeDuration)
                    .SetEase(Ease.OutQuad);
            }
            else
            {
                transform.position = position;
            }
        }
        
        /// <summary>
        /// Animação de spawn (entrada dramática).
        /// </summary>
        public void PlaySpawnAnimation(Vector3 targetPosition)
        {
            if (_layoutConfig == null)
            {
                transform.position = targetPosition;
                return;
            }
            
            // Começa acima e invisível
            Vector3 startPos = targetPosition + Vector3.up * 2f;
            transform.position = startPos;
            transform.localScale = Vector3.zero;
            
            // Anima para posição final
            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOMove(targetPosition, _layoutConfig.spawnDuration)
                .SetEase(_layoutConfig.spawnCurve));
            sequence.Join(transform.DOScale(Vector3.one * _layoutConfig.scale, _layoutConfig.spawnDuration)
                .SetEase(Ease.OutBack));
            
            // Pulso de glow no spawn
            if (_glowRenderer != null)
            {
                sequence.Join(_glowRenderer.DOFade(0.8f, _layoutConfig.spawnDuration * 0.5f));
                sequence.Append(_glowRenderer.DOFade(0f, _layoutConfig.spawnDuration * 0.5f));
            }
        }
        
        /// <summary>
        /// Atualiza o índice do slot.
        /// </summary>
        public void SetSlotIndex(int index)
        {
            _slotIndex = index;
        }
        
        // --- Hover (Para tooltip futuro) ---
        
        private void OnMouseEnter()
        {
            if (_layoutConfig == null) return;
            
            _isHovered = true;
            
            // Eleva levemente
            Vector3 hoverPos = _targetPosition + Vector3.up * _layoutConfig.hoverElevation;
            _moveTween?.Kill();
            _moveTween = transform.DOMove(hoverPos, 0.15f).SetEase(Ease.OutQuad);
            
            // Ativa glow
            if (_glowRenderer != null)
            {
                _glowTween?.Kill();
                _glowTween = _glowRenderer.DOFade(0.6f, 0.15f);
            }
            
            // TODO: Mostrar tooltip com descrição
        }
        
        private void OnMouseExit()
        {
            if (_layoutConfig == null) return;
            
            _isHovered = false;
            
            // Retorna à posição normal
            _moveTween?.Kill();
            _moveTween = transform.DOMove(_targetPosition, 0.15f).SetEase(Ease.OutQuad);
            
            // Desativa glow
            if (_glowRenderer != null)
            {
                _glowTween?.Kill();
                _glowTween = _glowRenderer.DOFade(0f, 0.15f);
            }
            
            // TODO: Esconder tooltip
        }
        
        private void OnDestroy()
        {
            _moveTween?.Kill();
            _glowTween?.Kill();
        }
    }
}
