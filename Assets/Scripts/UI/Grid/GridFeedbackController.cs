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
        if (_gridManager == null) return;

        // MUDANÇA: Em vez de acessar SaveManager direto (que expõe tudo), 
        // vamos ser educados e pedir ao GridService (que agora expõe ReadOnly).
        // Se preferir manter acesso ao SaveManager, ok, mas o cast automático ajuda.

        // Vamos usar a via segura se tiver acesso ao GridService, 
        // ou se continuar acessando via AppCore.SaveManager, tudo bem, 
        // mas aqui vamos assumir que você acessa o dado bruto:

        IReadOnlyCropState slotState = AppCore.Instance.GetGridLogic().GetSlotReadOnly(slotIndex);

        // Se você mudou para usar GridService.GetSlotReadOnly:
        // IReadOnlyCropState slotState = AppCore.Instance.GetGridLogic().GetSlotReadOnly(slotIndex);

        Vector3 worldPos = _gridManager.GetSlotPosition(slotIndex);

        // A lógica abaixo funciona tanto para Classe quanto para Interface
        if (slotState.IsWatered)
        {
            PlayFeedback(_waterEffectPrefab, worldPos, _waterSound);
        }
        else if (slotState.CropID.IsValid && slotState.CurrentGrowth == 0)
        {
            PlayFeedback(_plantEffectPrefab, worldPos, _plantSound);
        }
    }

    private void PlayFeedback(ParticleSystem prefab, Vector3 position, string audioName)
    {
        if (prefab != null)
        {
            var particle = Instantiate(prefab, position, Quaternion.identity);
            particle.Play();
            Destroy(particle.gameObject, 2f);
        }

        // if (!string.IsNullOrEmpty(audioName)) AppCore.Instance.AudioManager.PlaySFX(audioName);
    }
}