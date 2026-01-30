using UnityEngine;

/// <summary>
/// Bootstrapper Pattern: Centraliza referencias visuais da scene.
/// Elimina multiplos FindFirstObjectByType no AppCore.
///
/// SETUP:
/// 1. Adicione este componente em um GameObject na scene "Game"
/// 2. Arraste as referencias no Inspector
/// 3. AppCore busca APENAS este bootstrapper (1x ao inves de 3x)
///
/// BENEFICIOS:
/// - Performance: 1 busca ao inves de 3
/// - Validacao: Erros claros no Inspector se faltar algo
/// - Manutenibilidade: Todas referencias visuais em 1 lugar
/// </summary>
public class DailyVisualBootstrapper : MonoBehaviour
{
    [Header("Visual Controllers (Arraste da Scene)")]
    [SerializeField] private AnalyzingPhaseOrchestrator _analyzer;
    [SerializeField] private GridSlotScanner _scanner;
    [SerializeField] private PatternUIManager _uiManager;
    [SerializeField] private HandManager _handManager;

    [Header("Debug")]
    [SerializeField] private bool _validateOnAwake = true;

    private void Awake()
    {
        if (_validateOnAwake)
        {
            ValidateReferences();
        }
    }

    /// <summary>
    /// Cria contexto visual com todas as dependencias.
    /// Chamado pelo AppCore apos scene load.
    /// </summary>
    public DailyVisualContext CreateContext()
    {
        if (!ValidateReferences())
        {
            Debug.LogError("[DailyVisualBootstrapper] Nao e possivel criar contexto - referencias invalidas!");
            return null;
        }

        Debug.Log("[DailyVisualBootstrapper] Contexto visual criado com sucesso");
        return new DailyVisualContext(_analyzer, _scanner, _uiManager, _handManager);
    }

    /// <summary>
    /// Valida se todas as referencias foram atribuidas no Inspector.
    /// </summary>
    private bool ValidateReferences()
    {
        bool isValid = true;

        if (_analyzer == null)
        {
            Debug.LogError("[DailyVisualBootstrapper] AnalyzingPhaseOrchestrator nao atribuido no Inspector!", this);
            isValid = false;
        }

        if (_scanner == null)
        {
            Debug.LogError("[DailyVisualBootstrapper] GridSlotScanner nao atribuido no Inspector!", this);
            isValid = false;
        }

        if (_uiManager == null)
        {
            Debug.LogError("[DailyVisualBootstrapper] PatternUIManager nao atribuido no Inspector!", this);
            isValid = false;
        }
        
        if (_handManager == null)
        {
            Debug.LogWarning("[DailyVisualBootstrapper] HandManager nao atribuido - ForceReleaseAllDrags desabilitado", this);
            // Nao e critico, apenas warning
        }

        if (isValid)
        {
            Debug.Log("[DailyVisualBootstrapper] Todas as referencias visuais validas");
        }

        return isValid;
    }

    /// <summary>
    /// Getter publico para acesso individual (se necessario).
    /// </summary>
    public AnalyzingPhaseOrchestrator Analyzer => _analyzer;
    public GridSlotScanner Scanner => _scanner;
    public PatternUIManager UIManager => _uiManager;
    public HandManager HandManager => _handManager;
}
