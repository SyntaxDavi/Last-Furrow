using UnityEngine;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// Base abstrata para efeitos de Tradição como ScriptableObjects.
    /// Cada tipo de efeito herda desta classe.
    /// 
    /// ARQUITETURA:
    /// - TraditionData contém uma lista de TraditionEffectBase
    /// - Permite composição: uma tradição pode ter múltiplos efeitos
    /// - Cada efeito é configurável no Inspector
    /// - Novos efeitos são adicionados criando novas classes derivadas
    /// 
    /// FASES DE AVALIAÇÃO:
    /// Ver documentação em TraditionEffectPhase para ordem de execução.
    /// </summary>
    public abstract class TraditionEffectBase : ScriptableObject, ITraditionEffect
    {
        [Header("Effect Metadata")]
        [SerializeField] protected string _effectName = "New Effect";
        [SerializeField, TextArea(2, 4)] protected string _descriptionTemplate = "Effect description with {0} placeholder";
        
        [Header("Evaluation Phase")]
        [SerializeField, Tooltip("Em qual fase da análise este efeito é avaliado")]
        protected TraditionEffectPhase _phase = TraditionEffectPhase.FinalMultiplier;
        
        public string EffectName => _effectName;
        public TraditionEffectPhase Phase => _phase;
        
        /// <summary>
        /// Retorna a descrição formatada do efeito.
        /// Override para injetar valores específicos (ex: "+{0}% de desconto" -> "+15% de desconto").
        /// </summary>
        public virtual string GetDescription()
        {
            return _descriptionTemplate;
        }
        
        /// <summary>
        /// Inicializa o efeito quando a tradição é equipada.
        /// Chamado uma vez ao adicionar a tradição.
        /// IMPORTANTE: Para efeitos Persistent, registre-se em eventos aqui.
        /// </summary>
        public virtual void OnEquip(TraditionInstance tradition, TraditionContext context) { }
        
        /// <summary>
        /// Limpa o efeito quando a tradição é removida.
        /// IMPORTANTE: Para efeitos Persistent, desregistre de eventos aqui.
        /// </summary>
        public virtual void OnUnequip(TraditionInstance tradition, TraditionContext context) { }
        
        /// <summary>
        /// Chamado quando a fase deste efeito é atingida durante análise.
        /// Retorna modificação de score (pode ser 0).
        /// </summary>
        /// <param name="tradition">Instância da tradição</param>
        /// <param name="context">Contexto com RunData, score atual, etc</param>
        /// <returns>Modificador de score (pode ser aditivo ou multiplicativo dependendo da implementação)</returns>
        public virtual int Evaluate(TraditionInstance tradition, TraditionEvaluationContext context)
        {
            return 0;
        }
        
        /// <summary>
        /// Para efeitos OnPatternDetected: chamado para cada padrão encontrado.
        /// </summary>
        public virtual int EvaluatePattern(TraditionInstance tradition, TraditionEvaluationContext context, PatternMatchResult pattern)
        {
            return 0;
        }
    }
    
    /// <summary>
    /// Contexto base passado para OnEquip/OnUnequip.
    /// Contém referências para sistemas que efeitos Persistent podem usar.
    /// </summary>
    public class TraditionContext
    {
        public RunData RunData { get; set; }
        public GameEvents Events { get; set; }
        public IGameLibrary Library { get; set; }
        
        // Sistemas que efeitos Persistent podem modificar
        public IEconomyService Economy { get; set; }
        public IGridService GridService { get; set; }
    }
    
    /// <summary>
    /// Contexto passado durante avaliação de efeitos na análise.
    /// Contém estado do momento da avaliação.
    /// </summary>
    public class TraditionEvaluationContext
    {
        // Estado da Run
        public RunData RunData { get; set; }
        public GameEvents Events { get; set; }
        public IGridService GridService { get; set; }
        
        // Estado do Dia Atual
        public int CurrentDay { get; set; }
        public int CurrentWeek { get; set; }
        
        // Estado da Análise (atualizado durante as fases)
        public int PassiveScore { get; set; }
        public int PatternScore { get; set; }
        public int TotalPatternsCompleted { get; set; }
        public int CurrentTotalScore => PassiveScore + PatternScore;
        
        // Para PostDay
        public int FinalDayScore { get; set; }
        public bool MetWeeklyGoal { get; set; }
    }
}
