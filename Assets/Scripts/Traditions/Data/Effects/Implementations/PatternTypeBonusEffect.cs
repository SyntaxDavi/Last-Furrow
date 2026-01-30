using UnityEngine;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// Exemplo de efeito OnPatternDetected: Bônus para tipos específicos de padrão.
    /// Fase: OnPatternDetected (durante detecção de cada padrão)
    /// 
    /// EXEMPLO DE USO:
    /// "Mestre das Linhas" - Padrões de linha dão +5 pontos extras
    /// </summary>
    [CreateAssetMenu(fileName = "PatternTypeBonusEffect", menuName = "Last Furrow/Traditions/Effects/Pattern/Type Bonus")]
    public class PatternTypeBonusEffect : TraditionEffectBase
    {
        [Header("Effect Settings")]
        [SerializeField] private string _targetPatternID = "FULL_ROW";
        [SerializeField] private int _bonusPoints = 5;
        
        private void OnEnable()
        {
            _phase = TraditionEffectPhase.OnPatternDetected;
            _effectName = "Bônus de Padrão";
        }
        
        public override string GetDescription()
        {
            return $"Padrões '{_targetPatternID}' dão +{_bonusPoints} pontos extras";
        }
        
        public override int EvaluatePattern(TraditionInstance tradition, TraditionEvaluationContext context, PatternMatchResult pattern)
        {
            if (pattern == null) return 0;
            
            // Verifica se é o padrão alvo
            if (pattern.PatternID == _targetPatternID)
            {
                Debug.Log($"[PatternTypeBonusEffect] Matched {_targetPatternID}: +{_bonusPoints}");
                return _bonusPoints;
            }
            
            return 0;
        }
    }
}
