using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// Gerencia o loadout de tradições do jogador.
    /// 
    /// RESPONSABILIDADES (Domain Layer - SEM dependência de Unity Visual):
    /// - Manter lista de tradições equipadas
    /// - Gerenciar limite de slots
    /// - Ordenar por prioridade de execução
    /// - Lifecycle das instâncias
    /// 
    /// NÃO FAZ:
    /// - Spawning de Views (isso é TraditionViewManager)
    /// - Animações (isso é View Layer)
    /// - Persistência direta (isso é RunData/SaveSystem)
    /// - Lookup de dados (isso é IGameLibrary)
    /// </summary>
    [Serializable]
    public class TraditionLoadout
    {
        [SerializeField] private List<TraditionInstance> _traditions = new List<TraditionInstance>();
        [SerializeField] private int _maxSlots = 5;
        
        // Runtime only
        [NonSerialized] private TraditionContext _context;
        [NonSerialized] private bool _isInitialized;
        
        // Eventos (para View Layer reagir)
        public event Action<TraditionInstance> OnTraditionAdded;
        public event Action<TraditionInstance> OnTraditionRemoved;
        public event Action OnLoadoutChanged;
        
        // --- Propriedades ---
        
        public int Count => _traditions.Count;
        public int MaxSlots => _maxSlots;
        public bool CanAdd => Count < _maxSlots;
        public bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// Retorna tradições ordenadas por prioridade de execução.
        /// Esta é a ordem para avaliar efeitos (não a ordem visual).
        /// </summary>
        public IReadOnlyList<TraditionInstance> GetByExecutionOrder()
        {
            return _traditions.OrderBy(t => t.ExecutionPriority).ToList().AsReadOnly();
        }
        
        /// <summary>
        /// Retorna tradições na ordem de inserção (para display visual).
        /// </summary>
        public IReadOnlyList<TraditionInstance> GetByDisplayOrder()
        {
            return _traditions.AsReadOnly();
        }
        
        // --- Inicialização ---
        
        public void Initialize(TraditionContext context, IGameLibrary library)
        {
            _context = context;
            
            // Hydrate todas as instâncias com dados
            foreach (var instance in _traditions)
            {
                if (library.TryGetTradition(instance.TraditionID, out var data))
                {
                    instance.HydrateData(data);
                    instance.OnAdded(context);
                }
            }
            
            _isInitialized = true;
        }
        
        public void SetMaxSlots(int slots)
        {
            _maxSlots = Mathf.Max(1, slots);
        }
        
        // --- Operações ---
        
        /// <summary>
        /// Adiciona uma tradição ao loadout.
        /// Retorna false se não há slot disponível.
        /// </summary>
        public bool TryAdd(TraditionInstance instance)
        {
            if (instance == null || !CanAdd)
                return false;
            
            // Define prioridade baseada na posição de inserção (pode ser alterada depois)
            instance.ExecutionPriority = _traditions.Count;
            
            _traditions.Add(instance);
            
            if (_isInitialized && _context != null)
            {
                instance.OnAdded(_context);
            }
            
            OnTraditionAdded?.Invoke(instance);
            OnLoadoutChanged?.Invoke();
            
            return true;
        }
        
        /// <summary>
        /// Adiciona tradição criando instância a partir de dados.
        /// </summary>
        public bool TryAdd(TraditionData data, out TraditionInstance instance)
        {
            instance = null;
            if (data == null || !CanAdd)
                return false;
            
            instance = new TraditionInstance(data, _traditions.Count);
            return TryAdd(instance);
        }
        
        /// <summary>
        /// Remove uma tradição do loadout.
        /// </summary>
        public bool TryRemove(TraditionInstance instance)
        {
            if (instance == null)
                return false;
            
            int index = _traditions.IndexOf(instance);
            if (index < 0)
                return false;
            
            instance.OnRemoved(_context);
            _traditions.RemoveAt(index);
            
            // Reajusta prioridades
            RecalculatePriorities();
            
            OnTraditionRemoved?.Invoke(instance);
            OnLoadoutChanged?.Invoke();
            
            return true;
        }
        
        /// <summary>
        /// Remove tradição por ID.
        /// </summary>
        public bool TryRemove(TraditionID id)
        {
            var instance = _traditions.FirstOrDefault(t => t.TraditionID == id);
            return instance != null && TryRemove(instance);
        }
        
        /// <summary>
        /// Troca a prioridade de execução de duas tradições.
        /// Isso NÃO muda a posição visual (display order).
        /// </summary>
        public void SwapExecutionPriority(int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= _traditions.Count) return;
            if (indexB < 0 || indexB >= _traditions.Count) return;
            if (indexA == indexB) return;
            
            var priorityA = _traditions[indexA].ExecutionPriority;
            var priorityB = _traditions[indexB].ExecutionPriority;
            
            _traditions[indexA].ExecutionPriority = priorityB;
            _traditions[indexB].ExecutionPriority = priorityA;
            
            OnLoadoutChanged?.Invoke();
        }
        
        /// <summary>
        /// Muda a posição visual (display order) de uma tradição.
        /// </summary>
        public void MoveDisplayPosition(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _traditions.Count) return;
            if (toIndex < 0 || toIndex >= _traditions.Count) return;
            if (fromIndex == toIndex) return;
            
            var item = _traditions[fromIndex];
            _traditions.RemoveAt(fromIndex);
            _traditions.Insert(toIndex, item);
            
            OnLoadoutChanged?.Invoke();
        }
        
        /// <summary>
        /// Obtém tradição por índice de display.
        /// </summary>
        public TraditionInstance GetByIndex(int index)
        {
            if (index < 0 || index >= _traditions.Count)
                return null;
            return _traditions[index];
        }
        
        /// <summary>
        /// Busca tradição por ID.
        /// </summary>
        public TraditionInstance FindByID(TraditionID id)
        {
            return _traditions.FirstOrDefault(t => t.TraditionID == id);
        }
        
        /// <summary>
        /// Verifica se já possui uma tradição com este ID.
        /// </summary>
        public bool Contains(TraditionID id)
        {
            return _traditions.Any(t => t.TraditionID == id);
        }
        
        // --- Lifecycle Events ---
        
        public void OnDayStart()
        {
            foreach (var tradition in _traditions)
            {
                tradition.OnDayStart();
            }
        }
        
        public void OnWeekStart()
        {
            foreach (var tradition in _traditions)
            {
                tradition.OnWeekStart();
            }
        }
        
        // --- Serialização ---
        
        /// <summary>
        /// Retorna lista de IDs para persistência.
        /// </summary>
        public List<TraditionID> GetIDsForSave()
        {
            return _traditions.Select(t => t.TraditionID).ToList();
        }
        
        /// <summary>
        /// Cria loadout a partir de IDs salvos.
        /// </summary>
        public static TraditionLoadout FromSavedIDs(List<TraditionID> ids, int maxSlots)
        {
            var loadout = new TraditionLoadout { _maxSlots = maxSlots };
            
            for (int i = 0; i < ids.Count; i++)
            {
                var instance = new TraditionInstance(ids[i], i);
                loadout._traditions.Add(instance);
            }
            
            return loadout;
        }
        
        // --- Helpers ---
        
        private void RecalculatePriorities()
        {
            for (int i = 0; i < _traditions.Count; i++)
            {
                _traditions[i].ExecutionPriority = i;
            }
        }
        
        public void Clear()
        {
            foreach (var tradition in _traditions.ToList())
            {
                TryRemove(tradition);
            }
        }
    }
}
