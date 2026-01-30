using System.Collections.Generic;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// Interface para o serviço de tradições.
    /// Service Layer - contém lógica de negócio sem dependências visuais.
    /// </summary>
    public interface ITraditionService
    {
        /// <summary>
        /// Loadout atual do jogador.
        /// </summary>
        TraditionLoadout Loadout { get; }
        
        /// <summary>
        /// Avaliador de efeitos de tradição.
        /// </summary>
        TraditionEvaluator Evaluator { get; }
        
        /// <summary>
        /// Configura o serviço com dependências.
        /// </summary>
        void Configure(RunData runData, IGameLibrary library, GameEvents events);
        
        /// <summary>
        /// Inicializa o loadout a partir da RunData.
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Tenta adicionar uma tradição ao loadout.
        /// </summary>
        bool TryAddTradition(TraditionID id);
        bool TryAddTradition(TraditionData data);
        
        /// <summary>
        /// Remove uma tradição do loadout.
        /// </summary>
        bool TryRemoveTradition(TraditionID id);
        
        /// <summary>
        /// Persiste o estado atual na RunData.
        /// </summary>
        void SaveToRunData();
        
        /// <summary>
        /// Obtém modificador persistente (para sistemas externos como Shop).
        /// </summary>
        int GetPersistentModifier(PersistentModifierType type);
    }
}
