using UnityEngine;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// Efeito de exemplo: Placeholder sem efeito real.
    /// Usado para criar tradições de teste antes de implementar efeitos reais.
    /// </summary>
    [CreateAssetMenu(fileName = "PlaceholderEffect", menuName = "Last Furrow/Traditions/Effects/Placeholder")]
    public class PlaceholderEffect : TraditionEffectBase
    {
        [Header("Placeholder Settings")]
        [SerializeField] private string _placeholderValue = "???";
        [SerializeField] private int _testBonus = 10;
        
        public override string GetDescription()
        {
            return string.Format(_descriptionTemplate, _placeholderValue);
        }
        
        public override void OnEquip(TraditionInstance tradition, TraditionContext context)
        {
            Debug.Log($"[PlaceholderEffect] Equipped: {_effectName}");
        }
        
        public override void OnUnequip(TraditionInstance tradition, TraditionContext context)
        {
            Debug.Log($"[PlaceholderEffect] Unequipped: {_effectName}");
        }
        
        public override int Evaluate(TraditionInstance tradition, TraditionEvaluationContext context)
        {
            Debug.Log($"[PlaceholderEffect] Evaluated: {_effectName} at phase {_phase}, bonus: +{_testBonus}");
            return _testBonus;
        }
        
        public override int EvaluatePattern(TraditionInstance tradition, TraditionEvaluationContext context, PatternMatchResult pattern)
        {
            Debug.Log($"[PlaceholderEffect] Pattern evaluated: {pattern?.PatternID ?? "Unknown"}");
            return 0;
        }
    }
}
