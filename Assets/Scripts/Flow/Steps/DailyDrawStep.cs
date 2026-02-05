using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// SOLID: Single Responsibility - Apenas executa o draw diário seguindo a política.
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
            Debug.LogError("[DailyDrawStep] Dependências nulas. Pulando step.");
            return;
        }

        // PROTEÇÃO: Se já deu draw hoje, não dá de novo. 
        // Agora HasDrawnDailyHand é uma propriedade computada robusta em RunData.
        if (_runData.HasDrawnDailyHand)
        {
            Debug.LogWarning("[DailyDrawStep] Cartas já foram distribuídas hoje. Pulando draw para evitar duplicação.");
            return;
        }

        Debug.Log($"[DailyDrawStep] Verificando política de Draw (Dia {_runData.CurrentDay}, Semana {_runData.CurrentWeek})");

        // SOLID: Usa política para decidir se deve dar cartas
        bool shouldDraw = _drawPolicy.ShouldDrawCards(_runData, _runManager.CurrentPhase);
        
        if (shouldDraw)
        {
            Debug.Log($"[DailyDrawStep] Iniciando Draw Diário...");
            
            _handSystem.ProcessDailyDraw(_runData);
            AppCore.Instance.SaveManager.SaveGame(); 
            
            // Tempo para animação "Fan Out" (0.8s = 800ms)
            await UniTask.Delay(800);
        }
        else
        {
            Debug.Log($"[DailyDrawStep] Sem cartas hoje (Fase: {_runManager.CurrentPhase}). Marcando dia como processado.");
            // Marcamos o dia atual como "processado" mesmo que não tenha dado cartas
            // para que o DailyDrawStep não tente rodar de novo em loops de animação ou recarregamento
            _runData.LastDrawnDay = _runData.CurrentDay;
            _runData.LastDrawnWeek = _runData.CurrentWeek;
            
            // Pequeno respiro
            await UniTask.Delay(500);
        }
    }
}