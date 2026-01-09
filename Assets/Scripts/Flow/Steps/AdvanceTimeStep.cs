using System.Collections;
using UnityEngine;

public class AdvanceTimeStep : IFlowStep
{
    private readonly RunManager _runManager;
    private readonly SaveManager _saveManager;

    public AdvanceTimeStep(RunManager runManager, SaveManager saveManager)
    {
        _runManager = runManager;
        _saveManager = saveManager;
    }

    public IEnumerator Execute()
    {
        // 1. Avança o tempo (Dia 1 -> 2)
        _runManager.AdvanceDay();

        // 2. Salva o progresso imediatamente para evitar perda se crashar
        _saveManager.SaveGame();

        Debug.Log("[Step] Dia avançado e jogo salvo.");
        yield return null;
    }
}