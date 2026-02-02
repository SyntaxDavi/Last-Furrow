using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Step que processa o crescimento do grid (Ciclo Noturno).
/// Agora é puramente lógico (Headless) para permitir que a contagem seja contínua no DetectPatternsStep.
/// </summary>
public class GrowGridStep : IFlowStep
{
    private readonly IGridService _gridService;
    private readonly GameEvents _events;
    private readonly RunData _runData;
    private readonly DayAnalysisResult _analysisResult;

    public GrowGridStep(
        IGridService gridService,
        GameEvents events,
        InputManager inputManager,
        RunData runData,
        AnalyzingPhaseOrchestrator visualController, // Mantido na assinatura para não quebrar o builder ainda
        DayAnalysisResult analysisResult = null)
    {
        _gridService = gridService;
        _events = events;
        _runData = runData;
        _analysisResult = analysisResult;
    }

    public async UniTask Execute(FlowControl control)
    {
        Debug.Log("[GrowGridStep] Processando ciclo noturno (Headless)");

        // 1. Processa Lógica de Crescimento
        for (int i = 0; i < _runData.GridSlots.Length; i++)
        {
            if (!_gridService.IsSlotUnlocked(i)) continue;
            
            // PROCESSAMENTO SILENCIOSO: A lógica roda, mas não troca o sprite agora.
            _gridService.ProcessNightCycleForSlot(i, out _, silent: true);    

            // 2. Pré-calcula pontos passivos para a fase visual posterior
            if (_analysisResult != null)
            {
                int points = ScoringCalculator.CalculatePassiveScore(_runData.GridSlots[i], AppCore.Instance.GameLibrary);
                if (points > 0)
                {
                    _analysisResult.AddPassiveScore(i, points);
                }
            }
        }

        await UniTask.Yield();
    }
}
