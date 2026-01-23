using Cysharp.Threading.Tasks;
using UnityEngine;

public class DailyDrawStep : IFlowStep
{
    private readonly DailyHandSystem _handSystem;
    private readonly RunManager _runManager;
    private readonly RunData _runData;
    public string Name => "Daily Draw";
    public DailyDrawStep(DailyHandSystem handSystem, RunManager runManager, RunData runData)
    {
        _handSystem = handSystem;
        _runManager = runManager;
        _runData = runData;
    }

    public async UniTask Execute(FlowControl control)
    {
        if (_handSystem == null || _runData == null)
        {
            Debug.LogError("[DailyDrawStep] Dependências nulas. Pulando step.");
            return;
        }

        // PROTEÇÃO: Se já deu draw hoje, não dá de novo
        if (_runData.HasDrawnDailyHand)
        {
            Debug.LogWarning("[DailyDrawStep] Cartas já foram distribuídas hoje. Pulando draw para evitar duplicação.");
            return;
        }

        // REGRA DE OURO: Só dá cartas se for dia de trabalho
        if (_runManager.CurrentPhase == RunPhase.Production)
        {
            Debug.Log("[Step] Iniciando Draw Diário...");
            
            // Marca que as cartas foram distribuídas ANTES do draw
            _runData.HasDrawnDailyHand = true;
            
            _handSystem.ProcessDailyDraw(_runData);

            // Tempo para animação "Fan Out" (0.8s = 800ms)
            await UniTask.Delay(800);
        }
        else
        {
            Debug.Log("[Step] Fim de Semana: Sem cartas hoje.");
            // Marca como "drawn" mesmo que não tenha dado cartas
            // para evitar re-execução
            _runData.HasDrawnDailyHand = true;
            
            // Pequeno respiro (0.5s = 500ms)
            await UniTask.Delay(500);
        }
    }
}