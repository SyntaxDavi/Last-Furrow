using UnityEngine;
using System.Collections.Generic;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// ScriptableObject que define uma Tradição.
    /// Tradições são "Jokers" que dão buffs passivos/ativos ao jogador.
    /// 
    /// DESIGN:
    /// - ID único para persistência
    /// - Múltiplos efeitos via composição (TraditionEffectBase[])
    /// - Visual separado das cartas normais
    /// - Compradas na loja, máximo de 5 ativas
    /// </summary>
    [CreateAssetMenu(fileName = "New Tradition", menuName = "Last Furrow/Traditions/Tradition Data")]
    public class TraditionData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName = "Nova Tradição";
        [SerializeField, TextArea(2, 5)] private string _description = "Descrição da tradição";
        
        [Header("Visual")]
        [SerializeField] private Sprite _icon;
        [SerializeField] private Sprite _cardFrame; // Moldura especial da tradição
        [SerializeField] private Color _glowColor = new Color(1f, 0.8f, 0.2f, 1f); // Dourado por padrão
        
        [Header("Rarity & Cost")]
        [SerializeField] private TraditionRarity _rarity = TraditionRarity.Common;
        [SerializeField] private int _baseShopPrice = 100;
        [SerializeField, Tooltip("Valor de venda manual. Se 0, usa metade do preço de compra.")]
        private int _baseSellValue = 0;
        
        [Header("Effects (Composição)")]
        [SerializeField] private TraditionEffectBase[] _effects;
        
        // --- Propriedades Públicas ---
        
        public string ID => string.IsNullOrEmpty(_id) ? name : _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public Sprite CardFrame => _cardFrame;
        public Color GlowColor => _glowColor;
        public TraditionRarity Rarity => _rarity;
        public int BaseShopPrice => _baseShopPrice;
        public IReadOnlyList<TraditionEffectBase> Effects => _effects;
        
        /// <summary>
        /// Gera descrição completa incluindo todos os efeitos.
        /// </summary>
        public string GetFullDescription()
        {
            if (_effects == null || _effects.Length == 0)
                return _description;
            
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(_description);
            sb.AppendLine();
            
            foreach (var effect in _effects)
            {
                if (effect != null)
                {
                    sb.AppendLine($"• {effect.GetDescription()}");
                }
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Retorna preço ajustado por raridade.
        /// </summary>
        public int GetAdjustedPrice()
        {
            float multiplier = _rarity switch
            {
                TraditionRarity.Common => 1.0f,
                TraditionRarity.Uncommon => 1.5f,
                TraditionRarity.Rare => 2.5f,
                TraditionRarity.Legendary => 5.0f,
                _ => 1.0f
            };
            
            return Mathf.RoundToInt(_baseShopPrice * multiplier);
        }
        
        /// <summary>
        /// Retorna valor de venda (para quando jogador vende manualmente).
        /// </summary>
        public int GetSellValue()
        {
            if (_baseSellValue > 0)
                return _baseSellValue;
            
            // Fallback: metade do preço de compra ajustado
            return Mathf.RoundToInt(GetAdjustedPrice() * 0.5f);
        }
        
        private void OnValidate()
        {
            // Auto-gera ID baseado no nome do asset se vazio
            if (string.IsNullOrEmpty(_id))
            {
                _id = name.ToUpperInvariant().Replace(" ", "_");
            }
        }
    }
    
    /// <summary>
    /// Raridade da tradição (afeta preço e chance de aparecer na loja).
    /// </summary>
    public enum TraditionRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }
}
