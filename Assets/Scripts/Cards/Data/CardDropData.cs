using UnityEngine;

/// <summary>
/// Configuração de drop de uma carta no RunDeck.
/// Define peso, raridade e regras de quando a carta pode aparecer.
/// 
/// DESIGN: Cada carta tem uma instância de CardDropData que define seu comportamento
/// no sistema de economia da run.
/// </summary>
[CreateAssetMenu(fileName = "New Card Drop", menuName = "Last Furrow/Card Drop Data")]
public class CardDropData : ScriptableObject
{
    [Header("Identificação")]
    [Tooltip("ID da carta que esta configuração representa.")]
    public CardID CardID;

    [Header("Balanceamento")]
    [Tooltip("Peso base no deck. Maior peso = mais cópias no deck embaralhado.")]
    [Min(1)] public int Weight = 1;

    [Tooltip("Raridade da carta. Afeta multiplicadores e filtros.")]
    public CardRarity Rarity = CardRarity.Common;

    [Tooltip("Máximo de cópias desta carta que podem existir no deck de uma run.")]
    [Min(1)] public int MaxCopiesInDeck = 5;

    [Header("Categorização")]
    [Tooltip("Tags para filtros contextuais (estação seca, etc).")]
    public CardTag Tags = CardTag.None;

    /// <summary>
    /// Verifica se a carta possui uma tag específica.
    /// </summary>
    public bool HasTag(CardTag tag)
    {
        return (Tags & tag) == tag;
    }
}
