using UnityEngine;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// Exemplo de efeito PostDay: Ganha dinheiro baseado no score.
    /// Fase: PostDay (após resolução completa do dia)
    /// 
    /// EXEMPLO DE USO:
    /// "Investidor" - Ganha $1 para cada 10 pontos do dia
    /// </summary>
    [CreateAssetMenu(fileName = "ScoreToMoneyEffect", menuName = "Last Furrow/Traditions/Effects/PostDay/Score to Money")]
    public class ScoreToMoneyEffect : TraditionEffectBase
    {
        [Header("Effect Settings")]
        [SerializeField] private int _pointsPerMoney = 10;
        [SerializeField] private int _moneyPerThreshold = 1;
        
        private void OnEnable()
        {
            _phase = TraditionEffectPhase.PostDay;
            _effectName = "Score para Dinheiro";
        }
        
        public override string GetDescription()
        {
            return $"Ganha ${_moneyPerThreshold} para cada {_pointsPerMoney} pontos do dia";
        }
        
        public override int Evaluate(TraditionInstance tradition, TraditionEvaluationContext context)
        {
            if (context.FinalDayScore <= 0) return 0;
            
            int moneyEarned = (context.FinalDayScore / _pointsPerMoney) * _moneyPerThreshold;
            
            if (moneyEarned > 0)
            {
                // Adiciona dinheiro via RunData
                context.RunData.Money += moneyEarned;
                context.RunData.TotalMoneyEarned += moneyEarned;
                
                Debug.Log($"[ScoreToMoneyEffect] {context.FinalDayScore} pts / {_pointsPerMoney} = +${moneyEarned}");
                
                // Dispara evento se disponível
                // context.Events?.Economy?.OnMoneyEarned?.Invoke(moneyEarned);
            }
            
            return 0; // PostDay não retorna score, já foi aplicado
        }
    }
}
