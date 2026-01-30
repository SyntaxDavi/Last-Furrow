using UnityEngine;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// Exemplo de efeito Persistent: Modifica preço da loja.
    /// Fase: Persistent (sempre ativo enquanto equipado)
    /// 
    /// EXEMPLO DE USO:
    /// "Mercador Amigo" - Itens na loja custam 10% menos
    /// </summary>
    [CreateAssetMenu(fileName = "ShopDiscountEffect", menuName = "Last Furrow/Traditions/Effects/Persistent/Shop Discount")]
    public class ShopDiscountEffect : TraditionEffectBase, IPersistentModifier
    {
        [Header("Effect Settings")]
        [SerializeField, Range(1, 50)] 
        private int _discountPercentage = 10;
        
        private void OnEnable()
        {
            _phase = TraditionEffectPhase.Persistent;
            _effectName = "Desconto na Loja";
        }
        
        public override string GetDescription()
        {
            return $"Itens na loja custam {_discountPercentage}% menos";
        }
        
        public override void OnEquip(TraditionInstance tradition, TraditionContext context)
        {
            Debug.Log($"[ShopDiscountEffect] Ativado: -{_discountPercentage}% na loja");
            // O efeito é consultado sob demanda via IPersistentModifier
        }
        
        public override void OnUnequip(TraditionInstance tradition, TraditionContext context)
        {
            Debug.Log($"[ShopDiscountEffect] Desativado");
        }
        
        /// <summary>
        /// Retorna modificador para o tipo solicitado.
        /// ShopPriceModifier negativo = desconto.
        /// </summary>
        public int GetModifier(PersistentModifierType type)
        {
            if (type == PersistentModifierType.ShopPriceModifier)
            {
                return -_discountPercentage; // Negativo = desconto
            }
            
            return 0;
        }
    }
}
