using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using DG.Tweening;

namespace LastFurrow.UI.Components
{
    /// <summary>
    /// BRAIN pattern: Owns all selection state, arrow, input handling, and SFX.
    /// Menu items (AnimatedMenuText) are MUSCLE - visual only.
    /// 
    /// Architecture:
    /// - Single source of truth for selection index
    /// - Owns the global arrow indicator
    /// - Handles keyboard navigation (via IMenuInputProvider or fallback)
    /// - Plays navigation/confirm SFX
    /// - Skips disabled items during navigation
    /// </summary>
    public class MenuGroup : MonoBehaviour
    {
        public event Action<AnimatedMenuText> OnItemConfirmed;
        
        [Header("Configuration")]
        [SerializeField] private MenuTextStyle _style;
        [SerializeField] private bool _wrapNavigation = true;
        [SerializeField] private int _defaultSelectedIndex = 0;
        
        [Header("Menu Items")]
        [SerializeField] private List<AnimatedMenuText> _menuItems = new List<AnimatedMenuText>();
        
        [Header("Selection Arrow (will be reparented to each item's arrowAnchor)")]
        [SerializeField] private GameObject _arrowPrefab;
        
        [Header("Input")]
        [SerializeField] private bool _useKeyboardFallback = true;
        
        private IMenuInputProvider _inputProvider;
        private int _currentIndex = -1;
        private bool _isInitialized = false;
        private Tween _arrowBobTween;
        private GameObject _arrowInstance;
        
        /// <summary>
        /// Currently selected item, or null if none.
        /// </summary>
        public AnimatedMenuText CurrentItem => 
            _currentIndex >= 0 && _currentIndex < _menuItems.Count 
                ? _menuItems[_currentIndex] 
                : null;
        
        /// <summary>
        /// Initialize with optional input provider.
        /// If null and _useKeyboardFallback is true, uses keyboard polling.
        /// </summary>
        public void Initialize(IMenuInputProvider inputProvider = null)
        {
            _inputProvider = inputProvider;
            
            // Initialize all items with style
            foreach (var item in _menuItems)
            {
                if (item == null) continue;
                
                item.Initialize(_style);
                item.OnPointerEnterRequest += HandlePointerEnterRequest;
                item.OnClickRequest += HandleClickRequest;
            }
            
            // Initial arrow setup will happen on first SelectIndex call
            if (_arrowInstance != null)
            {
                _arrowInstance.SetActive(true);
            }
            else if (_arrowPrefab != null && !IsPrefabAsset(_arrowPrefab))
            {
                // If it's already a scene object, use it directly
                _arrowInstance = _arrowPrefab;
                _arrowInstance.SetActive(true);
            }
            
            // Subscribe to input provider if available
            if (_inputProvider != null)
            {
                _inputProvider.OnNavigateUp += NavigateUp;
                _inputProvider.OnNavigateDown += NavigateDown;
                _inputProvider.OnConfirm += ConfirmSelection;
            }
            
            _isInitialized = true;
            
            // Select default item
            SelectIndex(_defaultSelectedIndex, immediate: true, playSound: false);
        }
        
        private void OnEnable()
        {
            if (_isInitialized && _currentIndex >= 0)
            {
                // Restore selection visual when re-enabled
                SelectIndex(_currentIndex, immediate: true, playSound: false);
            }
        }
        
        private void OnDisable()
        {
            KillArrowTweens();
        }
        
        private void OnDestroy()
        {
            // Cleanup subscriptions
            foreach (var item in _menuItems)
            {
                if (item == null) continue;
                item.OnPointerEnterRequest -= HandlePointerEnterRequest;
                item.OnClickRequest -= HandleClickRequest;
            }
            
            if (_inputProvider != null)
            {
                _inputProvider.OnNavigateUp -= NavigateUp;
                _inputProvider.OnNavigateDown -= NavigateDown;
                _inputProvider.OnConfirm -= ConfirmSelection;
            }
            
            DOTween.Kill(this);
        }
        
