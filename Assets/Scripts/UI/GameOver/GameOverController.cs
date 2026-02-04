using System;
using UnityEngine;

namespace LastFurrow.UI.GameOver
{
    /// <summary>
    /// Professional Presenter for the Game Over Screen.
    /// - Reactive: Observes GameState and TimeEvents to Setup/Show the View.
    /// - Decoupled: Translates data from events to the View.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameOverView _view;

        public event Action OnMainMenuRequested;

        private IGameStateProvider _stateProvider;
        private RunEndReason _lastReason = RunEndReason.GameOver;

        public void Initialize(IGameStateProvider stateProvider, TimeEvents timeEvents)
        {
            _stateProvider = stateProvider;

            if (_view == null)
            {
                Debug.LogError($"[GameOverController] View is missing on {gameObject.name}");
                enabled = false;
                return;
            }

            // Unsubscribe just in case
            if (timeEvents != null)
            {
                timeEvents.OnRunEnded -= HandleRunEnded;
                timeEvents.OnRunEnded += HandleRunEnded;
            }

            if (_stateProvider != null)
            {
                _stateProvider.OnStateChanged -= HandleStateChanged;
                _stateProvider.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnEnable()
        {
            if (_view != null)
            {
                _view.OnReturnToMenuRequested += HandleReturnRequested;
            }
        }

        private void OnDisable()
        {
            if (_view != null)
            {
                _view.OnReturnToMenuRequested -= HandleReturnRequested;
            }

            // Provider cleanup is usually better in Initialize or OnDestroy if initialized via code
            // But for safety in Unity lifecycle:
            if (_stateProvider != null)
                _stateProvider.OnStateChanged -= HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (_stateProvider != null)
                _stateProvider.OnStateChanged -= HandleStateChanged;

            if (AppCore.Instance != null && AppCore.Instance.Events?.Time != null)
                AppCore.Instance.Events.Time.OnRunEnded -= HandleRunEnded;
        }

        private void HandleRunEnded(RunEndReason reason)
        {
            _lastReason = reason;
        }

        private void HandleStateChanged(GameState newState)
        {
            if (newState == GameState.GameOver)
            {
                _view.Setup(_lastReason);
                _view.Show();
            }
            else if (_view.gameObject.activeSelf)
            {
                _view.Hide();
            }
        }

        private void HandleReturnRequested()
        {
            OnMainMenuRequested?.Invoke();
        }
    }
}
