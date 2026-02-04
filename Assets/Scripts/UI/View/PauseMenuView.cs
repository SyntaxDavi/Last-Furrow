using System;
using UnityEngine;
using UnityEngine.UI;

namespace LastFurrow.UI.Pause
{
    /// <summary>
    /// Professional Pure View for the Pause Menu.
    /// - Reactive: No knowledge of GameState or AppCore.
    /// - Explicit: Methods for listeners instead of lambdas.
    /// - Robust: Strict lifecycle management.
    /// </summary>
    public class PauseMenuView : UIView
    {
        public event Action OnResumeClicked;
        public event Action OnOptionsClicked;
        public event Action OnMainMenuClicked;
        public event Action OnQuitDesktopClicked;

        [Header("Button References")]
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _optionsButton;
        [SerializeField] private Button _mainMenuButton;
        [SerializeField] private Button _quitDesktopButton;

        private void OnEnable()
        {
            if (_resumeButton != null) _resumeButton.onClick.AddListener(HandleResume);
            if (_optionsButton != null) _optionsButton.onClick.AddListener(HandleOptions);
            if (_mainMenuButton != null) _mainMenuButton.onClick.AddListener(HandleMainMenu);
            if (_quitDesktopButton != null) _quitDesktopButton.onClick.AddListener(HandleQuitDesktop);
        }

        private void OnDisable()
        {
            if (_resumeButton != null) _resumeButton.onClick.RemoveListener(HandleResume);
            if (_optionsButton != null) _optionsButton.onClick.RemoveListener(HandleOptions);
            if (_mainMenuButton != null) _mainMenuButton.onClick.RemoveListener(HandleMainMenu);
            if (_quitDesktopButton != null) _quitDesktopButton.onClick.RemoveListener(HandleQuitDesktop);
        }

        private void HandleResume() => OnResumeClicked?.Invoke();
        private void HandleOptions() => OnOptionsClicked?.Invoke();
        private void HandleMainMenu() => OnMainMenuClicked?.Invoke();
        private void HandleQuitDesktop() => OnQuitDesktopClicked?.Invoke();
    }
}
