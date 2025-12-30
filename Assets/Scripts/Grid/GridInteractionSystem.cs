using UnityEngine;

public class GridInteractionSystem : MonoBehaviour
{
    private void Start()
    {
        if (AppCore.Instance != null)
            AppCore.Instance.Events.OnCardDroppedOnSlot += HandleCardDrop;
    }

    private void OnDestroy()
    {
        if (AppCore.Instance != null)
            AppCore.Instance.Events.OnCardDroppedOnSlot -= HandleCardDrop;
    }

    private void HandleCardDrop(int slotIndex, CardData cardData)
    {
        var saveManager = AppCore.Instance.SaveManager;
        var runData = saveManager.Data.CurrentRun;

        // Validação de Limites
        if (slotIndex < 0 || slotIndex >= runData.GridSlots.Length) return;

        var slotState = runData.GridSlots[slotIndex];
        bool operationSuccess = false;

        // --- ROTEAMENTO DE LÓGICA (Precursor do Strategy Pattern) ---
        // Aqui isolamos a lógica por tipo.
        if (cardData.Type == CardType.Plant)
        {
            operationSuccess = TryPlant(slotState, cardData);
        }
        else if (cardData.Type == CardType.Modify)
        {
            operationSuccess = TryModify(slotState, cardData);
        }

        // --- CONCLUSÃO ---
        if (operationSuccess)
        {
            // 1. Remove carta do Deck (Dados)
            runData.DeckIDs.Remove(cardData.ID); 

            // 2. Salva
            saveManager.SaveGame();

            // 3. Notifica HandManager para atualizar UI da mão
            AppCore.Instance.Events.TriggerCardConsumed(cardData.ID);

            // 4. Notifica GridManager para atualizar UI deste slot específico
            AppCore.Instance.Events.TriggerGridSlotUpdated(slotIndex);
        }
    }

    // Lógica pura de Plantação
    private bool TryPlant(CropState slot, CardData card)
    {
        if (!string.IsNullOrEmpty(slot.CropID)) return false; 

        slot.CropID = card.CropToPlant.ID;
        slot.CurrentGrowth = 0;
        slot.IsWatered = false;
        slot.IsWithered = false;
        return true;
    }

    // Lógica pura de Modificação
    private bool TryModify(CropState slot, CardData card)
    {
        if (string.IsNullOrEmpty(slot.CropID)) return false; // Vazio

        if (card.ID == "card_water")
        {
            if (slot.IsWatered) return false; // Já regado?
            slot.IsWatered = true;
            return true;
        }
        // Futuro: Adubos, etc.
        return false;
    }
}