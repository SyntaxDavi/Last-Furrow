using System;
using UnityEngine;
using UnityEngine.UI;
using LastFurrow.UI.Components;

namespace LastFurrow.UI.Pause
{
    /// <summary>
    /// Pure View for the Pause Menu with animated text support.
    /// Supports both legacy Button mode and new AnimatedMenuText mode.
    /// </summary>
    public class PauseMenuView : UIView
    {
        public event Action OnResumeClicked;
        public event Action OnOptionsClicked;
        public event Action OnMainMenuClicked;
        public event Action OnQuitDesktopClicked;

        [Header("Animated Menu System")]
        [SerializeField] private MenuGroup _menuGroup;
        [SerializeField] private AnimatedMenuText _resumeText;
        [SerializeField] private AnimatedMenuText _optionsText;
        [SerializeField] private AnimatedMenuText _mainMenuText;
        [SerializeField] private AnimatedMenuText _quitDesktopText;

        [Header("Legacy Button References (optional fallback)")]
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _optionsButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _quitDesktopButton;

        private bool _useAnimatedMenu = false;

        protected override void Awake()
        {
            base.Awake(); 
            
            _useAnimatedMenu = _menuGroup != null && _resumeText != null;
        }

        private void OnEnable()
        {
            if (_useAnimatedMenu)
            {
                _menuGroup.Initialize();
                _menuGroup.OnItemConfirmed += HandleItemConfirmed;
            }
            else
            {
                if (_resumeButton != null) _resumeButton.onClick.AddListener(HandleResume);
                if (_optionsButton != null) _optionsButton.onClick.AddListener(HandleOptions);
                if (_mainMenuButton != null) _mainMenuButton.onClick.AddListener(HandleMainMenu);
                if (_quitDesktopButton != null) _quitDesktopButton.onClick.AddListener(HandleQuitDesktop);
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
                if (_resumeButton != null) _resumeButton.onClick.RemoveListener(HandleResume);
                if (_optionsButton != null) _optionsButton.onClick.RemoveListener(HandleOptions);
                if (_mainMenuButton != null) _mainMenuButton.onClick.RemoveListener(HandleMainMenu);
                if (_quitDesktopButton != null) _quitDesktopButton.onClick.RemoveListener(HandleQuitDesktop);
            }
        }

        private void HandleItemConfirmed(AnimatedMenuText item)
        {
            if (item == _resumeText)
                OnResumeClicked?.Invoke();
            else if (item == _optionsText)
                OnOptionsClicked?.Invoke();
            else if (item == _mainMenuText)
                OnMainMenuClicked?.Invoke();
            else if (item == _quitDesktopText)
                OnQuitDesktopClicked?.Invoke();
        }

        // Legacy button handlers
        private void HandleResume() => OnResumeClicked?.Invoke();
        private void HandleOptions() => OnOptionsClicked?.Invoke();
        private void HandleMainMenu() => OnMainMenuClicked?.Invoke();
        private void HandleQuitDesktop() => OnQuitDesktopClicked?.Invoke();
    }
}
