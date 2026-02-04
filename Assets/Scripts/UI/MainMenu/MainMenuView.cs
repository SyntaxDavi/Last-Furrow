using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LastFurrow.UI.MainMenu
{
    /// <summary>
    /// Pure View for the Main Menu.
    /// Handles visual updates and exposes events through explicit methods.
    /// </summary>
    public class MainMenuView : UIView
    {
        public event Action<MainMenuAction> OnActionTriggered;

        [Header("Button References")]
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _quitButton;

        [Header("Text References")]
        [SerializeField] private TextMeshProUGUI _versionText;

        private void OnEnable()
        {
            if (_newGameButton != null) _newGameButton.onClick.AddListener(OnNewGameClicked);
            if (_continueButton != null) _continueButton.onClick.AddListener(OnContinueClicked);
            if (_quitButton != null) _quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void OnDisable()
        {
            if (_newGameButton != null) _newGameButton.onClick.RemoveListener(OnNewGameClicked);
            if (_continueButton != null) _continueButton.onClick.RemoveListener(OnContinueClicked);
            if (_quitButton != null) _quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        public void SetVersionText(string versionText)
        {
            if (_versionText != null) _versionText.text = versionText;
        }

        public void SetContinueButtonState(bool isAvailable)
        {
            if (_continueButton != null) _continueButton.interactable = isAvailable;
        }

        private void OnNewGameClicked() => OnActionTriggered?.Invoke(MainMenuAction.NewGame);
        private void OnContinueClicked() => OnActionTriggered?.Invoke(MainMenuAction.Continue);
        private void OnQuitClicked() => OnActionTriggered?.Invoke(MainMenuAction.Quit);
    }
}
