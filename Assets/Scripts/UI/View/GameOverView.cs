using UnityEngine;
using TMPro;

public class GameOverView : UIView
{
    [Header("Referências UI")]
    [SerializeField] private TextMeshProUGUI _reasonTitle;
    [SerializeField] private TextMeshProUGUI _detailsText;

    public void Setup(RunEndReason reason)
    {
        switch (reason)
        {
            case RunEndReason.Abandoned:
                SetText("DESISTÊNCIA", "A fazenda foi abandonada.");
                break;
            case RunEndReason.HarvestFailed:
                SetText("FALÊNCIA", "Acúmulo de falhas econômicas.");
                break;
            case RunEndReason.WitheredOverload:
                SetText("COLAPSO ECOLÓGICO", "O solo está morto (80%+ de contaminação).");
                break;
            case RunEndReason.Victory:
                SetText("VITÓRIA!", "O ciclo foi completado com sucesso.");
                break;
        }
    }

    private void SetText(string title, string detail)
    {
        _reasonTitle.text = title;
        _detailsText.text = detail;
    }

    // Vinculado ao botão "Voltar ao Menu" na Unity
    public void OnMainMenuClicked()
    {
        AppCore.Instance.ReturnToMainMenu();
    }
}