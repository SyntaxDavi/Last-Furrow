using System.Collections;
using UnityEngine;

public class DailyDrawStep : IFlowStep
{
    private readonly DailyHandSystem _handSystem;
    private readonly RunManager _runManager;
    private readonly RunData _runData;

    public DailyDrawStep(DailyHandSystem handSystem, RunManager runManager, RunData runData)
    {
        _handSystem = handSystem;
        _runManager = runManager;
        _runData = runData;
    }

    public IEnumerator Execute()
    {
        // REGRA DE OURO: Só dá cartas se for dia de trabalho
        if (_runManager.CurrentPhase == RunPhase.Production)
        {
            Debug.Log("[Step] Iniciando Draw Diário...");
            _handSystem.ProcessDailyDraw(_runData);

            // Tempo para animação "Fan Out"
            yield return new WaitForSeconds(0.8f);
        }
        else
        {
            Debug.Log("[Step] Fim de Semana: Sem cartas hoje.");
            // Pequeno respiro
            yield return new WaitForSeconds(0.5f);
        }
    }
}