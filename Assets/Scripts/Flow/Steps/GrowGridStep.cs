using System.Collections;
using UnityEngine;

public class GrowGridStep : IFlowStep
{
    private readonly IGridService _gridService;
    private readonly GameEvents _events;
    private readonly InputManager _input;
    private readonly RunData _runData;
    
    // HARDCODE: Reference to AnalyzingPhaseController
    private AnalyzingPhaseController _analyzingController;
    private bool _controllerCached;

    public GrowGridStep(IGridService gridService, GameEvents events, InputManager input, RunData runData)
    {
        _gridService = gridService;
        _events = events;
        _input = input;
        _runData = runData;
    }

    public IEnumerator Execute(FlowControl control)
    {
        // HARDCODE: Cachear controller
        if (!_controllerCached)
        {
            _analyzingController = Object.FindFirstObjectByType<AnalyzingPhaseController>();
            _controllerCached = true;
            
            if (_analyzingController != null)
            {
                Debug.Log("[GrowGridStep] ? AnalyzingPhaseController encontrado!");
            }
            else
            {
                Debug.LogError("[GrowGridStep] ? AnalyzingPhaseController NÃO encontrado na cena!");
            }
        }
        
        // HARDCODE: AnalyzingPhaseController faz análise VISUAL primeiro
        if (_analyzingController != null)
        {
            Debug.Log("[GrowGridStep] === FASE 1: ANÁLISE VISUAL ===");
            yield return _analyzingController.AnalyzeAndGrowGrid(_gridService, _events, _runData);
            Debug.Log("[GrowGridStep] === ANÁLISE VISUAL CONCLUÍDA ===");
        }
        
        // FASE 2: CRESCER AS PLANTAS (SEM VISUAL, RÁPIDO)
        Debug.Log("[GrowGridStep] === FASE 2: CRESCIMENTO DAS PLANTAS ===");
        for (int i = 0; i < _runData.GridSlots.Length; i++)
        {
            if (!_gridService.IsSlotUnlocked(i))
            {
                continue;
            }

            Debug.Log($"   ?? Crescendo slot {i}...");
            _gridService.ProcessNightCycleForSlot(i);
        }
        Debug.Log("[GrowGridStep] === CRESCIMENTO CONCLUÍDO ===");
        
        yield return new WaitForSeconds(0.5f);
    }
}