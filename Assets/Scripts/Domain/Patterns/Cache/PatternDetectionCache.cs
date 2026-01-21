using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cache temporário que armazena padrões detectados na verificação inteira.
/// 
/// RESPONSABILIDADE (SOLID):
/// - Armazenar List<PatternMatch> após DetectAll()
/// - Fornecer lookup rápido por slot (GetPatternsForSlot)
/// - Limpar dados quando necessário
/// 
/// FILOSOFIA:
/// - Singleton temporário (vive apenas durante verificação)
/// - Desacopla detecção de grid da UI e scanner incremental
/// - Single Source of Truth para padrões detectados
/// 
/// FLOW:
/// 1. Verificação Inteira ? StorePatterns(matches)
/// 2. Scanner Incremental ? GetPatternsForSlot(slotIndex)
/// 3. Fim da verificação ? Clear()
/// 
/// </summary>
public class PatternDetectionCache : MonoBehaviour, IPatternCache
{
    public static PatternDetectionCache Instance { get; private set; }
    
    // Padrões detectados (fonte da verdade)
    private List<PatternMatch> _detectedPatterns = new List<PatternMatch>();
    
    // Índice para lookup rápido: slotIndex ? List<PatternMatch>
    private Dictionary<int, List<PatternMatch>> _slotToPatterns = new Dictionary<int, List<PatternMatch>>();
    
    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    /// <summary>
    /// Armazena padrões detectados e cria índice para lookup rápido.
    /// </summary>
    public void StorePatterns(List<PatternMatch> patterns)
    {
        if (patterns == null)
        {
            Debug.LogWarning("[PatternCache] Tentativa de armazenar NULL, ignorando");
            return;
        }
        
        // Limpar cache anterior
        Clear();
        
        _detectedPatterns = new List<PatternMatch>(patterns);
        
        // Criar índice: slotIndex ? List<PatternMatch>
        foreach (var pattern in patterns)
        {
            if (pattern.SlotIndices == null) continue;
            
            foreach (int slotIndex in pattern.SlotIndices)
            {
                if (!_slotToPatterns.ContainsKey(slotIndex))
                {
                    _slotToPatterns[slotIndex] = new List<PatternMatch>();
                }
                
                _slotToPatterns[slotIndex].Add(pattern);
            }
        }
        
        Debug.Log($"[PatternCache] {patterns.Count} padrões armazenados, índice criado para {_slotToPatterns.Count} slots");
    }
    
    /// <summary>
    /// Retorna todos os padrões que incluem um slot específico.
    /// Retorna lista vazia se slot não faz parte de nenhum padrão.
    /// </summary>
    public List<PatternMatch> GetPatternsForSlot(int slotIndex)
    {
        if (_slotToPatterns.TryGetValue(slotIndex, out var patterns))
        {
            return patterns;
        }
        
        return new List<PatternMatch>(); // Vazio se slot não tem padrões
    }
    
    /// <summary>
    /// Retorna todos os padrões detectados.
    /// </summary>
    public List<PatternMatch> GetAllPatterns()
    {
        return new List<PatternMatch>(_detectedPatterns); // Cópia para evitar modificação externa
    }
    
    /// <summary>
    /// Verifica se há padrões armazenados.
    /// </summary>
    public bool HasPatterns()
    {
        return _detectedPatterns.Count > 0;
    }
    
    /// <summary>
    /// Retorna total de padrões armazenados.
    /// </summary>
    public int GetPatternCount()
    {
        return _detectedPatterns.Count;
    }
    
    /// <summary>
    /// Limpa cache (chama ao fim da verificação ou início de novo dia).
    /// </summary>
    public void Clear()
    {
        _detectedPatterns.Clear();
        _slotToPatterns.Clear();
        Debug.Log("[PatternCache] Cache limpo");
    }
}
