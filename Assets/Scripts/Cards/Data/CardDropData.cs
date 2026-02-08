using UnityEngine;

/// <summary>
/// Configuração de drop de uma carta no RunDeck.
/// Define peso, raridade e regras de quando a carta pode aparecer.
/// 
/// DESIGN: Cada carta tem uma instância de CardDropData que define seu comportamento
/// no sistema de economia da run.
/// 
/// SEÇÕES:
/// - Identificação: CardID único
/// - Balanceamento: Peso, raridade, cópias no deck
/// - Draw Rules: Regras por draw diário (duplicatas, garantias)
/// - Categorização: Tags para filtros
/// </summary>
[CreateAssetMenu(fileName = "New Card Drop", menuName = "Last Furrow/Card Drop Data")]
public class CardDropData : ScriptableObject
{
    [Header("Identificação")]
    [Tooltip("ID da carta que esta configuração representa.")]
    public CardID CardID;

    [Header("Balanceamento de Deck")]
    [Tooltip("Peso base no deck. Maior peso = mais cópias no deck embaralhado.")]
    [Min(1)] public int Weight = 1;

    [Tooltip("Raridade da carta. Afeta multiplicadores e filtros.")]
    public CardRarity Rarity = CardRarity.Common;

    [Tooltip("Máximo de cópias desta carta que podem existir no deck de uma run.")]
    [Min(1)] public int MaxCopiesInDeck = 5;

    [Header("Regras de Draw Diário")]
    [Tooltip("Máximo desta carta por draw diário. 0 = usa default global (2).")]
    [Range(0, 5)] public int MaxPerDraw = 0;

    [Tooltip("Garantir esta carta após X DRAWS sem aparecer (conta dias de produção, não dias da semana). 0 = nunca garantir.")]
    [Range(0, 20)] public int GuaranteeAfterDays = 0;

    [Tooltip("Prioridade de garantia. Menor = garante primeiro quando múltiplas cartas precisam aparecer.")]
    [Range(1, 100)] public int GuaranteePriority = 50;

    [Header("Categorização")]
    [Tooltip("Tags para filtros contextuais (estação seca, etc).")]
    public CardTag Tags = CardTag.None;

    // ===== PROPRIEDADES AUXILIARES =====

    /// <summary>
    /// Retorna true se esta carta tem limite de duplicatas customizado.
    /// </summary>
    public bool HasCustomMaxPerDraw => MaxPerDraw > 0;

    /// <summary>
    /// Retorna true se esta carta deve ser garantida após X dias.
    /// </summary>
    public bool HasGuarantee => GuaranteeAfterDays > 0;

    /// <summary>
    /// Retorna o limite efetivo de duplicatas por draw.
    /// </summary>
    /// <param name="globalDefault">Valor default se esta carta não tiver customização.</param>
    public int GetEffectiveMaxPerDraw(int globalDefault = 2)
    {
        return HasCustomMaxPerDraw ? MaxPerDraw : globalDefault;
    }

    /// <summary>
    /// Verifica se a carta possui uma tag específica.
    /// </summary>
    public bool HasTag(CardTag tag)
    {
        return (Tags & tag) == tag;
    }
}