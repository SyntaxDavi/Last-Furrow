using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// Instância runtime de uma Tradição equipada pelo jogador.
    /// 
    /// RESPONSABILIDADES:
    /// - Manter estado runtime (stacks, counters, cooldowns)
    /// - Gerenciar lifecycle (OnAdded, OnRemoved, OnTurnStart)
    /// - Isolar efeitos por instância
    /// - Definir prioridade de execução (NÃO posição visual)
    /// 
    /// ARQUITETURA:
    /// - TraditionData = template imutável (ScriptableObject)
    /// - TraditionInstance = estado mutável (runtime)
    /// - Ordem de efeitos = _executionPriority (não posição na UI)
    /// </summary>
    [Serializable]
    public class TraditionInstance
    {
        [SerializeField] private TraditionID _traditionID;
        [SerializeField] private string _instanceID;
        [SerializeField] private int _executionPriority;
        
        // Estado Runtime (não serializado)
        [NonSerialized] private TraditionData _cachedData;
        [NonSerialized] private bool _isActive = true;
        [NonSerialized] private Dictionary<string, int> _counters;
        [NonSerialized] private Dictionary<string, float> _timers;
        [NonSerialized] private bool _isInitialized;
        
        // --- Propriedades Públicas ---
        
        public TraditionID TraditionID => _traditionID;
        public string InstanceID => _instanceID;
        
        /// <summary>
        /// Prioridade de execução dos efeitos. MENOR = executa primeiro.
        /// Independente da posição visual na UI.
        /// </summary>
        public int ExecutionPriority
        {
            get => _executionPriority;
            set => _executionPriority = value;
        }
        
        public TraditionData Data => _cachedData;
        public bool IsActive => _isActive;
        public bool IsInitialized => _isInitialized;
        
        // --- Construtores ---
        
        public TraditionInstance() 
        {
            _counters = new Dictionary<string, int>();
            _timers = new Dictionary<string, float>();
        }
        
        public TraditionInstance(TraditionData data, int priority = 0) : this()
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            
            _traditionID = data.ID;
            _instanceID = GenerateInstanceID();
            _executionPriority = priority;
            _cachedData = data;
        }
        
        public TraditionInstance(TraditionID id, int priority = 0) : this()
        {
            _traditionID = id;
            _instanceID = GenerateInstanceID();
            _executionPriority = priority;
        }
        
        // --- Lifecycle Hooks ---
        
        /// <summary>
        /// Chamado quando a tradição é adicionada ao loadout.
        /// </summary>
        public void OnAdded(TraditionContext context)
        {
            if (_isInitialized) return;
            
            _isInitialized = true;
            _isActive = true;
            
            if (_cachedData?.Effects == null) return;
            
            foreach (var effect in _cachedData.Effects)
            {
                effect?.OnEquip(this, context);
            }
        }
        
        /// <summary>
        /// Chamado quando a tradição é removida do loadout.
        /// </summary>
        public void OnRemoved(TraditionContext context)
        {
            if (!_isInitialized) return;
            
            if (_cachedData?.Effects != null)
            {
                foreach (var effect in _cachedData.Effects)
                {
                    effect?.OnUnequip(this, context);
                }
            }
            
            _isInitialized = false;
            _isActive = false;
            ClearState();
        }
        
        /// <summary>
        /// Chamado no início de cada dia.
        /// </summary>
        public void OnDayStart()
        {
            // Reset de timers diários, etc.
        }
        
        /// <summary>
        /// Chamado no início de cada semana.
        /// </summary>
        public void OnWeekStart()
        {
            // Reset de counters semanais, etc.
        }
        
        // --- Hydration (para loads) ---
        
        public void HydrateData(TraditionData data)
        {
            if (data != null && data.ID == _traditionID)
            {
                _cachedData = data;
            }
        }
        
        // --- Estado Runtime ---
        
        public void SetActive(bool active) => _isActive = active;
        
        public int GetCounter(string key) => _counters.TryGetValue(key, out var val) ? val : 0;
        public void SetCounter(string key, int value) => _counters[key] = value;
        public void IncrementCounter(string key, int amount = 1)
        {
            _counters[key] = GetCounter(key) + amount;
        }
        
        public float GetTimer(string key) => _timers.TryGetValue(key, out var val) ? val : 0f;
        public void SetTimer(string key, float value) => _timers[key] = value;
        
        public void ClearState()
        {
            _counters?.Clear();
            _timers?.Clear();
        }
        
        // --- Helpers ---
        
        private string GenerateInstanceID()
        {
            return $"{_traditionID}_{Guid.NewGuid():N}".Substring(0, 20);
        }
        
        public override string ToString() => $"[Tradition: {_traditionID} (P{_executionPriority})]";
    }
}
