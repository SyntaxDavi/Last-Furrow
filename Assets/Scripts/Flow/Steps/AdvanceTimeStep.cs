using Cysharp.Threading.Tasks;
using UnityEngine;

public class AdvanceTimeStep : IFlowStep
{
    private readonly RunManager _runManager;
    private readonly SaveManager _saveManager;
    private readonly RunData _runData;
    
    public string Name => "Advance Time";
    
    public AdvanceTimeStep(RunManager runManager, SaveManager saveManager, RunData runData)
    {
        _runManager = runManager;
        _saveManager = saveManager;
        _runData = runData;
    }

    public async UniTask Execute(FlowControl control)
    {
        // 1. Avança o tempo (Dia 1 -> 2)
        _runManager.AdvanceDay();

        // 2. CRÍTICO: Reseta a flag de draw diário para permitir o próximo draw
        // Isso evita duplicação de cartas caso o pipeline seja executado múltiplas vezes
        _runData.HasDrawnDailyHand = false;

        // 3. Salva o progresso imediatamente para evitar perda se crashar
        _saveManager.SaveGame();

        Debug.Log("[Step] Dia avançado e jogo salvo.");

        // Espera 1 frame para garantir que outros sistemas reajam à mudança de dia
        await UniTask.Yield();
    }
}