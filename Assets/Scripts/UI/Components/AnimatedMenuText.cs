using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using System;

namespace LastFurrow.UI.Components
{
    /// <summary>
    /// Visual-only animated menu text item (MUSCLE pattern).
    /// Does NOT own selection state - only reacts to SetSelected(bool).
    /// Reports pointer events to parent MenuGroup for decision.
    /// 
    /// State Priority (highest to lowest):
    /// 1. Disabled
    /// 2. Selected
    /// 3. Normal
    /// </summary>
    public class AnimatedMenuText : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        public event Action<AnimatedMenuText> OnPointerEnterRequest;
        public event Action<AnimatedMenuText> OnClickRequest;
        
        [Header("References")]
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private RectTransform _arrowAnchor; // Where arrow should position
        
        [Header("Style (injected by MenuGroup)")]
        private MenuTextStyle _style;
        
        private bool _isInteractable = true;
        private bool _isSelected = false;
        private Tween _scaleTween;
        private Tween _colorTween;
        
        /// <summary>
        /// Anchor point where MenuGroup should position the arrow.
        /// </summary>
        public RectTransform ArrowAnchor => _arrowAnchor != null ? _arrowAnchor : (RectTransform)transform;
        
        /// <summary>
        /// Is this item currently interactable?
        /// </summary>
        public bool IsInteractable => _isInteractable;
        
        /// <summary>
        /// Initialize with style from MenuGroup.
        /// </summary>
        public void Initialize(MenuTextStyle style)
        {
            _style = style;
            ApplyVisualState(immediate: true);
        }
        
        /// <summary>
        /// Set interactable state. Called by parent MenuGroup.
        /// </summary>
        public void SetInteractable(bool value)
        {
            if (_isInteractable == value) return;
            
            _isInteractable = value;
            
            // If disabling while selected, MenuGroup handles deselection
            ApplyVisualState(immediate: false);
        }
        
        /// <summary>
        /// Set selection state. Only MenuGroup should call this.
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (_isSelected == selected) return;
            
            _isSelected = selected;
            ApplyVisualState(immediate: false);
        }
        
        /// <summary>
        /// Play click feedback animation. Called by MenuGroup after confirm.
        /// </summary>
        public void PlayClickFeedback()
        {
            if (_style == null || _text == null) return;
            
            KillTweens();
            
            // Quick scale pulse: up then back
            _scaleTween = _text.transform
                .DOScale(_style.ClickScale, _style.ClickDuration * 0.5f)
                .SetEase(_style.ClickEase)
                .SetTarget(this)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _scaleTween = _text.transform
                        .DOScale(_style.SelectedScale, _style.ClickDuration * 0.5f)
                        .SetEase(Ease.OutQuad)
                        .SetTarget(this)
                        .SetUpdate(true);
                });
        }
        
        #region Pointer Events (report to MenuGroup, don't decide)
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isInteractable) return;
            
            // Just report - let MenuGroup decide
            OnPointerEnterRequest?.Invoke(this);
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isInteractable) return;
            
            // Just report - let MenuGroup decide
            OnClickRequest?.Invoke(this);
        }
        
        #endregion
        
        #region Visual State Management
        
        private void ApplyVisualState(bool immediate)
        {
            if (_style == null || _text == null) return;
            
            // Determine target state based on priority
            Color targetColor;
            float targetScale;
            
            if (!_isInteractable)
            {
                // Priority 1: Disabled
                targetColor = _style.DisabledColor;
                targetScale = _style.NormalScale;
            }
            else if (_isSelected)
            {
                // Priority 2: Selected
                targetColor = _style.SelectedColor;
                targetScale = _style.SelectedScale;
            }
            else
            {
                // Priority 3: Normal
                targetColor = _style.NormalColor;
                targetScale = _style.NormalScale;
            }
            
            KillTweens();
            
            if (immediate)
            {
                _text.color = targetColor;
                _text.transform.localScale = Vector3.one * targetScale;
            }
            else
            {
                float duration = _style.SelectDuration;
                
                _colorTween = _text
                    .DOColor(targetColor, duration)
                    .SetEase(_style.SelectEase)
                    .SetTarget(this)
                    .SetUpdate(true);
                
                _scaleTween = _text.transform
                    .DOScale(targetScale, duration)
                    .SetEase(_style.SelectEase)
                    .SetTarget(this)
                    .SetUpdate(true);
            }
        }
        
        private void KillTweens()
        {
            _scaleTween?.Kill();
            _colorTween?.Kill();
        }
        
        #endregion
        
        private void OnDisable()
        {
            DOTween.Kill(this);
        }
        
        private void OnDestroy()
        {
            DOTween.Kill(this);
        }
    }
}
