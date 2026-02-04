using System;
using UnityEngine;

namespace LastFurrow.UI.Pause
{
    /// <summary>
    /// Professional Presenter for the Pause Menu.
    /// - Reactive: Observes GameState to Show/Hide the View.
    /// - Decoupled: No direct logic, just translation of requests.
    /// - Input Debouncing: Prevents rapid-click issues.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PauseMenuView _view;

        public event Action OnResumeRequested;
        public event Action OnOptionsRequested;
        public event Action OnMainMenuRequested;
        public event Action OnQuitRequested;

        private IGameStateProvider _stateProvider;
        private bool _isProcessing = false;

        public void Initialize(IGameStateProvider stateProvider)
        {
            _stateProvider = stateProvider;
            
            if (_view == null)
            {
                Debug.LogError($"[PauseMenuController] View is missing on {gameObject.name}");
                enabled = false;
                return;
            }

            // Unsubscribe just in case of re-initialization
            _stateProvider.OnStateChanged -= HandleStateChanged;
            _stateProvider.OnStateChanged += HandleStateChanged;
        }

        private void OnEnable()
        {
            if (_view != null)
            {
                _view.OnResumeClicked += HandleResumeClicked;
                _view.OnOptionsClicked += HandleOptionsClicked;
                _view.OnMainMenuClicked += HandleMainMenuClicked;
                _view.OnQuitDesktopClicked += HandleQuitClicked;
            }

            // If we already have a provider, ensure we are synced
            if (_stateProvider != null)
                HandleStateChanged(_stateProvider.CurrentState);
        }

        private void OnDisable()
        {
            if (_view != null)
            {
                _view.OnResumeClicked -= HandleResumeClicked;
                _view.OnOptionsClicked -= HandleOptionsClicked;
                _view.OnMainMenuClicked -= HandleMainMenuClicked;
                _view.OnQuitDesktopClicked -= HandleQuitClicked;
            }

            if (_stateProvider != null)
                _stateProvider.OnStateChanged -= HandleStateChanged;

            _isProcessing = false;
        }

        private void HandleStateChanged(GameState newState)
        {
            if (newState == GameState.Paused)
            {
                _view.Show();
                _isProcessing = false; // Reset lock when menu opens
            }
            else if (_view.gameObject.activeSelf)
            {
                _view.Hide();
            }
        }

        private void HandleResumeClicked()
        {
            if (_isProcessing) return;
            _isProcessing = true;
            OnResumeRequested?.Invoke();
        }

        private void HandleOptionsClicked()
        {
            OnOptionsRequested?.Invoke();
        }

        private void HandleMainMenuClicked()
        {
            if (_isProcessing) return;
            _isProcessing = true;
            OnMainMenuRequested?.Invoke();
        }

        private void HandleQuitClicked()
        {
            if (_isProcessing) return;
            _isProcessing = true;
            OnQuitRequested?.Invoke();
        }

        public void UnlockInput() => _isProcessing = false;
    }
}
