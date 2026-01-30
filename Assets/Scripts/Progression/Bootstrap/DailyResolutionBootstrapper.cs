using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// Bootstrapper responsável por inicializar o DailyResolutionSystem na cena.
/// Elimina a necessidade de injeção manual e delays de frame (race conditions) no AppCore.
/// </summary>
public class DailyResolutionBootstrapper : MonoBehaviour
{
    [Header("System to Initialize")]
    [SerializeField] private DailyResolutionSystem _resolutionSystem;

    [Header("Visual Source")]
    [SerializeField] private DailyVisualBootstrapper _visualBootstrapper;

    private void Start()
    {
        InitializeAsync().Forget();
    }

    private async UniTaskVoid InitializeAsync()
    {
        // 1. Aguarda as dependências globais estarem prontas
        await UniTask.WaitUntil(() => AppCore.Instance != null && AppCore.Instance.GridService != null);
        
        // 2. Aguarda a Run estar ativa
        await UniTask.WaitUntil(() => AppCore.Instance.SaveManager?.Data?.CurrentRun != null);

        var runData = AppCore.Instance.SaveManager.Data.CurrentRun;

        // 3. Garante que o PatternTracking esteja inicializado
        if (AppCore.Instance.PatternTracking == null)
        {
            AppCore.Instance.InitializePatternTracking(runData);
        }

        // 4. Coleta contextos
        var logicContext = CreateLogicContext();
        var visualContext = _visualBootstrapper != null ? _visualBootstrapper.CreateContext() : null;
        var pipelineBuilder = new DailyPipelineBuilder();

        // 5. Injeta no sistema
        if (_resolutionSystem != null)
        {
            _resolutionSystem.Construct(logicContext, visualContext, pipelineBuilder);
            
            // 6. Registra no AppCore para acesso global (ex: RunManager terminar o dia)
            AppCore.Instance.RegisterDailyResolutionSystem(_resolutionSystem);
            
            Debug.Log("[DailyResolutionBootstrapper] ✓ Sistema inicializado e registrado com sucesso.");
        }
        else
        {
            Debug.LogError("[DailyResolutionBootstrapper] ✗ Falha: DailyResolutionSystem não atribuído!");
        }
    }

    private DailyResolutionContext CreateLogicContext()
    {
        var app = AppCore.Instance;
        return new DailyResolutionContext(
            app.RunManager,
            app.SaveManager,
            app.InputManager,
            app.Events,
            app.DailyHandSystem,
            app.WeeklyGoalSystem,
            app.GridService,
            app.PatternDetector,
            app.PatternTracking,
            app.PatternCalculator,
            app.Services.Traditions
        );
    }
}
