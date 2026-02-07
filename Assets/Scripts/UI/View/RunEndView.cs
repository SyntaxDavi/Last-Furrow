using System;
using UnityEngine;
using TMPro;

namespace LastFurrow.UI.RunEnd
{
    /// <summary>
    /// Professional Pure View for the Run End Screen (Victory or Game Over).
    /// - Reactive: Only handles visual presentation.
    /// - Communicates intent via events.
    /// </summary>
    public class RunEndView : UIView
    {
        public event Action OnReturnToMenuRequested;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _reasonTitle;
        [SerializeField] private TextMeshProUGUI _detailsText;

        public void Setup(RunEndReason reason)
        {
            switch (reason)
            {
                case RunEndReason.HarvestFailed:
                case RunEndReason.WitheredOverload:
                    SetText("GAME OVER", "A fazenda não resistiu aos desafios.");
                    break;
                case RunEndReason.Victory:
                    SetText("VITÓRIA!", "O ciclo foi completado com sucesso.");
                    break;
                case RunEndReason.Abandoned:
                    SetText("DESISTÊNCIA", "A fazenda foi abandonada.");
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
