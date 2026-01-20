using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject que contém a biblioteca de todos os padrões do jogo.
/// 
/// RESPONSABILIDADE:
/// - Centralizar configuração de TODOS os padrões
/// - Prover acesso read-only aos padrões
/// - Validar IDs únicos
/// - Permitir tuning de valores via Inspector
/// 
/// ARQUITETURA:
/// PatternLibrary (este arquivo) ? PatternDetector ? IGridPattern implementations
/// 
/// WORKFLOW DE BALANCEAMENTO:
/// 1. Abra PatternLibrary.asset no Inspector
/// 2. Modifique BaseScore de qualquer padrão
/// 3. Play ? teste ? ajuste
/// 4. Nenhum código precisa mudar!
/// 
/// USO:
/// - Criar via: Assets ? Create ? Patterns ? Pattern Library
/// - Arrastar no AppCore Inspector
/// </summary>
[CreateAssetMenu(fileName = "PatternLibrary", menuName = "Patterns/Pattern Library", order = 0)]
public class PatternLibrary : ScriptableObject
{
    [Header("Biblioteca de Padrões")]
    [Tooltip("Lista de todos os padrões disponíveis no jogo")]
    [SerializeField] private List<PatternDefinitionSO> _patterns = new List<PatternDefinitionSO>();
    
    [Header("Debug")]
    [SerializeField] private bool _enableValidationLogs = true;
    
    /// <summary>
    /// Acesso read-only à lista de padrões.
    /// </summary>
    public IReadOnlyList<PatternDefinitionSO> Patterns => _patterns;
    
    /// <summary>
    /// Busca padrão por ID (usado em SaveData).
    /// </summary>
    /// <param name="patternID">ID estável do padrão (ex: "FULL_LINE")</param>
    /// <returns>PatternDefinition ou null se não encontrado</returns>
    public PatternDefinitionSO GetPatternByID(string patternID)
    {
        if (string.IsNullOrEmpty(patternID))
        {
            Debug.LogWarning("[PatternLibrary] GetPatternByID: patternID é null ou vazio");
            return null;
        }
        
        foreach (var pattern in _patterns)
        {
            if (pattern != null && pattern.PatternID == patternID)
            {
                return pattern;
            }
        }
        
        if (_enableValidationLogs)
        {
            Debug.LogWarning($"[PatternLibrary] Padrão não encontrado: {patternID}");
        }
        
        return null;
    }
    
    /// <summary>
    /// Busca padrão por nome de classe de implementação.
    /// </summary>
    /// <param name="className">Nome da classe (ex: "AdjacentPairPattern")</param>
    /// <returns>PatternDefinition ou null se não encontrado</returns>
    public PatternDefinitionSO GetPatternByClassName(string className)
    {
        if (string.IsNullOrEmpty(className))
            return null;
        
        foreach (var pattern in _patterns)
        {
            if (pattern != null && pattern.ImplementationClassName == className)
            {
                return pattern;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Retorna todos os padrões de um tier específico.
    /// </summary>
    public List<PatternDefinitionSO> GetPatternsByTier(int tier)
    {
        var result = new List<PatternDefinitionSO>();
        
        foreach (var pattern in _patterns)
        {
            if (pattern != null && pattern.Tier == tier)
            {
                result.Add(pattern);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Validação automática no Inspector.
    /// Verifica:
    /// - IDs duplicados
    /// - Padrões nulos
    /// - Campos obrigatórios vazios
    /// </summary>
    private void OnValidate()
    {
        if (_patterns == null || _patterns.Count == 0)
        {
            if (_enableValidationLogs)
            {
                Debug.LogWarning("[PatternLibrary] Lista de padrões está vazia!");
            }
            return;
        }
        
        // Detectar IDs duplicados
        var seenIDs = new HashSet<string>();
        var duplicates = new List<string>();
        
        foreach (var pattern in _patterns)
        {
            if (pattern == null)
            {
                Debug.LogError("[PatternLibrary] Padrão NULL detectado na lista! Remova slots vazios.");
                continue;
            }
            
            if (string.IsNullOrEmpty(pattern.PatternID))
            {
                Debug.LogError($"[PatternLibrary] Padrão '{pattern.name}' tem PatternID vazio!");
                continue;
            }
            
            if (seenIDs.Contains(pattern.PatternID))
            {
                duplicates.Add(pattern.PatternID);
            }
            else
            {
                seenIDs.Add(pattern.PatternID);
            }
        }
        
        if (duplicates.Count > 0 && _enableValidationLogs)
        {
            Debug.LogError($"[PatternLibrary] IDs DUPLICADOS detectados: {string.Join(", ", duplicates)}");
        }
        
        if (_enableValidationLogs && duplicates.Count == 0)
        {
            Debug.Log($"[PatternLibrary] ? Validação OK: {_patterns.Count} padrões únicos");
        }
    }
    
    /// <summary>
    /// Helper para criar asset inicial com todos os padrões.
    /// Chamado via menu de contexto no Inspector.
    /// </summary>
    [ContextMenu("Log All Pattern IDs")]
    private void LogAllPatternIDs()
    {
        Debug.Log("=== PATTERN LIBRARY IDs ===");
        foreach (var pattern in _patterns)
        {
            if (pattern != null)
            {
                Debug.Log($"• {pattern.PatternID} ({pattern.DisplayName}) = {pattern.BaseScore} pts [Tier {pattern.Tier}]");
            }
        }
        Debug.Log("===========================");
    }
}
