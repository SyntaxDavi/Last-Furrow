using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LastFurrow.UI.Components;

namespace LastFurrow.UI.MainMenu
{
    /// <summary>
    /// Pure View for the Main Menu with animated text support.
    /// Supports both legacy Button mode and new AnimatedMenuText mode.
    /// </summary>
    public class MainMenuView : UIView
    {
        public event Action<MainMenuAction> OnActionTriggered;

        [Header("Background")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Sprite _backgroundSprite;

        [Header("Animated Menu System")]
        [SerializeField] private MenuGroup _menuGroup;
        [SerializeField] private AnimatedMenuText _newGameText;
        [SerializeField] private AnimatedMenuText _continueText;
        [SerializeField] private AnimatedMenuText _quitText;

        [Header("Legacy Button References (optional fallback)")]
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _quitButton;

        [Header("Text References")]
        [SerializeField] private TextMeshProUGUI _versionText;

        private bool _useAnimatedMenu = false;

        protected override void Awake()
        {
            base.Awake(); 
            
            // Determine which system to use based on what's assigned
            _useAnimatedMenu = _menuGroup != null && _newGameText != null;
            
            // Setup background if assigned
            if (_backgroundImage != null && _backgroundSprite != null)
            {
                _backgroundImage.sprite = _backgroundSprite;
                _backgroundImage.enabled = true;
            }
        }

        private void OnEnable()
        {
            if (_useAnimatedMenu)
            {
                // Use new animated system
                _menuGroup.Initialize();
                _menuGroup.OnItemConfirmed += HandleItemConfirmed;
            }
            else
            {
                // Fallback to legacy buttons
                if (_newGameButton != null) _newGameButton.onClick.AddListener(OnNewGameClicked);
                if (_continueButton != null) _continueButton.onClick.AddListener(OnContinueClicked);
                if (_quitButton != null) _quitButton.onClick.AddListener(OnQuitClicked);
            }
        }

        private void OnDisable()
        {
            if (_useAnimatedMenu)
            {
                if (_menuGroup != null)
                    _menuGroup.OnItemConfirmed -= HandleItemConfirmed;
            }
            else
            {
                if (_newGameButton != null) _newGameButton.onClick.RemoveListener(OnNewGameClicked);
                if (_continueButton != null) _continueButton.onClick.RemoveListener(OnContinueClicked);
                if (_quitButton != null) _quitButton.onClick.RemoveListener(OnQuitClicked);
            }
        }

        private void HandleItemConfirmed(AnimatedMenuText item)
        {
            if (item == _newGameText)
                OnActionTriggered?.Invoke(MainMenuAction.NewGame);
            else if (item == _continueText)
                OnActionTriggered?.Invoke(MainMenuAction.Continue);
            else if (item == _quitText)
                OnActionTriggered?.Invoke(MainMenuAction.Quit);
        }

        public void SetVersionText(string versionText)
        {
            if (_versionText != null) _versionText.text = versionText;
        }

        public void SetContinueButtonState(bool isAvailable)
        {
            if (_useAnimatedMenu)
            {
                // Use MenuGroup's API to set interactable state
                // Continue is typically index 1 (after NewGame)
                if (_menuGroup != null)
                    _menuGroup.SetItemInteractable(1, isAvailable);
            }
            else
            {
                if (_continueButton != null) _continueButton.interactable = isAvailable;
            }
        }

        // Legacy button handlers (still work if using Button mode)
        private void OnNewGameClicked() => OnActionTriggered?.Invoke(MainMenuAction.NewGame);
        private void OnContinueClicked() => OnActionTriggered?.Invoke(MainMenuAction.Continue);
        private void OnQuitClicked() => OnActionTriggered?.Invoke(MainMenuAction.Quit);
    }
}
