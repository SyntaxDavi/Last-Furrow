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
        // HARDCODE: Chamar AnalyzingPhaseController ANTES de processar
        if (!_controllerCached)
        {
            _analyzingController = Object.FindFirstObjectByType<AnalyzingPhaseController>();
            _controllerCached = true;
            
            if (_analyzingController != null)
            {
                Debug.Log("[GrowGridStep] AnalyzingPhaseController encontrado!");
            }
            else
            {
                Debug.LogWarning("[GrowGridStep] AnalyzingPhaseController NÃO encontrado na cena!");
            }
        }
        
        // EXECUTAR ANÁLISE HARDCODE
        if (_analyzingController != null)
        {
            Debug.Log("[GrowGridStep] === CHAMANDO ANALYZING PHASE (HARDCODE) ===");
            yield return _analyzingController.AnalyzeGridHardcoded();
            Debug.Log("[GrowGridStep] === ANALYZING PHASE CONCLUÍDA ===");
        }
        
        // Processar crescimento normal (rápido, sem visual)
        for (int i = 0; i < _runData.GridSlots.Length; i++)
        {
            if (!_gridService.IsSlotUnlocked(i))
            {
                continue; // Pula para próximo slot
            }

            _events.Grid.TriggerAnalyzeSlot(i);
            _gridService.ProcessNightCycleForSlot(i);

            // Ritmo acelerado se segurar botão
            float delay = _input.IsPrimaryButtonHeld ? 0.05f : 0.3f;
            yield return new WaitForSeconds(delay);
        }
        yield return new WaitForSeconds(0.5f);
    }
}