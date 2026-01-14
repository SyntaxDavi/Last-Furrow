using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla o botão UI para embaralhar as cartas da mão.
/// O sistema de reorganização manual via drag and drop já funciona nativamente.
/// </summary>
public class HandOrganizerUIController : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Button _shuffleButton;
    [SerializeField] private HandManager _handManager;

    // ==============================================================================================
    // INICIALIZAÇÃO
    // ==============================================================================================

    private void Start()
    {
        // Valida referências
        if (_shuffleButton == null || _handManager == null)
        {
            Debug.LogError("[HandOrganizerUIController] Referências não configuradas no Inspector!");
            enabled = false;
            return;
        }

        // Conecta o botão ao evento de clique
        _shuffleButton.onClick.AddListener(OnShuffleButtonClicked);
    }

    private void OnDestroy()
    {
        if (_shuffleButton != null)
            _shuffleButton.onClick.RemoveListener(OnShuffleButtonClicked);
    }

    // ==============================================================================================
    // LÓGICA PRINCIPAL
    // ==============================================================================================

    /// <summary>
    /// Embaralha as cartas da mão quando o botão é clicado
    /// </summary>
    public void OnShuffleButtonClicked()
    {
        var organizer = _handManager.GetOrganizer();
        if (organizer != null)
        {
            organizer.Shuffle();
            Debug.Log("[HandOrganizer] Cartas embaralhadas!");
        }
    }
}
