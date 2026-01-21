using Cysharp.Threading.Tasks;
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

    public async UniTask Execute(FlowControl control)
    {
        // REGRA DE OURO: Só dá cartas se for dia de trabalho
        if (_runManager.CurrentPhase == RunPhase.Production)
        {
            Debug.Log("[Step] Iniciando Draw Diário...");
            _handSystem.ProcessDailyDraw(_runData);

            // Tempo para animação "Fan Out" (0.8s = 800ms)
            await UniTask.Delay(800);
        }
        else
        {
            Debug.Log("[Step] Fim de Semana: Sem cartas hoje.");
            // Pequeno respiro (0.5s = 500ms)
            await UniTask.Delay(500);
        }
    }
}