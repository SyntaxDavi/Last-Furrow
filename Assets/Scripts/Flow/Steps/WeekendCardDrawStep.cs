using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// SOLID: Single Responsibility - Apenas executa o draw de cartas após o shop (semana 2+).
/// Este step é executado no pipeline de saída do weekend, após o shop.
/// </summary>
public class WeekendCardDrawStep : IFlowStep
{
    private readonly DailyHandSystem _handSystem;
    private readonly RunData _runData;
    private readonly CardDrawPolicy _drawPolicy;
    
    public string Name => "Weekend Card Draw";
    
    public WeekendCardDrawStep(
        DailyHandSystem handSystem, 
        RunData runData,
        CardDrawPolicy drawPolicy)
    {
        _handSystem = handSystem;
        _runData = runData;
        _drawPolicy = drawPolicy ?? new CardDrawPolicy();
    }

    public async UniTask Execute(FlowControl control)
    {
        if (_handSystem == null || _runData == null)
        {
            Debug.LogError("[WeekendCardDrawStep] Dependências nulas. Pulando step.");
            return;
        }

        // Verifica se deve dar cartas após o shop (semana 2+)
        if (!_drawPolicy.ShouldDrawCardsAfterShop(_runData))
        {
            Debug.Log($"[WeekendCardDrawStep] Não é necessário dar cartas após shop (Semana {_runData.CurrentWeek}, Dia {_runData.CurrentDay}).");
            return;
        }

        // PROTEÇÃO: Se já deu draw hoje, não dá de novo
        if (_runData.HasDrawnDailyHand)
        {
            Debug.LogWarning("[WeekendCardDrawStep] Cartas já foram distribuídas hoje. Pulando draw para evitar duplicação.");
            return;
        }

        Debug.Log($"[WeekendCardDrawStep] Iniciando Draw após Shop (Dia {_runData.CurrentDay}, Semana {_runData.CurrentWeek})...");
        
        // Marca que as cartas foram distribuídas ANTES do draw (idempotência)
        _runData.HasDrawnDailyHand = true;
        
        _handSystem.ProcessDailyDraw(_runData);

        // Tempo para animação "Fan Out" (0.8s = 800ms)
        await UniTask.Delay(800);
    }
}
