using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// Implementação do serviço de tradições.
    /// 
    /// RESPONSABILIDADES (Service Layer):
    /// - Gerenciar TraditionLoadout
    /// - Coordenar TraditionEvaluator
    /// - Sincronizar com RunData (persistência)
    /// - Fornecer API para sistemas externos
    /// 
    /// NÃO FAZ:
    /// - Spawning de Views (TraditionViewManager)
    /// - Animações
    /// - Cálculos de preço (EconomyService)
    /// </summary>
    public class TraditionService : ITraditionService
    {
        private RunData _runData;
        private IGameLibrary _library;
        private GameEvents _events;
        private TraditionContext _context;
        
        private TraditionLoadout _loadout;
        private TraditionEvaluator _evaluator;
        
        public TraditionLoadout Loadout => _loadout;
        public TraditionEvaluator Evaluator => _evaluator;
        
        public TraditionService()
        {
            _loadout = new TraditionLoadout();
            _evaluator = new TraditionEvaluator(_loadout);
        }
        
        public void Configure(RunData runData, IGameLibrary library, GameEvents events)
        {
            _runData = runData;
            _library = library;
            _events = events;
            
            _context = new TraditionContext
            {
                RunData = runData,
                Events = events,
                Library = library
            };
            
            // Atualiza max slots da RunData
            _loadout.SetMaxSlots(runData.MaxTraditionSlots);
        }
        
        public void Initialize()
        {
            if (_runData == null || _library == null)
            {
                Debug.LogError("[TraditionService] Not configured! Call Configure first.");
                return;
            }
            
            // Carrega tradições da RunData
            LoadFromRunData();
            
            // Inicializa loadout
            _loadout.Initialize(_context, _library);
            
            Debug.Log($"[TraditionService] Initialized with {_loadout.Count} traditions");
        }
        
        public bool TryAddTradition(TraditionID id)
        {
            if (!_library.TryGetTradition(id, out var data))
            {
                Debug.LogWarning($"[TraditionService] Tradition not found: {id}");
                return false;
            }
            
            return TryAddTradition(data);
        }
        
        public bool TryAddTradition(TraditionData data)
        {
            if (data == null) return false;
            
            if (!_loadout.CanAdd)
            {
                Debug.LogWarning($"[TraditionService] Cannot add - max slots reached ({_loadout.MaxSlots})");
                return false;
            }
            
            if (_loadout.Contains(data.ID))
            {
                Debug.LogWarning($"[TraditionService] Already has tradition: {data.ID}");
                return false;
            }
            
            if (_loadout.TryAdd(data, out var instance))
            {
                SaveToRunData();
                Debug.Log($"[TraditionService] Added: {data.DisplayName}");
                return true;
            }
            
            return false;
        }
        
        public bool TryRemoveTradition(TraditionID id)
        {
            if (_loadout.TryRemove(id))
            {
                SaveToRunData();
                Debug.Log($"[TraditionService] Removed: {id}");
                return true;
            }
            return false;
        }
        
        public void SaveToRunData()
        {
            if (_runData == null) return;
            
            _runData.ActiveTraditionIDs = _loadout.GetIDsForSave();
            _runData.MaxTraditionSlots = _loadout.MaxSlots;
        }
        
        public int GetPersistentModifier(PersistentModifierType type)
        {
            return _evaluator.GetPersistentModifier(type);
        }
        
        // --- Helpers ---
        
        private void LoadFromRunData()
        {
            if (_runData.ActiveTraditionIDs == null) return;
            
            _loadout = TraditionLoadout.FromSavedIDs(
                _runData.ActiveTraditionIDs,
                _runData.MaxTraditionSlots
            );
            
            _evaluator = new TraditionEvaluator(_loadout);
        }
    }
}
