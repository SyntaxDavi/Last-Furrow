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
/// 
/// ONDA 5.5: Usa IPatternFactory ao invés de reflexão (SOLID).
/// </summary>
public class PatternDetector
{
    private readonly List<IGridPattern> _patterns;
    private readonly PatternLibrary _library;
    private readonly IPatternFactory _factory;
    
    /// <summary>
    /// Construtor refatorado: Recebe IPatternFactory (Dependency Inversion).
    /// </summary>
    public PatternDetector(PatternLibrary library, IPatternFactory factory)
    {
        _library = library;
        _factory = factory;
        _patterns = new List<IGridPattern>();
        
        if (_library == null)
        {
            Debug.LogError("[PatternDetector] PatternLibrary é NULL! Nenhum padrão será detectado.");
            return;
        }
        
        if (_factory == null)
        {
            Debug.LogError("[PatternDetector] PatternFactory é NULL! Nenhum padrão será detectado.");
            return;
        }
        
        // Instanciar padrões usando factory (type-safe, sem reflexão)
        InitializePatternsFromLibrary();
        
        Debug.Log($"[PatternDetector] ? Inicializado com {_patterns.Count} padrões (Onda 5.5 - Factory)");
    }
    
    /// <summary>
    /// Instancia IGridPattern implementations usando factory.
    /// </summary>
    private void InitializePatternsFromLibrary()
    {
        foreach (var definition in _library.Patterns)
        {
            if (definition == null)
            {
                Debug.LogWarning("[PatternDetector] PatternDefinition NULL encontrado!");
                continue;
            }
            
            if (string.IsNullOrEmpty(definition.ImplementationClassName))
            {
                Debug.LogWarning($"[PatternDetector] {definition.PatternID} não tem ImplementationClassName!");
                continue;
            }
            
            // Usar factory ao invés de reflexão
            IGridPattern pattern = _factory.CreatePattern(definition);
            
            if (pattern != null)
            {
                _patterns.Add(pattern);
                Debug.Log($"[PatternDetector] • {definition.PatternID} ({definition.DisplayName}) = {definition.BaseScore} pts");
            }
        }
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
        
        if (_patterns.Count == 0)
        {
            Debug.LogWarning("[PatternDetector] Nenhum padrão registrado! Verifique PatternLibrary.");
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
