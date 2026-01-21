using System.Collections;
using UnityEngine;

public class GrowGridStep : IFlowStep
{
    private readonly IGridService _gridService;
    private readonly GameEvents _events;
    private readonly InputManager _input;
    private readonly RunData _runData;
    
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
        // FASE 1: ANÁLISE VISUAL
        if (!_controllerCached)
        {
            _analyzingController = Object.FindFirstObjectByType<AnalyzingPhaseController>();
            _controllerCached = true;
            
            if (_analyzingController == null)
            {
                Debug.LogWarning("[GrowGridStep] AnalyzingPhaseController not found! Skipping visual analysis.");
            }
            else
            {
                Debug.Log("[GrowGridStep] AnalyzingPhaseController found!");
            }
        }
        
        if (_analyzingController != null)
        {
            yield return _analyzingController.AnalyzeAndGrowGrid(_gridService, _events, _runData);
        }
        else
        {
            // Fallback: crescer sem visual
            Debug.Log("[GrowGridStep] Growing plants without visual analysis...");
            for (int i = 0; i < _runData.GridSlots.Length; i++)
            {
                if (!_gridService.IsSlotUnlocked(i)) continue;
                _gridService.ProcessNightCycleForSlot(i);
            }
        }
        
        yield return new WaitForSeconds(0.5f);
    }
}