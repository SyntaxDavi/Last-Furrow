using UnityEngine;

/// <summary>
/// Singleton Pattern: Gerencia instância global do PatternVisualConfig.
/// Resolve problema de múltiplos Resources.Load (performance).
/// 
/// SETUP:
/// 1. Adicione ScriptableObject PatternVisualConfig em Resources/Patterns/
/// 2. Este singleton carrega 1x e mantém referência
/// 3. Controllers acessam via PatternVisualConfigProvider.Instance.Config
/// </summary>
public class PatternVisualConfigProvider : MonoBehaviour
{
    private static PatternVisualConfigProvider _instance;
    public static PatternVisualConfigProvider Instance => _instance;
    
    [SerializeField] private PatternVisualConfig _config;
    
    public PatternVisualConfig Config => _config;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Fallback: Se não atribuído, tenta carregar
        if (_config == null)
        {
            _config = Resources.Load<PatternVisualConfig>("Patterns/PatternVisualConfig");
            
            if (_config == null)
            {
                Debug.LogError("[PatternVisualConfigProvider] Config não encontrado! Atribua no Inspector.");
            }
        }
        
        Debug.Log($"[PatternVisualConfigProvider] ? Config carregado: {_config != null}");
    }
    
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
