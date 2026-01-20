using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema centralizado de object pooling para evitar GC spikes.
/// 
/// RESPONSABILIDADE:
/// - Gerenciar pools de GameObjects reutilizáveis
/// - Pre-warm (criar objetos antecipadamente)
/// - Get/Return pattern para reuso
/// - Auto-grow se pool esvaziar
/// 
/// USO:
/// var popup = PatternObjectPool.Instance.Get("PopupText");
/// // ... usar popup ...
/// PatternObjectPool.Instance.Return("PopupText", popup);
/// 
/// FILOSOFIA: Singleton para acesso global, mas configurável via SO.
/// </summary>
public class PatternObjectPool : MonoBehaviour
{
    public static PatternObjectPool Instance { get; private set; }
    
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _config;
    
    [Header("Prefabs (Arrastar do Project)")]
    [SerializeField] private GameObject _popupTextPrefab;
    [SerializeField] private GameObject _sparklesPrefab;
    [SerializeField] private GameObject _highlightPrefab;
    
    // Dicionário de pools por chave
    private Dictionary<string, Queue<GameObject>> _pools;
    
    // Parent para organizar hierarchy
    private Transform _poolParent;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        // Inicializar pools
        _pools = new Dictionary<string, Queue<GameObject>>();
        
        // Criar parent para organizar
        _poolParent = new GameObject("[Object Pool]").transform;
        _poolParent.SetParent(transform);
        
        // Pre-warm se configurado
        if (_config != null && _config.preWarmPools)
        {
            PreWarmPools();
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    /// <summary>
    /// Pre-warm: criar objetos antecipadamente para evitar spikes no gameplay.
    /// </summary>
    private void PreWarmPools()
    {
        if (_config == null)
        {
            Debug.LogWarning("[PatternObjectPool] Config não atribuído! Pre-warm cancelado.");
            return;
        }
        
        // Pre-warm pop-ups
        if (_popupTextPrefab != null)
        {
            PreWarmPool("PopupText", _popupTextPrefab, _config.popupPoolSize);
        }
        
        // Pre-warm sparkles
        if (_sparklesPrefab != null)
        {
            PreWarmPool("Sparkles", _sparklesPrefab, _config.particlePoolSize);
        }
        
        // Pre-warm highlights
        if (_highlightPrefab != null)
        {
            PreWarmPool("Highlight", _highlightPrefab, 10);
        }
        
        _config?.DebugLog($"Pools pre-warmed: PopupText({_config.popupPoolSize}), " +
                         $"Sparkles({_config.particlePoolSize})");
    }
    
    /// <summary>
    /// Pre-warm um pool específico.
    /// </summary>
    private void PreWarmPool(string poolKey, GameObject prefab, int size)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[PatternObjectPool] Prefab NULL para pool '{poolKey}'");
            return;
        }
        
        if (!_pools.ContainsKey(poolKey))
        {
            _pools[poolKey] = new Queue<GameObject>();
        }
        
        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab, _poolParent);
            obj.SetActive(false);
            obj.name = $"{poolKey}_{i}";
            _pools[poolKey].Enqueue(obj);
        }
    }
    
    /// <summary>
    /// Pega um objeto do pool (ou cria novo se pool vazio).
    /// </summary>
    public GameObject Get(string poolKey)
    {
        // Debug override: desabilitar pooling (testar GC)
        if (_config != null && _config.disablePooling)
        {
            return CreateNewObject(poolKey);
        }
        
        // Se pool não existe, criar
        if (!_pools.ContainsKey(poolKey))
        {
            _pools[poolKey] = new Queue<GameObject>();
        }
        
        GameObject obj;
        
        // Se pool tem objetos, retornar um
        if (_pools[poolKey].Count > 0)
        {
            obj = _pools[poolKey].Dequeue();
            obj.SetActive(true);
            _config?.DebugLog($"Pool GET: {poolKey} (pool size: {_pools[poolKey].Count})");
        }
        else
        {
            // Pool vazio - auto-grow (criar novo)
            obj = CreateNewObject(poolKey);
            _config?.DebugLog($"Pool AUTO-GROW: {poolKey} (pool vazio)");
        }
        
        return obj;
    }
    
    /// <summary>
    /// Retorna um objeto ao pool.
    /// </summary>
    public void Return(string poolKey, GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogWarning($"[PatternObjectPool] Tentativa de retornar objeto NULL ao pool '{poolKey}'");
            return;
        }
        
        // Debug override: se pooling desabilitado, destruir ao invés de retornar
        if (_config != null && _config.disablePooling)
        {
            Destroy(obj);
            return;
        }
        
        // Desativar e retornar ao pool
        obj.SetActive(false);
        obj.transform.SetParent(_poolParent);
        
        if (!_pools.ContainsKey(poolKey))
        {
            _pools[poolKey] = new Queue<GameObject>();
        }
        
        _pools[poolKey].Enqueue(obj);
        _config?.DebugLog($"Pool RETURN: {poolKey} (pool size: {_pools[poolKey].Count})");
    }
    
    /// <summary>
    /// Cria um novo objeto baseado na chave do pool.
    /// </summary>
    private GameObject CreateNewObject(string poolKey)
    {
        GameObject prefab = GetPrefabForKey(poolKey);
        
        if (prefab == null)
        {
            Debug.LogError($"[PatternObjectPool] Nenhum prefab configurado para pool '{poolKey}'!");
            return null;
        }
        
        GameObject obj = Instantiate(prefab, _poolParent);
        obj.name = $"{poolKey}_dynamic";
        return obj;
    }
    
    /// <summary>
    /// Retorna o prefab correto baseado na chave.
    /// </summary>
    private GameObject GetPrefabForKey(string poolKey)
    {
        return poolKey switch
        {
            "PopupText" => _popupTextPrefab,
            "Sparkles" => _sparklesPrefab,
            "Highlight" => _highlightPrefab,
            _ => null
        };
    }
    
    /// <summary>
    /// Limpa todos os pools (útil para testes ou mudança de cena).
    /// </summary>
    public void ClearAll()
    {
        foreach (var pool in _pools.Values)
        {
            while (pool.Count > 0)
            {
                var obj = pool.Dequeue();
                if (obj != null) Destroy(obj);
            }
        }
        
        _pools.Clear();
        _config?.DebugLog("Todos os pools foram limpos");
    }
    
    /// <summary>
    /// Retorna estatísticas dos pools (para debug/metrics).
    /// </summary>
    public Dictionary<string, int> GetPoolStats()
    {
        var stats = new Dictionary<string, int>();
        
        foreach (var kvp in _pools)
        {
            stats[kvp.Key] = kvp.Value.Count;
        }
        
        return stats;
    }
}
