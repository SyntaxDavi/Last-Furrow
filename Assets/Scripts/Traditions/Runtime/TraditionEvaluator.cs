using System.Collections.Generic;
using UnityEngine;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// Serviço que avalia tradições durante a análise de dia.
    /// Chamado pelo pipeline de resolução diária nas fases apropriadas.
    /// 
    /// USO:
    /// 1. Chamar StartDayEvaluation() no início da análise
    /// 2. Chamar cada fase na ordem correta
    /// 3. Chamar EndDayEvaluation() no final
    /// 
    /// A ordem de avaliação é:
    /// PreScoring → (Sistema calcula passivos) → 
    /// OnPatternDetected (para cada padrão) → (Sistema pontua padrões) →
    /// PostPattern → FinalMultiplier → (Sistema aplica score) → PostDay
    /// </summary>
    public class TraditionEvaluator
    {
        private readonly TraditionManager _manager;
        private TraditionEvaluationContext _currentContext;
        
        public TraditionEvaluator(TraditionManager manager)
        {
            _manager = manager;
        }
        
        /// <summary>
        /// Inicia a avaliação do dia. Cria o contexto de avaliação.
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
            
            Debug.Log($"[TraditionEvaluator] Started day evaluation with {_manager.ActiveCount} traditions");
        }
        
        /// <summary>
        /// Fase 1: PreScoring - Modifica valores base antes de calcular passivos.
        /// </summary>
        /// <returns>Modificador aditivo para o score passivo</returns>
        public int EvaluatePreScoring()
        {
            return EvaluatePhase(TraditionEffectPhase.PreScoring);
        }
        
        /// <summary>
        /// Fase 2: OnPatternDetected - Chamado para cada padrão encontrado.
        /// </summary>
        /// <returns>Modificador aditivo para este padrão específico</returns>
        public int EvaluatePatternDetected(PatternMatchResult pattern)
        {
            if (_manager == null || _currentContext == null) return 0;
            
            int totalBonus = 0;
            
            foreach (var tradition in _manager.ActiveTraditions)
            {
                if (!tradition.IsActive || tradition.Data == null) continue;
                
                foreach (var effect in tradition.Data.Effects)
                {
                    if (effect != null && effect.Phase == TraditionEffectPhase.OnPatternDetected)
                    {
                        int bonus = effect.EvaluatePattern(tradition, _currentContext, pattern);
                        totalBonus += bonus;
                        
                        if (bonus != 0)
                        {
                            Debug.Log($"[TraditionEvaluator] {tradition.Data.DisplayName} -> Pattern bonus: +{bonus}");
                        }
                    }
                }
            }
            
            _currentContext.TotalPatternsCompleted++;
            return totalBonus;
        }
        
        /// <summary>
        /// Atualiza o contexto com o score passivo calculado pelo sistema.
        /// Chamar depois de calcular os pontos passivos.
        /// </summary>
        public void SetPassiveScore(int passiveScore)
        {
            if (_currentContext != null)
            {
                _currentContext.PassiveScore = passiveScore;
            }
        }
        
        /// <summary>
        /// Atualiza o contexto com o score de padrões calculado pelo sistema.
        /// Chamar depois de calcular todos os padrões.
        /// </summary>
        public void SetPatternScore(int patternScore)
        {
            if (_currentContext != null)
            {
                _currentContext.PatternScore = patternScore;
            }
        }
        
        /// <summary>
        /// Fase 3: PostPattern - Bônus condicionais após todos os padrões.
        /// </summary>
        public int EvaluatePostPattern()
        {
            return EvaluatePhase(TraditionEffectPhase.PostPattern);
        }
        
        /// <summary>
        /// Fase 4: FinalMultiplier - Multiplicadores finais no score total.
        /// Esta é a fase mais comum para tradições.
        /// </summary>
        public int EvaluateFinalMultiplier()
        {
            return EvaluatePhase(TraditionEffectPhase.FinalMultiplier);
        }
        
        /// <summary>
        /// Fase 5: PostDay - Efeitos de "fim de turno".
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
        
        /// <summary>
        /// Finaliza a avaliação do dia.
        /// </summary>
        public void EndDayEvaluation()
        {
            _currentContext = null;
            Debug.Log("[TraditionEvaluator] Day evaluation ended");
        }
        
        // --- Helpers ---
        
        private int EvaluatePhase(TraditionEffectPhase phase)
        {
            if (_manager == null || _currentContext == null) return 0;
            
            int totalBonus = 0;
            
            // Avalia tradições na ordem em que aparecem (ordem importa!)
            foreach (var tradition in _manager.ActiveTraditions)
            {
                if (!tradition.IsActive || tradition.Data == null) continue;
                
                foreach (var effect in tradition.Data.Effects)
                {
                    if (effect != null && effect.Phase == phase)
                    {
                        int bonus = effect.Evaluate(tradition, _currentContext);
                        totalBonus += bonus;
                        
                        if (bonus != 0)
                        {
                            Debug.Log($"[TraditionEvaluator] {tradition.Data.DisplayName} ({phase}): +{bonus}");
                        }
                    }
                }
            }
            
            return totalBonus;
        }
        
        // --- Para Efeitos Persistent ---
        
        /// <summary>
        /// Consulta se há algum modificador Persistent para um valor específico.
        /// Usado por sistemas externos (Shop, Hand, etc).
        /// </summary>
        public int GetPersistentModifier(PersistentModifierType type)
        {
            if (_manager == null) return 0;
            
            int total = 0;
            
            foreach (var tradition in _manager.ActiveTraditions)
            {
                if (!tradition.IsActive || tradition.Data == null) continue;
                
                foreach (var effect in tradition.Data.Effects)
                {
                    if (effect != null && effect.Phase == TraditionEffectPhase.Persistent)
                    {
                        if (effect is IPersistentModifier persistent)
                        {
                            total += persistent.GetModifier(type);
                        }
                    }
                }
            }
            
            return total;
        }
    }
    
    /// <summary>
    /// Tipos de modificadores persistentes que tradições podem fornecer.
    /// Usado por sistemas externos para consultar bônus ativos.
    /// </summary>
    public enum PersistentModifierType
    {
        /// <summary>Modificador de preço na loja (negativo = desconto)</summary>
        ShopPriceModifier,
        
        /// <summary>Modificador de tamanho máximo da mão</summary>
        MaxHandSize,
        
        /// <summary>Modificador de cartas compradas por dia</summary>
        CardsPerDay,
        
        /// <summary>Modificador de dinheiro ganho ao colher</summary>
        HarvestMoneyBonus,
        
        /// <summary>Modificador de vidas máximas</summary>
        MaxLives
    }
    
    /// <summary>
    /// Interface para efeitos que fornecem modificadores persistentes.
    /// Implementada por efeitos com Phase = Persistent.
    /// </summary>
    public interface IPersistentModifier
    {
        int GetModifier(PersistentModifierType type);
    }
}
