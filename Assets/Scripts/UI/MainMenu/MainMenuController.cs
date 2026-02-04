using System;
using UnityEngine;

namespace LastFurrow.UI.MainMenu
{
    /// <summary>
    /// Professional Presenter for the Main Menu.
    /// - Follows "Dumb View/Controller" pattern: No knowledge of game flow or singletons.
    /// - Input Debouncing: Prevents rapid-click bugs.
    /// - Injected Dependencies: Initialized by a Bootstrapper.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MainMenuView _view;

        // Events for the Orchestrator/Bootstrapper
        public event Action OnNewGameRequested;
        public event Action OnContinueRequested;
        public event Action OnQuitRequested;

        private bool _isProcessing = false;

        public void Initialize(string versionText, bool canContinue)
        {
            if (_view == null)
            {
                Debug.LogError($"[MainMenuController] View reference is missing on {gameObject.name}.");
                enabled = false;
                return;
            }

            // Sync visual state once upon initialization
            _view.SetVersionText(versionText);
            _view.SetContinueButtonState(canContinue);
        }

        private void OnEnable()
        {
            if (_view != null)
                _view.OnActionTriggered += HandleMenuAction;
        }

        private void OnDisable()
        {
            if (_view != null)
                _view.OnActionTriggered -= HandleMenuAction;
            
            _isProcessing = false; // Reset lock if the object is disabled
        }

        private void HandleMenuAction(MainMenuAction action)
        {
            if (_isProcessing) return;

            switch (action)
            {
                case MainMenuAction.NewGame:
                    _isProcessing = true;
                    OnNewGameRequested?.Invoke();
                    break;
                case MainMenuAction.Continue:
                    _isProcessing = true;
                    OnContinueRequested?.Invoke();
                    break;
                case MainMenuAction.Quit:
                    _isProcessing = true;
                    OnQuitRequested?.Invoke();
                    break;
            }
        }

        /// <summary>
        /// Allows the Orchestrator to unlock input (e.g., if a quit dialog is cancelled).
        /// </summary>
        public void UnlockInput() => _isProcessing = false;
    }
}
