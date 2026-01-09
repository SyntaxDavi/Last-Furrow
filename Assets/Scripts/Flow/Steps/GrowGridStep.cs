using System.Collections;
using UnityEngine;

public class GrowGridStep : IFlowStep
{
    private readonly IGridService _gridService;
    private readonly GameEvents _events;
    private readonly InputManager _input;
    private readonly RunData _runData;

    public GrowGridStep(IGridService gridService, GameEvents events, InputManager input, RunData runData)
    {
        _gridService = gridService;
        _events = events;
        _input = input;
        _runData = runData;
    }

    public IEnumerator Execute()
    {
        for (int i = 0; i < _runData.GridSlots.Length; i++)
        {
            _events.Grid.TriggerAnalyzeSlot(i);
            _gridService.ProcessNightCycleForSlot(i);

            // Ritmo acelerado se segurar botão
            float delay = _input.IsPrimaryButtonHeld ? 0.05f : 0.3f;
            yield return new WaitForSeconds(delay);
        }
        yield return new WaitForSeconds(0.5f);
    }
}