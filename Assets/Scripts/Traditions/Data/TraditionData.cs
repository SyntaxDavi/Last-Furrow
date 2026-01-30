using UnityEngine;
using System.Collections.Generic;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// ScriptableObject que define uma Tradição.
    /// 
    /// REGRA ARQUITETURAL:
    /// Este é um DADO ESTÁTICO PURO. Define O QUE É a tradição.
    /// NÃO contém lógica de pricing, economia, ou cálculos.
    /// 
    /// QUEM DECIDE VALORES:
    /// - Preço de compra → EconomyService / ShopPricingService
    /// - Preço de venda → EconomyService
    /// - Descrição formatada → UI Layer (TraditionTooltip)
    /// </summary>
    [CreateAssetMenu(fileName = "New Tradition", menuName = "Last Furrow/Traditions/Tradition Data")]
    public class TraditionData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private TraditionID _id;
        [SerializeField] private string _displayName = "Nova Tradição";
        [SerializeField, TextArea(2, 5)] private string _description = "Descrição da tradição";
        
        [Header("Visual")]
        [SerializeField] private Sprite _icon;
        [SerializeField] private Sprite _cardFrame;
        [SerializeField] private Color _glowColor = new Color(1f, 0.8f, 0.2f, 1f);
        
        [Header("Base Values (Economia decide o resto)")]
        [SerializeField] private TraditionRarity _rarity = TraditionRarity.Common;
        [SerializeField, Tooltip("Valor base para cálculos de economia. Serviço decide preço final.")]
        private int _baseValue = 100;
        
        [Header("Effects")]
        [SerializeField] private TraditionEffectBase[] _effects;
        
        // --- Propriedades Públicas (Somente Leitura) ---
        
        public TraditionID ID => _id.IsValid ? _id : new TraditionID(name);
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public Sprite CardFrame => _cardFrame;
        public Color GlowColor => _glowColor;
        public TraditionRarity Rarity => _rarity;
        public int BaseValue => _baseValue;
        public IReadOnlyList<TraditionEffectBase> Effects => _effects;
        
        // Removemos o OnValidate que sobrescrevia o ID automaticamente para dar mais liberdade ao designer
        // O getter acima já cuida de retornar o nome do asset se o ID estiver vazio.
    }
    
    /// <summary>
    /// Raridade da tradição. Usado por sistemas de economia e loja.
    /// </summary>
    public enum TraditionRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }
}
