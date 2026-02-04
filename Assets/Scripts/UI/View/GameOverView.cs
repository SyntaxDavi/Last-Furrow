using System;
using UnityEngine;
using TMPro;

namespace LastFurrow.UI.GameOver
{
    /// <summary>
    /// Professional Pure View for the Game Over Screen.
    /// - Reactive: Only handles visual presentation.
    /// - Communicates intent via events.
    /// </summary>
    public class GameOverView : UIView
    {
        public event Action OnReturnToMenuRequested;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _reasonTitle;
        [SerializeField] private TextMeshProUGUI _detailsText;

        public void Setup(RunEndReason reason)
        {
            switch (reason)
            {
                case RunEndReason.GameOver:
                    SetText("GAME OVER", "Sua jornada terminou aqui.");
                    break;
                case RunEndReason.Victory:
                    SetText("VITÓRIA!", "O ciclo foi completado com sucesso.");
                    break;
                case RunEndReason.Abandoned:
                    SetText("DESISTÊNCIA", "A fazenda foi abandonada (ou contaminação).");
                    break;
            }
        }

        private void SetText(string title, string detail)
        {
            if (_reasonTitle != null) _reasonTitle.text = title;
            if (_detailsText != null) _detailsText.text = detail;
        }

        // Linked to the Button in Unity Inspector
        public void OnMainMenuClicked()
        {
            OnReturnToMenuRequested?.Invoke();
        }
    }
}
