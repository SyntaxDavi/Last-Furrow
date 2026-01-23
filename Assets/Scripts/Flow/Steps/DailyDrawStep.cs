using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// SOLID: Single Responsibility - Apenas executa o draw di�rio seguindo a pol�tica.
/// </summary>
public class DailyDrawStep : IFlowStep
{
    private readonly DailyHandSystem _handSystem;
    private readonly RunManager _runManager;
    private readonly RunData _runData;
    private readonly CardDrawPolicy _drawPolicy;
    
    public string Name => "Daily Draw";
    
    public DailyDrawStep(
        DailyHandSystem handSystem, 
        RunManager runManager, 
        RunData runData,
        CardDrawPolicy drawPolicy)
    {
        _handSystem = handSystem;
        _runManager = runManager;
        _runData = runData;
        _drawPolicy = drawPolicy ?? new CardDrawPolicy();
    }

    public async UniTask Execute(FlowControl control)
    {
        if (_handSystem == null || _runData == null)
        {
            Debug.LogError("[DailyDrawStep] Depend�ncias nulas. Pulando step.");
            return;
        }

        // PROTE��O: Se j� deu draw hoje, n�o d� de novo
        if (_runData.HasDrawnDailyHand)
        {
            Debug.LogWarning("[DailyDrawStep] Cartas j� foram distribu�das hoje. Pulando draw para evitar duplica��o.");
            return;
        }

        Debug.Log($"[DailyDrawStep] Estado inicial: HasDrawnDailyHand={_runData.HasDrawnDailyHand}");

        // SOLID: Usa pol�tica para decidir se deve dar cartas
        bool shouldDraw = _drawPolicy.ShouldDrawCards(_runData, _runManager.CurrentPhase);
        Debug.Log($"[DailyDrawStep] ShouldDrawCards retornou: {shouldDraw}");
        
        if (shouldDraw)
        {
            Debug.Log($"[DailyDrawStep] Iniciando Draw Di�rio (Dia {_runData.CurrentDay}, Semana {_runData.CurrentWeek})...");
            
            _handSystem.ProcessDailyDraw(_runData);
            AppCore.Instance.SaveManager.SaveGame();
            // Tempo para anima��o "Fan Out" (0.8s = 800ms)
            await UniTask.Delay(800);
        }
        else
        {
            Debug.Log($"[DailyDrawStep] Sem cartas hoje (Dia {_runData.CurrentDay}, Fase: {_runManager.CurrentPhase}).");
            // Marca como "drawn" mesmo que n�o tenha dado cartas
            // para evitar re-execu��o
            _runData.HasDrawnDailyHand = true;
            
            // Pequeno respiro (0.5s = 500ms)
            await UniTask.Delay(500);
        }
    }
}