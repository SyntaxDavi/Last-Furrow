using UnityEngine;

public class GridFeedbackController : MonoBehaviour
{
    [Header("Áudio")]
    [SerializeField] private string _plantSound = "PlantSuccess";
    [SerializeField] private string _waterSound = "WaterSplash";

    [Header("Partículas")]
    [SerializeField] private ParticleSystem _plantEffectPrefab;
    [SerializeField] private ParticleSystem _waterEffectPrefab;

    // Dependência Local: Precisamos do GridManager para saber AONDE tocar o efeito (Posição do Slot)
    private GridManager _gridManager;

    // Injeção de Dependência (Feita pelo Bootstrapper)
    public void Configure(GridManager gridManager)
    {
        _gridManager = gridManager;
    }

    private void OnEnable()
    {
        if (AppCore.Instance != null)
        {
            // Escuta eventos globais de notificação
            AppCore.Instance.Events.Grid.OnSlotUpdated += HandleSlotUpdated;
            // AppCore.Instance.Events.OnCardConsumed += HandleCardConsumed; // Se quiser sons de UI
        }
    }

    private void OnDisable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Grid.OnSlotUpdated -= HandleSlotUpdated;
        }
    }

    private void HandleSlotUpdated(int slotIndex)
    {
        // 1. Segurança
        if (_gridManager == null) return;

        // 2. Descobrir o contexto para saber qual som/partícula tocar
        // Como o evento é genérico, precisamos olhar o estado atual do slot para "adivinhar" a ação
        // OU (melhor ainda) criar eventos específicos no futuro.
        // Por enquanto, vamos olhar o estado visual através do GridManager.

        var slotState = AppCore.Instance.SaveManager.Data.CurrentRun.GridSlots[slotIndex];
        Vector3 worldPos = _gridManager.GetSlotPosition(slotIndex);

        // Lógica de Feedback (Apenas cosmética)
        if (slotState.IsWatered)
        {
            // Tocar efeito de água
            PlayFeedback(_waterEffectPrefab, worldPos, _waterSound);
        }
        else if (!string.IsNullOrEmpty(slotState.CropID) && slotState.CurrentGrowth == 0)
        {
            // Se tem planta e crescimento é 0, provavelmente acabou de plantar
            PlayFeedback(_plantEffectPrefab, worldPos, _plantSound);
        }
    }

    private void PlayFeedback(ParticleSystem prefab, Vector3 position, string audioName)
    {
        // Instancia partícula (idealmente usaria um Object Pool, mas Instantiate serve pro MVP)
        if (prefab != null)
        {
            var particle = Instantiate(prefab, position, Quaternion.identity);
            particle.Play();
            Destroy(particle.gameObject, 2f); // Limpeza automática
        }

        // Toca som via AppCore
        if (!string.IsNullOrEmpty(audioName))
        {
            // Assumindo que seu AudioManager é global no AppCore
           //    AppCore.Instance.AudioManager.PlaySFX(audioName);
        }
    }
}