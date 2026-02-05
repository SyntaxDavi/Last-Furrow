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
            Debug.Log($"[WeekendCardDrawStep] Não é necessário dar cartas após shop.");
            return;
        }

        Debug.Log($"[WeekendCardDrawStep] Iniciando Draw após Shop...");
        
        _handSystem.ProcessDailyDraw(_runData);
        
        // FIX: Salvar imediatamente após draw para garantir persistência
        AppCore.Instance?.SaveManager?.SaveGame();

        // Tempo para animação "Fan Out" (0.8s = 800ms)
        await UniTask.Delay(800);
    }
}
