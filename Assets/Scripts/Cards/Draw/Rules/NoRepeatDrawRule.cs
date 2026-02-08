using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Regra: Impede que o draw seja idêntico ao dia anterior.
/// 
/// COMPORTAMENTO:
/// - Compara draw atual com último draw (independente da ordem)
/// - Se forem idênticos, substitui UMA carta pela próxima do deck
/// - Salva draw atual para comparação no próximo dia
/// 
/// EXEMPLO: 
/// Dia 1: [🌽, 🌾, 🥕]
/// Dia 2: [🥕, 🌽, 🌾] → Idêntico! → [🥕, 🌽, 🌻]
/// </summary>
public class NoRepeatDrawRule : IDrawRule
{
    public int Priority => 5; // Executa primeiro, antes de MaxDuplicates
    public string RuleName => "NoRepeatDraw";
    
    public List<CardID> Apply(List<CardID> drawnCards, DrawRuleContext context)
    {
        if (drawnCards == null || drawnCards.Count == 0 || context.RunData == null)
            return drawnCards;
        
        var runData = context.RunData;
        
        // 1. Verifica se temos draw anterior para comparar
        if (runData.LastDrawCardIDs == null || runData.LastDrawCardIDs.Count == 0)
        {
            // Primeiro draw da run - apenas salva e retorna
            SaveCurrentDraw(runData, drawnCards);
            return drawnCards;
        }
        
        // 2. Compara draws (independente da ordem)
        if (AreDrawsIdentical(drawnCards, runData.LastDrawCardIDs))
        {
            Debug.Log($"[{RuleName}] Draw idêntico ao anterior detectado! Substituindo uma carta...");
            
            // 3. Substitui UMA carta pela próxima do deck
            var modifiedDraw = SubstituteOneCard(drawnCards, runData.LastDrawCardIDs, context);
            
            // 4. Salva novo draw
            SaveCurrentDraw(runData, modifiedDraw);
            return modifiedDraw;
        }
        
        // Draw diferente - apenas salva e retorna
        SaveCurrentDraw(runData, drawnCards);
        return drawnCards;
    }
    
    /// <summary>
    /// Compara dois draws ignorando a ordem.
    /// </summary>
    private bool AreDrawsIdentical(List<CardID> current, List<string> previous)
    {
        if (current.Count != previous.Count)
            return false;
        
        // Cria contagem de cartas para ambos os draws
        var currentCounts = new Dictionary<string, int>();
        var previousCounts = new Dictionary<string, int>();
        
        foreach (var card in current)
        {
            if (!card.IsValid) continue;
            currentCounts.TryGetValue(card.Value, out int count);
            currentCounts[card.Value] = count + 1;
        }
        
        foreach (var cardId in previous)
        {
            previousCounts.TryGetValue(cardId, out int count);
            previousCounts[cardId] = count + 1;
        }
        
        // Compara contagens
        if (currentCounts.Count != previousCounts.Count)
            return false;
        
        foreach (var kvp in currentCounts)
        {
            if (!previousCounts.TryGetValue(kvp.Key, out int prevCount) || prevCount != kvp.Value)
                return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Substitui uma carta do draw atual por uma carta diferente do deck.
    /// </summary>
    private List<CardID> SubstituteOneCard(
        List<CardID> current, 
        List<string> previous, 
        DrawRuleContext context)
    {
        var result = new List<CardID>(current);
        
        if (context.Deck == null || context.Deck.RemainingCards == 0)
        {
            Debug.LogWarning($"[{RuleName}] Deck vazio! Não é possível substituir carta.");
            return result;
        }
        
        // Cria set do draw anterior para verificação rápida
        var previousSet = new HashSet<string>(previous);
        
        // Tenta encontrar uma carta substituta que seja diferente
        const int maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            var replacement = context.Deck.DrawNext();
            if (!replacement.IsValid)
            {
                Debug.LogWarning($"[{RuleName}] Deck esgotado durante substituição.");
                break;
            }
            
            // Verifica se a substituição cria um draw diferente
            // Substitui a primeira carta e verifica
            var testDraw = new List<CardID>(current);
            testDraw[0] = replacement;
            
            if (!AreDrawsIdentical(testDraw, previous))
            {
                result[0] = replacement;
                Debug.Log($"[{RuleName}] Carta substituída: {current[0].Value} → {replacement.Value}");
                return result;
            }
        }
        
        Debug.LogWarning($"[{RuleName}] Não foi possível encontrar carta substituta diferente após {maxAttempts} tentativas.");
        return result;
    }
    
    private void SaveCurrentDraw(RunData runData, List<CardID> draw)
    {
        runData.LastDrawCardIDs ??= new List<string>();
        runData.LastDrawCardIDs.Clear();
        
        foreach (var card in draw)
        {
            if (card.IsValid)
            {
                runData.LastDrawCardIDs.Add(card.Value);
            }
        }
    }
}
