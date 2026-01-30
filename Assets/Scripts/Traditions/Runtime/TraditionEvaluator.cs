using System.Collections.Generic;
using UnityEngine;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// Avalia tradições durante a análise de dia.
    /// 
    /// RESPONSABILIDADES:
    /// - Coordenar avaliação por fase
    /// - Manter contexto de avaliação
    /// - Consultar modificadores persistentes
    /// 
    /// ORDEM DE AVALIAÇÃO:
    /// PreScoring → (Sistema) → OnPatternDetected → (Sistema) → 
    /// PostPattern → FinalMultiplier → (Sistema) → PostDay
    /// 
    /// NOTA: Usa ExecutionPriority das instâncias, NÃO ordem visual.
    /// </summary>
    public class TraditionEvaluator
    {
        private readonly TraditionLoadout _loadout;
        private TraditionEvaluationContext _currentContext;
        
        public TraditionEvaluator(TraditionLoadout loadout)
        {
            _loadout = loadout;
        }
        
        /// <summary>
        /// Inicia a avaliação do dia.
        /// </summary>
        public void StartDayEvaluation(RunData runData, IGridService gridService, GameEvents events)
        {
            _currentContext = new TraditionEvaluationContext
            {
                RunData = runData,
                GridService = gridService,
                Events = events,
                CurrentDay = runData.CurrentDay,
                CurrentWeek = runData.CurrentWeek,
                PassiveScore = 0,
                PatternScore = 0,
                TotalPatternsCompleted = 0
            };
        }
        
        /// <summary>
        /// Fase 1: PreScoring
        /// </summary>
        public int EvaluatePreScoring()
        {
            return EvaluatePhase(TraditionEffectPhase.PreScoring);
        }
        
        /// <summary>
        /// Fase 2: OnPatternDetected (para cada padrão)
        /// </summary>
        public int EvaluatePatternDetected(PatternMatch pattern)
        {
            if (_loadout == null || _currentContext == null) return 0;
            
            int totalBonus = 0;
            
            foreach (var tradition in _loadout.GetByExecutionOrder())
            {
                if (!tradition.IsActive || tradition.Data == null) continue;
                
                foreach (var effect in tradition.Data.Effects)
                {
                    if (effect != null && effect.Phase == TraditionEffectPhase.OnPatternDetected)
                    {
                        totalBonus += effect.EvaluatePattern(tradition, _currentContext, pattern);
                    }
                }
            }
            
            _currentContext.TotalPatternsCompleted++;
            return totalBonus;
        }
        
        public void SetPassiveScore(int score) => _currentContext.PassiveScore = score;
        public void SetPatternScore(int score) => _currentContext.PatternScore = score;
        
        /// <summary>
        /// Fase 3: PostPattern
        /// </summary>
        public int EvaluatePostPattern()
        {
            return EvaluatePhase(TraditionEffectPhase.PostPattern);
        }
        
        /// <summary>
        /// Fase 4: FinalMultiplier
        /// </summary>
        public int EvaluateFinalMultiplier()
        {
            return EvaluatePhase(TraditionEffectPhase.FinalMultiplier);
        }
        
        /// <summary>
        /// Fase 5: PostDay
        /// </summary>
        public void EvaluatePostDay(int finalScore, bool metGoal)
        {
            if (_currentContext != null)
            {
                _currentContext.FinalDayScore = finalScore;
                _currentContext.MetWeeklyGoal = metGoal;
            }
            EvaluatePhase(TraditionEffectPhase.PostDay);
        }
        
        public void EndDayEvaluation()
        {
            _currentContext = null;
        }
        
        // --- Modificadores Persistentes ---
        
        public int GetPersistentModifier(PersistentModifierType type)
        {
            if (_loadout == null) return 0;
            
            int total = 0;
            
            foreach (var tradition in _loadout.GetByExecutionOrder())
            {
                if (!tradition.IsActive || tradition.Data == null) continue;
                
                foreach (var effect in tradition.Data.Effects)
                {
                    if (effect is IPersistentModifier persistent)
                    {
                        total += persistent.GetModifier(type);
                    }
                }
            }
            
            return total;
        }
        
        // --- Helpers ---
        
        private int EvaluatePhase(TraditionEffectPhase phase)
        {
            if (_loadout == null || _currentContext == null) return 0;
            
            int total = 0;
            
            // Avalia por ExecutionPriority, não por ordem visual
            foreach (var tradition in _loadout.GetByExecutionOrder())
            {
                if (!tradition.IsActive || tradition.Data == null) continue;
                
                foreach (var effect in tradition.Data.Effects)
                {
                    if (effect != null && effect.Phase == phase)
                    {
                        total += effect.Evaluate(tradition, _currentContext);
                    }
                }
            }
            
            return total;
        }
    }
}
