using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Regra: Máximo de N cartas iguais por draw.
/// 
/// COMPORTAMENTO:
/// - Lê MaxPerDraw de cada CardDropData
/// - Se MaxPerDraw == 0, usa globalDefault (normalmente 2)
/// - Cartas excedentes são substituídas por próximas do deck
/// 
/// EXEMPLO: Se Harvest tem MaxPerDraw=1 e draw vem [🌾, 🌾, 🌽]:
/// Resultado: [🌾, 🌽, 🥕] (segundo harvest substituído)
/// </summary>
public class MaxDuplicatesRule : IDrawRule
{
    private readonly CardDropLibrarySO _library;
    private readonly int _globalDefault;
    
    public int Priority => 10; // Executa cedo para limpar duplicatas
    public string RuleName => $"MaxDuplicates(global={_globalDefault})";
    
    /// <summary>
    /// Cria regra usando CardDropLibrary para limites por carta.
    /// </summary>
    /// <param name="library">Biblioteca de configurações de cartas (pode ser null para usar apenas default)</param>
    /// <param name="globalDefault">Limite global para cartas sem configuração customizada</param>
    public MaxDuplicatesRule(CardDropLibrarySO library = null, int globalDefault = 2)
    {
        _library = library;
        _globalDefault = Mathf.Max(1, globalDefault);
    }
    
    public List<CardID> Apply(List<CardID> drawnCards, DrawRuleContext context)
    {
        if (drawnCards == null || drawnCards.Count == 0)
            return drawnCards;
        
        var result = new List<CardID>();
        var counts = new Dictionary<string, int>();
        var replacementsNeeded = 0;
        
        // 1. Primeira passada: identifica o que manter e o que substituir
        foreach (var card in drawnCards)
        {
            if (!card.IsValid)
            {
                result.Add(card);
                continue;
            }
            
            string id = card.Value;
            int maxAllowed = GetMaxPerDraw(card);
            
            counts.TryGetValue(id, out int count);
            
            if (count < maxAllowed)
            {
                result.Add(card);
                counts[id] = count + 1;
            }
            else
            {
                replacementsNeeded++;
            }
        }
        
        // 2. Segunda passada: busca substituições no deck
        if (replacementsNeeded > 0 && context.Deck != null)
        {
            int attempts = 0;
            const int maxAttempts = 15;
            
            while (replacementsNeeded > 0 && attempts < maxAttempts)
            {
                var replacement = context.Deck.DrawNext();
                if (!replacement.IsValid)
                {
                    Debug.LogWarning($"[{RuleName}] Deck esgotado! {replacementsNeeded} cartas não substituídas.");
                    break;
                }
                
                string repId = replacement.Value;
                int repMaxAllowed = GetMaxPerDraw(replacement);
                
                counts.TryGetValue(repId, out int repCount);
                
                if (repCount < repMaxAllowed)
                {
                    result.Add(replacement);
                    counts[repId] = repCount + 1;
                    replacementsNeeded--;
                }
                
                attempts++;
            }
            
            if (replacementsNeeded > 0)
            {
                Debug.LogWarning($"[{RuleName}] Não foi possível substituir {replacementsNeeded} cartas após {maxAttempts} tentativas.");
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Retorna o limite de duplicatas para uma carta específica.
    /// </summary>
    private int GetMaxPerDraw(CardID cardId)
    {
        if (_library != null && _library.TryGetDropData(cardId, out var dropData))
        {
            return dropData.GetEffectiveMaxPerDraw(_globalDefault);
        }
        
        return _globalDefault;
    }
}
