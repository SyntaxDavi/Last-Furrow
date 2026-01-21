using UnityEngine;

/// <summary>
/// Bootstrapper Pattern: Centraliza referências visuais da scene.
/// Elimina múltiplos FindFirstObjectByType no AppCore.
/// 
/// SETUP:
/// 1. Adicione este componente em um GameObject na scene "Game"
/// 2. Arraste as referências no Inspector
/// 3. AppCore busca APENAS este bootstrapper (1x ao invés de 3x)
/// 
/// BENEFÍCIOS:
/// - Performance: 1 busca ao invés de 3
/// - Validação: Erros claros no Inspector se faltar algo
/// - Manutenibilidade: Todas referências visuais em 1 lugar
/// </summary>
public class DailyVisualBootstrapper : MonoBehaviour
{
    [Header("Visual Controllers (Arraste da Scene)")]
    [SerializeField] private AnalyzingPhaseController _analyzer;
    [SerializeField] private GridSlotScanner _scanner;
    [SerializeField] private PatternUIManager _uiManager;
    
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
    /// Cria contexto visual com todas as dependências.
    /// Chamado pelo AppCore após scene load.
    /// </summary>
    public DailyVisualContext CreateContext()
    {
        if (!ValidateReferences())
        {
            Debug.LogError("[DailyVisualBootstrapper] Não é possível criar contexto - referências inválidas!");
            return null;
        }
        
        Debug.Log("[DailyVisualBootstrapper] ? Contexto visual criado com sucesso");
        return new DailyVisualContext(_analyzer, _scanner, _uiManager);
    }
    
    /// <summary>
    /// Valida se todas as referências foram atribuídas no Inspector.
    /// </summary>
    private bool ValidateReferences()
    {
        bool isValid = true;
        
        if (_analyzer == null)
        {
            Debug.LogError("[DailyVisualBootstrapper] AnalyzingPhaseController não atribuído no Inspector!", this);
            isValid = false;
        }
        
        if (_scanner == null)
        {
            Debug.LogError("[DailyVisualBootstrapper] GridSlotScanner não atribuído no Inspector!", this);
            isValid = false;
        }
        
        if (_uiManager == null)
        {
            Debug.LogError("[DailyVisualBootstrapper] PatternUIManager não atribuído no Inspector!", this);
            isValid = false;
        }
        
        if (isValid)
        {
            Debug.Log("[DailyVisualBootstrapper] ? Todas as referências visuais válidas");
        }
        
        return isValid;
    }
    
    /// <summary>
    /// Getter público para acesso individual (se necessário).
    /// </summary>
    public AnalyzingPhaseController Analyzer => _analyzer;
    public GridSlotScanner Scanner => _scanner;
    public PatternUIManager UIManager => _uiManager;
}
