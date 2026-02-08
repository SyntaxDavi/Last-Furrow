using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implementação de ICardSourceStrategy e IRunDeckSource que usa o RunDeck.
/// 
/// ARQUITETURA:
/// - Consome cartas do RunDeck (fila pré-embaralhada)
/// - Aplica DrawValidationPolicy para validar/modificar draws
/// - Lê configurações de regras da CardDropLibrarySO
/// 
/// REGRAS APLICADAS (configuradas em cada CardDropData):
/// - MaxPerDraw: Limite de duplicatas por draw
/// - GuaranteeAfterDays: Garante cartas após X dias
/// </summary>
public class RunDeckSource : ICardSourceStrategy, IRunDeckSource
{
    private readonly RunDeck _deck;
    private readonly DrawValidationPolicy _validationPolicy;
    
    /// <summary>
    /// Construtor recomendado: usa biblioteca para configuração via Inspector.
    /// </summary>
    /// <param name="deck">Deck embaralhado da run</param>
    /// <param name="library">Biblioteca com configurações de cartas</param>
    public RunDeckSource(RunDeck deck, CardDropLibrarySO library)
        : this(deck, library != null 
            ? DrawValidationPolicy.CreateFromLibrary(library) 
            : DrawValidationPolicy.CreateDefault())
    {
    }
    
    /// <summary>
    /// Construtor legado: cria policy com defaults (sem Inspector).
    /// </summary>
    public RunDeckSource(RunDeck deck) 
        : this(deck, DrawValidationPolicy.CreateDefault())
    {
    }
    
    /// <summary>
    /// Construtor com política customizada (para testes).
    /// </summary>
    public RunDeckSource(RunDeck deck, DrawValidationPolicy validationPolicy)
    {
        _deck = deck;
        _validationPolicy = validationPolicy ?? DrawValidationPolicy.CreateDefault();
        
        Debug.Log($"[RunDeckSource] Inicializado com {_validationPolicy.RuleCount} regras: " +
                  $"[{string.Join(", ", _validationPolicy.GetRuleNames())}]");
    }

    public int RemainingCards => _deck?.Remaining ?? 0;

    // IRunDeckSource - Draw simples (sem validação)
    public CardID DrawNext()
    {
        if (_deck == null)
        {
            Debug.LogError("[RunDeckSource] RunDeck é NULL!");
            return default;
        }
        return _deck.Draw();
    }

    // IRunDeckSource - Draw múltiplo (sem validação)
    public List<CardID> DrawNext(int amount)
    {
        if (_deck == null)
        {
            Debug.LogError("[RunDeckSource] RunDeck é NULL!");
            return new List<CardID>();
        }
        return _deck.Draw(amount);
    }

    // ICardSourceStrategy (principal - com validação)
    public List<CardID> GetNextCardIDs(int amount, RunData currentRun)
    {
        if (_deck == null)
        {
            Debug.LogError("[RunDeckSource] RunDeck é NULL!");
            return new List<CardID>();
        }
        
        // 1. Draw bruto do deck
        var rawDraw = _deck.Draw(amount);
        
        if (rawDraw.Count == 0)
        {
            Debug.LogWarning("[RunDeckSource] Deck vazio! Nenhuma carta disponível.");
            return rawDraw;
        }
        
        // 2. Aplica validação se temos RunData
        if (_validationPolicy != null && currentRun != null)
        {
            var context = new DrawRuleContext(this, currentRun, amount);
            var validatedDraw = _validationPolicy.Validate(rawDraw, context);
            
            Debug.Log($"[RunDeckSource] Draw validado: {rawDraw.Count} → {validatedDraw.Count} cartas");
            return validatedDraw;
        }
        
        return rawDraw;
    }
}