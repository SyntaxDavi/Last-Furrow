using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Orquestra a aplicação de múltiplas regras de draw.
/// 
/// ARQUITETURA: Pipeline Pattern
/// - Regras são aplicadas em ordem de prioridade
/// - Cada regra recebe o output da anterior
/// - Contexto compartilhado entre todas
/// 
/// USO COM BIBLIOTECA:
/// var policy = DrawValidationPolicy.CreateFromLibrary(cardDropLibrary);
/// var validatedDraw = policy.Validate(drawnCards, context);
/// </summary>
public class DrawValidationPolicy
{
    private readonly List<IDrawRule> _rules = new List<IDrawRule>();
    private bool _rulesSorted = false;
    
    /// <summary>
    /// Adiciona uma regra ao pipeline.
    /// </summary>
    public DrawValidationPolicy AddRule(IDrawRule rule)
    {
        if (rule == null)
        {
            Debug.LogWarning("[DrawValidationPolicy] Tentativa de adicionar regra NULL ignorada.");
            return this;
        }
        
        _rules.Add(rule);
        _rulesSorted = false;
        return this; // Fluent API
    }
    
    /// <summary>
    /// Remove todas as regras de um tipo específico.
    /// </summary>
    public DrawValidationPolicy RemoveRulesOfType<T>() where T : IDrawRule
    {
        _rules.RemoveAll(r => r is T);
        return this;
    }
    
    /// <summary>
    /// Limpa todas as regras.
    /// </summary>
    public DrawValidationPolicy ClearRules()
    {
        _rules.Clear();
        return this;
    }
    
    /// <summary>
    /// Valida e modifica o draw aplicando todas as regras em ordem.
    /// </summary>
    public List<CardID> Validate(List<CardID> drawnCards, DrawRuleContext context)
    {
        if (drawnCards == null)
            return new List<CardID>();
        
        if (_rules.Count == 0)
        {
            Debug.LogWarning("[DrawValidationPolicy] Nenhuma regra configurada. Retornando draw original.");
            return drawnCards;
        }
        
        // Ordena por prioridade se necessário
        EnsureRulesSorted();
        
        var current = new List<CardID>(drawnCards);
        
        foreach (var rule in _rules)
        {
            var before = current.Count;
            current = rule.Apply(current, context);
            var after = current.Count;
            
            if (before != after)
            {
                Debug.Log($"[DrawValidationPolicy] Regra '{rule.RuleName}' modificou draw: {before} → {after} cartas");
            }
        }
        
        return current;
    }
    
    /// <summary>
    /// Cria uma policy vazia sem regras (para testes ou uso customizado).
    /// </summary>
    public static DrawValidationPolicy CreateEmpty()
    {
        return new DrawValidationPolicy();
    }
    
    /// <summary>
    /// Cria uma policy com regras padrão usando a CardDropLibrary.
    /// As regras lêem configurações de cada CardDropData.
    /// </summary>
    /// <param name="library">Biblioteca com configurações de cartas</param>
    /// <param name="globalMaxPerDraw">Limite global de duplicatas para cartas sem configuração</param>
    public static DrawValidationPolicy CreateFromLibrary(CardDropLibrarySO library, int globalMaxPerDraw = 2)
    {
        var policy = new DrawValidationPolicy();
        
        // Regra 0: Impede draw idêntico ao dia anterior (Priority 5)
        policy.AddRule(new NoRepeatDrawRule());
        
        // Regra 1: Máximo de duplicatas (lê MaxPerDraw de cada carta, Priority 10)
        policy.AddRule(new MaxDuplicatesRule(library, globalMaxPerDraw));
        
        // Regra 2: Cartas garantidas (auto-descobre cartas com GuaranteeAfterDays > 0, Priority 50)
        policy.AddRule(new GuaranteedCardsRule(library));
        
        return policy;
    }
    
    /// <summary>
    /// [DEPRECATED] Cria policy com valores hardcoded.
    /// Use CreateFromLibrary() para configuração via Inspector.
    /// </summary>
    public static DrawValidationPolicy CreateDefault()
    {
        Debug.LogWarning("[DrawValidationPolicy] CreateDefault() está deprecated. Use CreateFromLibrary() para configuração via Inspector.");
        return new DrawValidationPolicy()
            .AddRule(new MaxDuplicatesRule(null, 2))
            .AddRule(new GuaranteedCardsRule(null));
    }
    
    private void EnsureRulesSorted()
    {
        if (_rulesSorted) return;
        
        _rules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        _rulesSorted = true;
    }
    
    /// <summary>
    /// Número de regras configuradas.
    /// </summary>
    public int RuleCount => _rules.Count;
    
    /// <summary>
    /// Lista nomes das regras para debug.
    /// </summary>
    public IEnumerable<string> GetRuleNames()
    {
        return _rules.Select(r => r.RuleName);
    }
}
