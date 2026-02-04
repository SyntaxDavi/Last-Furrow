using UnityEngine;

namespace LastFurrow.UI.MainMenu
{
    /// <summary>
    /// Professional Flow Coordinator for the Main Menu.
    /// - Orchestrates the high-level transition sequences.
    /// - Manages scene-level lifecycle and dependency resolution.
    /// - Flow Guard: Prevents concurrent navigation requests.
    /// </summary>
    public class MainMenuFlowCoordinator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MainMenuController _controller;

        private bool _isBusy = false;

        private void OnEnable()
        {
            if (_controller != null)
            {
                _controller.OnNewGameRequested += HandleNewGameRequest;
                _controller.OnContinueRequested += HandleContinueRequest;
                _controller.OnQuitRequested += HandleQuitRequest;
            }
        }

        private void OnDisable()
        {
            if (_controller != null)
            {
                _controller.OnNewGameRequested -= HandleNewGameRequest;
                _controller.OnContinueRequested -= HandleContinueRequest;
                _controller.OnQuitRequested -= HandleQuitRequest;
            }
            
            _isBusy = false;
        }

        private void Start()
        {
            SetupMainMenuView();
        }

        private void SetupMainMenuView()
        {
            // Resolve dependencies from AppCore (Acting as Service Locator)
            if (AppCore.Instance == null)
            {
                Debug.LogError("[MainMenuFlowCoordinator] AppCore.Instance missing! Boot sequence incomplete.");
                return;
            }

            var runManager = AppCore.Instance.RunManager;
            string versionText = $"v{Application.version}";
            bool canContinue = runManager != null && runManager.IsRunActive;

            _controller.Initialize(versionText, canContinue);
            _controller.Show();
        }

        private void HandleNewGameRequest()
        {
            if (_isBusy) return;
            
            Debug.Log("[MainMenuFlowCoordinator] Executing NEW GAME flow...");
            _isBusy = true;

            // Scenario for future: Add Fade-out / Analytics / Save Slot Selection here
            
            if (AppCore.Instance != null && AppCore.Instance.RunManager != null)
            {
                AppCore.Instance.RunManager.StartNewRun();
                AppCore.Instance.LoadGameplay();
            }
            else
            {
                AbortFlow();
            }
        }

        private void HandleContinueRequest()
        {
            if (_isBusy) return;

            Debug.Log("[MainMenuFlowCoordinator] Executing CONTINUE flow...");
            _isBusy = true;

            if (AppCore.Instance != null)
            {
                AppCore.Instance.LoadGameplay();
            }
            else
            {
                AbortFlow();
            }
        }

        private void HandleQuitRequest()
        {
            if (_isBusy) return;

            Debug.Log("[MainMenuFlowCoordinator] Executing QUIT flow...");
            _isBusy = true;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void AbortFlow()
        {
            Debug.LogWarning("[MainMenuFlowCoordinator] Navigation flow aborted due to missing dependencies.");
            _isBusy = false;
            _controller.UnlockInput();
        }
    }
}
