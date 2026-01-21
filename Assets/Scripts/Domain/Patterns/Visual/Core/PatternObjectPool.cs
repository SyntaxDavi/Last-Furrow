using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pooling simples para objetos visuais de padrões.
/// </summary>
public class PatternObjectPool : MonoBehaviour
{
    [SerializeField] private PatternVisualConfig _config;
    [SerializeField] private GameObject _popupTextPrefab;
    [SerializeField] private GameObject _sparklesPrefab;
    [SerializeField] private GameObject _highlightPrefab;
    
    public static PatternObjectPool Instance { get; private set; }
    
    private Dictionary<string, Queue<GameObject>> _pools;
    private Transform _poolParent;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        _pools = new Dictionary<string, Queue<GameObject>>();
        
        _poolParent = new GameObject("[Object Pool]").transform;
        _poolParent.SetParent(transform);
        
        PreWarmPools();
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    private void PreWarmPools()
    {
        if (_popupTextPrefab != null)
        {
            PreWarmPool("PopupText", _popupTextPrefab, 5);
        }
        
        if (_sparklesPrefab != null)
        {
            PreWarmPool("Sparkles", _sparklesPrefab, 20);
        }
        
        if (_highlightPrefab != null)
        {
            PreWarmPool("Highlight", _highlightPrefab, 10);
        }
    }
    
    private void PreWarmPool(string poolKey, GameObject prefab, int size)
    {
        if (prefab == null) return;
        
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
    
    public GameObject Get(string poolKey)
    {
        if (!_pools.ContainsKey(poolKey))
        {
            _pools[poolKey] = new Queue<GameObject>();
        }
        
        GameObject obj;
        
        if (_pools[poolKey].Count > 0)
        {
            obj = _pools[poolKey].Dequeue();
            obj.SetActive(true);
        }
        else
        {
            obj = CreateNewObject(poolKey);
        }
        
        return obj;
    }
    
    public void Return(string poolKey, GameObject obj)
    {
        if (obj == null) return;
        
        obj.SetActive(false);
        obj.transform.SetParent(_poolParent);
        
        if (!_pools.ContainsKey(poolKey))
        {
            _pools[poolKey] = new Queue<GameObject>();
        }
        
        _pools[poolKey].Enqueue(obj);
    }
    
    private GameObject CreateNewObject(string poolKey)
    {
        GameObject prefab = GetPrefabForKey(poolKey);
        
        if (prefab == null)
        {
            Debug.LogError($"[PatternObjectPool] No prefab for key '{poolKey}'");
            return null;
        }
        
        GameObject obj = Instantiate(prefab, _poolParent);
        obj.name = $"{poolKey}_dynamic";
        return obj;
    }
    
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
}