        private void Update()
        {
            if (!_isInitialized) return;
            
            // Fallback keyboard input when no provider
            if (_inputProvider == null && _useKeyboardFallback)
            {
                PollKeyboardInput();
            }
        }
        
        #region Input Handling
        
        private void PollKeyboardInput()
        {
            // Navigation
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                NavigateUp();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                NavigateDown();
            }
            
            // Confirm
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                ConfirmSelection();
            }
        }
        
        #endregion
        
        #region Navigation
        
        public void NavigateUp()
        {
            int newIndex = FindNextInteractableIndex(_currentIndex, -1);
            if (newIndex != _currentIndex)
            {
                SelectIndex(newIndex, immediate: false, playSound: true);
            }
        }
        
        public void NavigateDown()
        {
            int newIndex = FindNextInteractableIndex(_currentIndex, 1);
            if (newIndex != _currentIndex)
            {
                SelectIndex(newIndex, immediate: false, playSound: true);
            }
        }
        
        /// <summary>
        /// Find next interactable item in direction, skipping disabled ones.
        /// </summary>
        private int FindNextInteractableIndex(int fromIndex, int direction)
        {
            if (_menuItems.Count == 0) return -1;
            
            int attempts = _menuItems.Count;
            int index = fromIndex;
            
            while (attempts > 0)
            {
                index += direction;
                
                // Handle wrap or clamp
                if (index < 0)
                {
                    index = _wrapNavigation ? _menuItems.Count - 1 : 0;
                }
                else if (index >= _menuItems.Count)
                {
                    index = _wrapNavigation ? 0 : _menuItems.Count - 1;
                }
                
                // Check if this item is interactable
                var item = _menuItems[index];
                if (item != null && item.IsInteractable)
                {
                    return index;
                }
                
                // Prevent infinite loop when no wrap
                if (!_wrapNavigation && (index == 0 || index == _menuItems.Count - 1))
                {
                    break;
                }
                
                attempts--;
            }
            
            return fromIndex; // No valid item found
        }
        
        #endregion
        
        #region Selection
        
        private void HandlePointerEnterRequest(AnimatedMenuText item)
        {
            int index = _menuItems.IndexOf(item);
            if (index >= 0 && index != _currentIndex)
            {
                SelectIndex(index, immediate: false, playSound: true);
            }
        }
        
        private void HandleClickRequest(AnimatedMenuText item)
        {
            // Ensure this item is selected first
            int index = _menuItems.IndexOf(item);
            if (index >= 0)
            {
                if (index != _currentIndex)
                {
                    SelectIndex(index, immediate: true, playSound: false);
                }
                ConfirmSelection();
            }
        }
        
        /// <summary>
        /// Select item at index. Single source of truth.
        /// </summary>
        private void SelectIndex(int newIndex, bool immediate, bool playSound)
        {
            // Validate index
            if (newIndex < 0 || newIndex >= _menuItems.Count)
            {
                Debug.LogWarning($"[MenuGroup] SelectIndex FAILED: index {newIndex} out of range (count: {_menuItems.Count})");
                return;
            }
            
            var newItem = _menuItems[newIndex];
            if (newItem == null)
            {
                Debug.LogWarning($"[MenuGroup] SelectIndex FAILED: item at index {newIndex} is null");
                return;
            }
            if (!newItem.IsInteractable)
            {
                Debug.LogWarning($"[MenuGroup] SelectIndex FAILED: item '{newItem.name}' is not interactable");
                return;
            }
            
            
            // Deselect previous
            if (_currentIndex >= 0 && _currentIndex < _menuItems.Count)
            {
                var oldItem = _menuItems[_currentIndex];
                if (oldItem != null)
                {
                    oldItem.SetSelected(false);
                }
            }
            
            // Select new
            _currentIndex = newIndex;
            newItem.SetSelected(true);
            
            // Move arrow
            UpdateArrowPosition(newItem.ArrowAnchor, immediate);
            
            // Play SFX (only if sound requested and we have audio)
            if (playSound && _style != null && _style.NavigateSFX != null)
            {
                PlaySFX(_style.NavigateSFX);
            }
        }
        
        public void ConfirmSelection()
        {
            if (_currentIndex < 0 || _currentIndex >= _menuItems.Count) return;
            
            var item = _menuItems[_currentIndex];
            if (item == null || !item.IsInteractable) return;
            
            // Play click feedback
            item.PlayClickFeedback();
            
            // Play SFX
            if (_style != null && _style.ConfirmSFX != null)
            {
                PlaySFX(_style.ConfirmSFX);
            }
            
            // Notify listeners
            OnItemConfirmed?.Invoke(item);
        }
        
        #endregion
        
        #region Arrow Management (global, not per-item)
        
        private void UpdateArrowPosition(RectTransform target, bool immediate)
        {
            if (_arrowPrefab == null)
            {
                Debug.LogWarning("[MenuGroup] UpdateArrowPosition FAILED: _arrowPrefab is NULL! Assign a prefab or scene object in Inspector.");
                return;
            }
            
            if (target == null)
            {
                Debug.LogWarning("[MenuGroup] UpdateArrowPosition FAILED: target ArrowAnchor is NULL!");
                return;
            }

            // Handle Instantiation if needed
            if (_arrowInstance == null)
            {
                if (IsPrefabAsset(_arrowPrefab))
                {
                    _arrowInstance = Instantiate(_arrowPrefab, target);
                }
                else
                {
                    _arrowInstance = _arrowPrefab;
                }
            }
            
            // Show arrow
            _arrowInstance.SetActive(true);
            
            // Re-parent arrow to the new anchor
            _arrowInstance.transform.SetParent(target, false);
            
            // Reset transforms
            _arrowInstance.transform.localPosition = Vector3.zero;
            _arrowInstance.transform.localRotation = Quaternion.identity;
            _arrowInstance.transform.localScale = Vector3.one;
            
            // Apply X offset from style
            float offsetX = _style != null ? _style.ArrowOffsetX : -40f;
            Vector3 finalPos = new Vector3(offsetX, 0, 0);
            
            KillArrowTweens();
            
            // Reparenting typically works best with immediate snap
            _arrowInstance.transform.localPosition = finalPos;
            
            // Start/restart bob animation
            StartArrowBob();
        }

        private bool IsPrefabAsset(GameObject go)
        {
            return go.scene.name == null;
        }
        
        private void StartArrowBob()
        {
            if (_arrowInstance == null || _style == null) return;
            
            // Kill existing bob
            _arrowBobTween?.Kill();
            
            float bobAmount = _style.ArrowBobAmount;
            float bobDuration = _style.ArrowBobDuration;
            
            // Simple horizontal breathing relative to current parent
            _arrowBobTween = _arrowInstance.transform
                .DOLocalMoveX(_arrowInstance.transform.localPosition.x + bobAmount, bobDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetTarget(this)
                .SetUpdate(true);
        }
        
        private void KillArrowTweens()
        {
            _arrowBobTween?.Kill();
        }
        
        #endregion
        
        #region Audio
        
        private void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            
            float volume = _style != null ? _style.SFXVolume : 0.5f;
            
            // Use AudioManager if available
            if (AppCore.Instance != null && AppCore.Instance.AudioManager != null)
            {
                AppCore.Instance.AudioManager.PlaySFX(clip, volume);
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Set interactable state for a specific item.
        /// MenuGroup handles navigation skip automatically.
        /// </summary>
        public void SetItemInteractable(int index, bool interactable)
        {
            if (index < 0 || index >= _menuItems.Count) return;
            
            var item = _menuItems[index];
            if (item == null) return;
            
            item.SetInteractable(interactable);
            
            // If disabling currently selected, move to next
            if (!interactable && index == _currentIndex)
            {
                int newIndex = FindNextInteractableIndex(_currentIndex, 1);
                if (newIndex != _currentIndex)
                {
                    SelectIndex(newIndex, immediate: false, playSound: false);
                }
            }
        }
        
        /// <summary>
        /// Force selection to a specific index.
        /// </summary>
        public void ForceSelect(int index)
        {
            SelectIndex(index, immediate: true, playSound: false);
        }
        
        #endregion
    }
}
