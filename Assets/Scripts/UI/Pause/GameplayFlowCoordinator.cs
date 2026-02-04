using UnityEngine;
using LastFurrow.UI.Pause;
using LastFurrow.UI.GameOver;

namespace LastFurrow.Flow
{
    /// <summary>
    /// Professional Gameplay Flow Coordinator.
    /// - Orchestrates application-level and scene-level transitions.
    /// - Reactive: Only changes state, doesn't control UI directly.
    /// - Unifies all gameplay-related flow triggers via Controllers.
    /// </summary>
    public class GameplayFlowCoordinator : MonoBehaviour
    {
        [Header("Controllers")]
        [SerializeField] private PauseMenuController _pauseController;
        [SerializeField] private GameOverController _gameOverController;

        private bool _isBusy = false;

        private void Start()
        {
            if (AppCore.Instance == null)
            {
                Debug.LogError("[GameplayFlowCoordinator] AppCore.Instance missing!");
                return;
            }

            var core = AppCore.Instance;

            // Initialize Controllers with dependencies from AppCore
            if (_pauseController != null)
                _pauseController.Initialize(core.GameStateManager);

            if (_gameOverController != null)
                _gameOverController.Initialize(core.GameStateManager, core.Events.Time);
        }

        private void OnEnable()
        {
            // Subscribe to Pause Flows
            if (_pauseController != null)
            {
                _pauseController.OnResumeRequested += HandleResume;
                _pauseController.OnOptionsRequested += HandleOptions;
                _pauseController.OnMainMenuRequested += HandleReturnToMenu;
                _pauseController.OnQuitRequested += HandleQuitToDesktop;
            }

            // Subscribe to Game Over Flows (via Controller now)
            if (_gameOverController != null)
            {
                _gameOverController.OnMainMenuRequested += HandleReturnToMenu;
            }
        }

        private void OnDisable()
        {
            if (_pauseController != null)
            {
                _pauseController.OnResumeRequested -= HandleResume;
                _pauseController.OnOptionsRequested -= HandleOptions;
                _pauseController.OnMainMenuRequested -= HandleReturnToMenu;
                _pauseController.OnQuitRequested -= HandleQuitToDesktop;
            }

            if (_gameOverController != null)
            {
                _gameOverController.OnMainMenuRequested -= HandleReturnToMenu;
            }
            
            _isBusy = false;
        }

        private void HandleResume()
        {
            if (_isBusy) return;

            Debug.Log("[GameplayFlowCoordinator] Flow Rule: User wants to resume. Changing GameState.");
            AppCore.Instance.GameStateManager.SetState(GameState.Playing);
        }

        private void HandleOptions()
        {
            Debug.Log("[GameplayFlowCoordinator] Flow Rule: Opening Options.");
            // Add options flow logic here
        }

        private void HandleReturnToMenu()
        {
            if (_isBusy) return;

            Debug.Log("[GameplayFlowCoordinator] Flow Rule: Returning to Main Menu.");
            _isBusy = true;

            if (AppCore.Instance != null)
            {
                AppCore.Instance.ReturnToMainMenu();
            }
            else
            {
               AbortFlow();
            }
        }

        private void HandleQuitToDesktop()
        {
            if (_isBusy) return;

            Debug.Log("[GameplayFlowCoordinator] Flow Rule: Quitting Game.");
            _isBusy = true;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void AbortFlow()
        {
            _isBusy = false;
            if (_pauseController != null) _pauseController.UnlockInput();
        }
    }
}
