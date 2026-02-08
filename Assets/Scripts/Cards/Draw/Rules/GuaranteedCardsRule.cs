using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Regra: Garante certas cartas após X dias sem vê-las.
/// 
/// COMPORTAMENTO:
/// - Auto-descobre cartas com GuaranteeAfterDays > 0 na CardDropLibrary
/// - Verifica dias desde última aparição
/// - SUBSTITUI cartas existentes (não adiciona, mantém tamanho do draw)
/// - Respeita GuaranteePriority para ordenar múltiplas garantias
/// 
/// TRACKING: Usa RunData.CardLastSeenDays para persistência entre saves.
/// </summary>
public class GuaranteedCardsRule : IDrawRule
{
    private readonly CardDropLibrarySO _library;
    private List<CardDropData> _guaranteedCards;
    
    public int Priority => 50; // Executa depois de MaxDuplicates
    public string RuleName => $"GuaranteedCards({_guaranteedCards?.Count ?? 0} cartas)";
    
    /// <summary>
    /// Cria regra que auto-descobre cartas garantidas da biblioteca.
    /// </summary>
    public GuaranteedCardsRule(CardDropLibrarySO library)
    {
        _library = library;
        DiscoverGuaranteedCards();
    }
    
    /// <summary>
    /// Descobre todas as cartas com garantia configurada.
    /// </summary>
    private void DiscoverGuaranteedCards()
    {
        _guaranteedCards = new List<CardDropData>();
        
        if (_library == null)
        {
            Debug.LogWarning("[GuaranteedCardsRule] CardDropLibrary é null. Nenhuma garantia será aplicada.");
            return;
        }
        
        foreach (var dropData in _library.AllCardDrops)
        {
            if (dropData != null && dropData.HasGuarantee)
            {
                _guaranteedCards.Add(dropData);
            }
        }
        
        // Ordena por prioridade (menor = primeiro)
        _guaranteedCards = _guaranteedCards
            .OrderBy(d => d.GuaranteePriority)
            .ToList();
        
        if (_guaranteedCards.Count > 0)
        {
            var cardNames = string.Join(", ", _guaranteedCards.Select(c => c.CardID.Value));
            Debug.Log($"[GuaranteedCardsRule] Cartas com garantia: {cardNames}");
        }
    }
    
    public List<CardID> Apply(List<CardID> drawnCards, DrawRuleContext context)
    {
        if (_guaranteedCards == null || _guaranteedCards.Count == 0 || context.RunData == null)
        {
            Debug.LogWarning($"[GuaranteedCardsRule] Pulando: guaranteedCards={_guaranteedCards?.Count ?? 0}, RunData={(context.RunData != null ? "OK" : "NULL")}");
            return drawnCards;
        }
        
        var result = new List<CardID>(drawnCards);
        int currentDay = context.RunData.CurrentDay;
        int currentWeek = context.RunData.CurrentWeek;
        int absoluteDay = CalculateAbsoluteDay(currentWeek, currentDay);
        
        Debug.LogWarning($"[GuaranteedCardsRule] Verificando {_guaranteedCards.Count} cartas. " +
                  $"Semana {currentWeek}, Dia {currentDay} (abs: {absoluteDay}), draw size: {result.Count}");
        
        // Atualiza tracking para cartas que já estão no draw
        foreach (var card in drawnCards)
        {
            if (card.IsValid)
            {
                UpdateLastSeen(context.RunData, card, absoluteDay);
            }
        }
        
        // Coleta cartas que PRECISAM ser garantidas (passaram do limite)
        var cardsToGuarantee = new List<CardDropData>();
        
        foreach (var dropData in _guaranteedCards)
        {
            var cardId = dropData.CardID;
            
            // Pula se já está no draw
            if (ContainsCard(result, cardId))
            {
                Debug.LogWarning($"[GuaranteedCardsRule] '{cardId.Value}' já está no draw.");
                continue;
            }
            
            // Calcula dias desde última aparição
            int lastSeenDay = GetLastSeenDay(context.RunData, cardId);
            int daysSinceLastSeen = absoluteDay - lastSeenDay;
            
            Debug.LogWarning($"[GuaranteedCardsRule] '{cardId.Value}': daysSince={daysSinceLastSeen}, limite={dropData.GuaranteeAfterDays}");
            
            // Se passou do limite, adiciona à lista de garantias pendentes
            if (daysSinceLastSeen >= dropData.GuaranteeAfterDays)
            {
                cardsToGuarantee.Add(dropData);
            }
        }
        
        // SUBSTITUIR cartas do final do draw ao invés de adicionar
        int replaceIndex = result.Count - 1;
        
        foreach (var dropData in cardsToGuarantee)
        {
            var cardId = dropData.CardID;
            
            if (replaceIndex < 0)
            {
                Debug.LogWarning($"[GuaranteedCardsRule] Sem espaço para '{cardId.Value}'!");
                break;
            }
            
            // Substitui a carta na posição atual
            var replacedCard = result[replaceIndex];
            result[replaceIndex] = cardId;
            
            UpdateLastSeen(context.RunData, cardId, absoluteDay);
            
            Debug.LogWarning($"[GuaranteedCardsRule] ✓ '{cardId.Value}' GARANTIDA! (substituiu '{replacedCard.Value}')");
            
            replaceIndex--;
        }
        
        return result;
    }
    
    private bool ContainsCard(List<CardID> cards, CardID target)
    {
        foreach (var card in cards)
        {
            if (card.Value == target.Value)
                return true;
        }
        return false;
    }
    
    private int CalculateAbsoluteDay(int week, int day)
    {
        return ((week - 1) * 7) + day;
    }
    
    private int GetLastSeenDay(RunData runData, CardID cardId)
    {
        if (runData.CardLastSeenDays != null && 
            runData.CardLastSeenDays.TryGetValue(cardId.Value, out int lastDay))
        {
            return lastDay;
        }
        return 0;
    }
    
    private void UpdateLastSeen(RunData runData, CardID cardId, int absoluteDay)
    {
        runData.CardLastSeenDays ??= new Dictionary<string, int>();
        runData.CardLastSeenDays[cardId.Value] = absoluteDay;
    }
}
