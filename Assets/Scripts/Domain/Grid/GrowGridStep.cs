using Cysharp.Threading.Tasks;
using UnityEngine;

public class GrowGridStep : IFlowStep
{
    private readonly IGridService _gridService;
    private readonly GameEvents _events;
    private readonly InputManager _input;
    private readonly RunData _runData;

    // Dependência Visual Injetada (pode ser null)
    private readonly AnalyzingPhaseController _visualController;

    public GrowGridStep(
        IGridService gridService,
        GameEvents events,
        InputManager input,
        RunData runData,
        AnalyzingPhaseController visualController)
    {
        _gridService = gridService;
        _events = events;
        _input = input;
        _runData = runData;
        _visualController = visualController;
    }

    public async UniTask Execute(FlowControl control)
    {
        // Lógica simplificada: Verifica se a dependência existe
        if (_visualController != null)
        {
            Debug.Log("[GrowGridStep] Usando AnalyzingPhaseController para animação.");

            // Aguarda a análise visual. 
            // NOTA: O método AnalyzeAndGrowGrid no controller também deve ser convertido para 'async UniTask'
            await _visualController.AnalyzeAndGrowGrid(_gridService, _events, _runData);
        }
        else
        {
            // FALLBACK: Crescer sem visual (Modo Headless ou Segurança)
            Debug.LogWarning("[GrowGridStep] AnalyzingPhaseController não injetado! Executando lógica sem animação.");

            for (int i = 0; i < _runData.GridSlots.Length; i++)
            {
                if (!_gridService.IsSlotUnlocked(i)) continue;
                _gridService.ProcessNightCycleForSlot(i);
            }
        }

        // Delay de 500ms (0.5s)
        await UniTask.Delay(500);
    }
}