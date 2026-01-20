using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Factory concreta para criação de padrões.
/// 
/// FILOSOFIA:
/// - Registro explícito (não reflexão)
/// - Type-safe
/// - Refactor-friendly
/// - Testável
/// 
/// VANTAGENS vs Reflexão:
/// - ? Compile-time safety
/// - ? Melhor performance
/// - ? Sem riscos de strings inválidas
/// - ? IDE autocomplete funciona
/// </summary>
public class PatternFactory : IPatternFactory
{
    private readonly Dictionary<string, Func<PatternDefinitionSO, IGridPattern>> _factories;
    
    public PatternFactory()
    {
        _factories = new Dictionary<string, Func<PatternDefinitionSO, IGridPattern>>
        {
            // Tier 1 - Iniciante
            ["AdjacentPairPattern"] = (def) => new AdjacentPairPattern(def),
            ["TrioLinePattern"] = (def) => new TrioLinePattern(def),
            ["GridCornerPattern"] = (def) => new GridCornerPattern(def),
            
            // Tier 2 - Casual
            ["FullLinePattern"] = (def) => new FullLinePattern(def),
            ["CheckerPattern"] = (def) => new CheckerPattern(def),
            ["GridCrossPattern"] = (def) => new GridCrossPattern(def),
            
            // Tier 3 - Dedicado
            ["DiagonalPattern"] = (def) => new DiagonalPattern(def),
            ["FramePattern"] = (def) => new FramePattern(def),
            ["RainbowLinePattern"] = (def) => new RainbowLinePattern(def),
            
            // Tier 4 - Master
            ["PerfectGridPattern"] = (def) => new PerfectGridPattern(def)
        };
    }
    
    public IGridPattern CreatePattern(PatternDefinitionSO definition)
    {
        if (definition == null)
        {
            Debug.LogError("[PatternFactory] PatternDefinition é null!");
            return null;
        }
        
        if (string.IsNullOrEmpty(definition.ImplementationClassName))
        {
            Debug.LogError($"[PatternFactory] {definition.PatternID} não tem ImplementationClassName!");
            return null;
        }
        
        if (_factories.TryGetValue(definition.ImplementationClassName, out var factory))
        {
            try
            {
                return factory(definition);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PatternFactory] Erro ao criar {definition.ImplementationClassName}: {ex.Message}");
                return null;
            }
        }
        
        Debug.LogError($"[PatternFactory] Padrão não registrado: {definition.ImplementationClassName}. " +
                      "Adicione ao dicionário _factories no construtor.");
        return null;
    }
    
    public bool CanCreate(string implementationClassName)
    {
        return !string.IsNullOrEmpty(implementationClassName) && 
               _factories.ContainsKey(implementationClassName);
    }
}
