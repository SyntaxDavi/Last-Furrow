using UnityEngine;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// Exemplo de efeito FinalMultiplier: Adiciona porcentagem ao score total.
    /// Fase: FinalMultiplier (última antes de aplicar score)
    /// 
    /// EXEMPLO DE USO:
    /// "Colheita Abundante" - +15% ao score final
    /// </summary>
    [CreateAssetMenu(fileName = "PercentageBonusEffect", menuName = "Last Furrow/Traditions/Effects/Final/Percentage Bonus")]
    public class PercentageBonusEffect : TraditionEffectBase
    {
        [Header("Effect Settings")]
        [SerializeField, Range(1, 100)] 
        private int _percentageBonus = 15;
        
        private void OnEnable()
        {
            _phase = TraditionEffectPhase.FinalMultiplier;
            _effectName = "Bônus Percentual";
        }
        
        public override string GetDescription()
        {
            return $"+{_percentageBonus}% ao score final do dia";
        }
        
        public override int Evaluate(TraditionInstance tradition, TraditionEvaluationContext context)
        {
            int currentTotal = context.CurrentTotalScore;
            int bonus = Mathf.RoundToInt(currentTotal * (_percentageBonus / 100f));
            
            Debug.Log($"[PercentageBonusEffect] {currentTotal} x {_percentageBonus}% = +{bonus}");
            return bonus;
        }
    }
}
