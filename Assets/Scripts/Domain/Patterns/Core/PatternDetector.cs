using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serviço responsável por detectar padrões no grid.
/// 
/// RESPONSABILIDADE ÚNICA (O Orquestrador Burro):
/// - Percorrer a lista de padrões registrados
/// - Delegar detecção para cada IGridPattern.DetectAll()
/// - Coletar resultados em lista de PatternMatch
/// 
/// EXPLICITAMENTE FORA DO ESCOPO:
/// - ? Cálculo de score (isso é PatternScoreCalculator)
/// - ? Aplicação de decay
/// - ? Cálculo de sinergia
/// - ? Priorização de padrões
/// - ? Agrupamento de matches
/// - ? Lógica de coordenação entre padrões
/// 
/// FILOSOFIA: Detector é STATELESS.
/// Não guarda histórico, não decide valor, não modifica estado.
/// </summary>
public class PatternDetector
{
    private readonly List<IGridPattern> _patterns;
    
    public PatternDetector()
    {
        // Onda 1: Padrões hardcoded (Onda 5 migrará para ScriptableObject)
        _patterns = new List<IGridPattern>
        {
            new AdjacentPairPattern(),
            new TrioLinePattern(),
            new GridCornerPattern(),
            new FullLinePattern(),
            new GridCrossPattern()
        };
        
        Debug.Log($"[PatternDetector] Inicializado com {_patterns.Count} padrões registrados");
    }
    
    /// <summary>
    /// Detecta todos os padrões válidos no grid atual.
    /// 
    /// REGRAS:
    /// - Cada padrão pode retornar múltiplos matches
    /// - Sobreposição é permitida e incentivada
    /// - Ordem de detecção não importa (padrões são independentes)
    /// </summary>
    /// <param name="gridService">Serviço de grid para consultar slots</param>
    /// <returns>Lista de todos os padrões detectados</returns>
    public List<PatternMatch> DetectAll(IGridService gridService)
    {
        var allMatches = new List<PatternMatch>();
        
        if (gridService == null)
        {
            Debug.LogError("[PatternDetector] GridService é null!");
            return allMatches;
        }
        
        foreach (var pattern in _patterns)
        {
            try
            {
                var matches = pattern.DetectAll(gridService);
                
                if (matches != null && matches.Count > 0)
                {
                    allMatches.AddRange(matches);
                    
                    // Log verbose para debug
                    foreach (var match in matches)
                    {
                        Debug.Log($"[PatternDetector] ? {match}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PatternDetector] Erro ao detectar {pattern.PatternID}: {ex.Message}");
            }
        }
        
        Debug.Log($"[PatternDetector] Total: {allMatches.Count} padrões detectados");
        return allMatches;
    }
}
