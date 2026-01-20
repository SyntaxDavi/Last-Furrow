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
/// ONDA 5: Padrões são instanciados dinamicamente a partir do PatternLibrary.
/// </summary>
public class PatternDetector
{
    private readonly List<IGridPattern> _patterns;
    private readonly PatternLibrary _library;
    
    /// <summary>
    /// Construtor da Onda 5: Recebe PatternLibrary e instancia padrões.
    /// </summary>
    /// <param name="library">ScriptableObject com definições de padrões</param>
    public PatternDetector(PatternLibrary library)
    {
        _library = library;
        _patterns = new List<IGridPattern>();
        
        if (_library == null)
        {
            Debug.LogError("[PatternDetector] PatternLibrary é NULL! Nenhum padrão será detectado.");
            return;
        }
        
        // Instanciar padrões dinamicamente baseado no PatternLibrary
        InitializePatternsFromLibrary();
        
        Debug.Log($"[PatternDetector] ? Inicializado com {_patterns.Count} padrões (Onda 5)");
    }
    
    /// <summary>
    /// Instancia IGridPattern implementations baseado no PatternLibrary.
    /// Usa reflexão para criar instâncias dinamicamente.
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
            
            // Criar instância do padrão via reflexão
            IGridPattern pattern = CreatePatternInstance(definition);
            
            if (pattern != null)
            {
                _patterns.Add(pattern);
                Debug.Log($"[PatternDetector] • {definition.PatternID} ({definition.DisplayName}) = {definition.BaseScore} pts");
            }
        }
    }
    
    /// <summary>
    /// Cria instância de IGridPattern usando reflexão.
    /// </summary>
    private IGridPattern CreatePatternInstance(PatternDefinitionSO definition)
    {
        try
        {
            // Buscar tipo pelo nome da classe
            System.Type type = System.Type.GetType(definition.ImplementationClassName);
            
            if (type == null)
            {
                Debug.LogError($"[PatternDetector] Classe não encontrada: {definition.ImplementationClassName}");
                return null;
            }
            
            // Verificar se tem construtor que recebe PatternDefinitionSO
            var constructor = type.GetConstructor(new[] { typeof(PatternDefinitionSO) });
            
            if (constructor != null)
            {
                // Instanciar com PatternDefinitionSO
                return (IGridPattern)constructor.Invoke(new object[] { definition });
            }
            else
            {
                // Fallback: tentar construtor sem parâmetros (compatibilidade Onda 1-4)
                var defaultConstructor = type.GetConstructor(System.Type.EmptyTypes);
                
                if (defaultConstructor != null)
                {
                    Debug.LogWarning($"[PatternDetector] {definition.ImplementationClassName} usa construtor antigo (sem SO). Considere atualizar.");
                    return (IGridPattern)defaultConstructor.Invoke(null);
                }
            }
            
            Debug.LogError($"[PatternDetector] Nenhum construtor válido encontrado para {definition.ImplementationClassName}");
            return null;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PatternDetector] Erro ao criar {definition.ImplementationClassName}: {ex.Message}");
            return null;
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
