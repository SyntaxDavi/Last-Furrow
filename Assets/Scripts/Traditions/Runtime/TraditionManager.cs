using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// Gerencia as tradições ativas do jogador.
    /// Responsável por:
    /// - Manter a lista de tradições equipadas
    /// - Spawnar/remover TraditionViews
    /// - Orquestrar posicionamento via layout
    /// - Disparar eventos de tradição
    /// - Reordenação de tradições (ordem importa para efeitos)
    /// 
    /// ARQUITETURA:
    /// - Similar ao HandManager mas para tradições
    /// - Tradições são fixas (não podem ser arrastadas)
    /// - Reordenação via Swap (seleciona 2 para trocar)
    /// - Max slots vem da RunData (pode ser aumentado por upgrades)
    /// </summary>
    public class TraditionManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private TraditionLayoutConfig _layoutConfig;
        [SerializeField] private TraditionView _traditionPrefab;
        
        [Header("References")]
        [SerializeField] private Transform _traditionContainer;
        
        // Estado interno
        private List<TraditionView> _activeViews = new List<TraditionView>();
        private List<TraditionInstance> _activeInstances = new List<TraditionInstance>();
        private RunData _runData;
        
        // Estado de reordenação
        private int _selectedSwapIndex = -1;
        private bool _isSwapMode = false;
        
        // Cache de dados (para lookup rápido)
        private IGameLibrary _library;
        
        // Eventos
        public event Action<TraditionInstance> OnTraditionAdded;
        public event Action<TraditionInstance> OnTraditionRemoved;
        public event Action OnTraditionsChanged;
        public event Action<int, int> OnTraditionsSwapped;
        
        // --- Propriedades Públicas ---
        
        public int ActiveCount => _activeInstances.Count;
        public int MaxTraditions => _runData?.MaxTraditionSlots ?? _layoutConfig?.maxTraditions ?? 5;
        public bool CanAddTradition => ActiveCount < MaxTraditions;
        public IReadOnlyList<TraditionInstance> ActiveTraditions => _activeInstances.AsReadOnly();
        public bool IsSwapMode => _isSwapMode;
        public int SelectedSwapIndex => _selectedSwapIndex;
        
        // --- Inicialização ---
        
        private void Awake()
        {
            if (_traditionContainer == null)
            {
                _traditionContainer = transform;
            }
        }
        
        /// <summary>
        /// Configura o manager com dependências externas.
        /// Chamado pelo bootstrap do jogo.
        /// </summary>
        public void Configure(RunData runData, IGameLibrary library)
        {
            _runData = runData;
            _library = library;
            
            Debug.Log($"[TraditionManager] Configured with max {MaxTraditions} slots");
        }
        
        /// <summary>
        /// Inicializa o manager com as tradições da RunData.
        /// </summary>
        public void Initialize()
        {
            if (_runData == null)
            {
                Debug.LogError("[TraditionManager] RunData not configured! Call Configure first.");
                return;
            }
            
            // Limpa estado anterior
            ClearAllTraditions();
            
            // Spawna cada tradição salva na ordem correta
            foreach (var traditionID in _runData.ActiveTraditionIDs)
            {
                if (_library.TryGetTradition(traditionID, out var data))
                {
                    var instance = new TraditionInstance(data);
                    SpawnTraditionView(instance, data, false);
                }
                else
                {
                    Debug.LogWarning($"[TraditionManager] Tradition not found: {traditionID}");
                }
            }
            
            // Recalcula layout
            RecalculateLayout();
            
            Debug.Log($"[TraditionManager] Initialized with {ActiveCount} traditions");
        }
        
        // --- API Pública ---
        
        /// <summary>
        /// Adiciona uma nova tradição (ex: comprada na loja).
        /// </summary>
        public bool TryAddTradition(TraditionData data)
        {
            if (data == null)
            {
                Debug.LogError("[TraditionManager] Cannot add null tradition");
                return false;
            }
            
            if (!CanAddTradition)
            {
                Debug.LogWarning($"[TraditionManager] Cannot add tradition - max reached ({MaxTraditions})");
                return false;
            }
            
            var instance = new TraditionInstance(data);
            SpawnTraditionView(instance, data, true);
            
            // Persiste na RunData
            _runData.ActiveTraditionIDs.Add(data.ID);
            
            OnTraditionAdded?.Invoke(instance);
            OnTraditionsChanged?.Invoke();
            
            Debug.Log($"[TraditionManager] Added tradition: {data.DisplayName}");
            return true;
        }
        
        /// <summary>
        /// Adiciona tradição por ID (para uso com shop items).
        /// </summary>
        public bool TryAddTradition(string traditionID)
        {
            if (_library.TryGetTradition(traditionID, out var data))
            {
                return TryAddTradition(data);
            }
            
            Debug.LogError($"[TraditionManager] Tradition not found: {traditionID}");
            return false;
        }
        
        /// <summary>
        /// Remove uma tradição e retorna seu valor de venda.
        /// </summary>
        public bool TrySellTradition(int index, out int sellValue)
        {
            sellValue = 0;
            
            if (index < 0 || index >= _activeInstances.Count)
                return false;
            
            var instance = _activeInstances[index];
            var data = instance.Data;
            
            if (data != null)
            {
                sellValue = data.GetSellValue();
            }
            
            return TryRemoveTradition(instance);
        }
        
        /// <summary>
        /// Remove uma tradição.
        /// </summary>
        public bool TryRemoveTradition(TraditionInstance instance)
        {
            if (instance == null) return false;
            
            int index = _activeInstances.IndexOf(instance);
            if (index < 0) return false;
            
            // Remove view
            var view = _activeViews[index];
            _activeViews.RemoveAt(index);
            _activeInstances.RemoveAt(index);
            
            // Remove da RunData
            _runData.ActiveTraditionIDs.RemoveAt(index);
            
            if (view != null)
            {
                Destroy(view.gameObject);
            }
            
            // Recalcula layout
            RecalculateLayout();
            
            // Cancela swap mode se estava ativo
            CancelSwapMode();
            
            OnTraditionRemoved?.Invoke(instance);
            OnTraditionsChanged?.Invoke();
            
            Debug.Log($"[TraditionManager] Removed tradition: {instance.TraditionID}");
            return true;
        }
        
        // --- Reordenação ---
        
        /// <summary>
        /// Entra no modo de swap (jogador selecionou uma tradição para trocar).
        /// </summary>
        public void EnterSwapMode(int index)
        {
            if (index < 0 || index >= _activeInstances.Count)
                return;
            
            if (_isSwapMode && _selectedSwapIndex == index)
            {
                // Clicou na mesma, cancela
                CancelSwapMode();
                return;
            }
            
            if (_isSwapMode && _selectedSwapIndex != index)
            {
                // Já tinha uma selecionada, faz o swap
                SwapTraditions(_selectedSwapIndex, index);
                CancelSwapMode();
                return;
            }
            
            // Primeira seleção
            _isSwapMode = true;
            _selectedSwapIndex = index;
            
            // Visual feedback (highlight)
            HighlightTradition(index, true);
            
            Debug.Log($"[TraditionManager] Swap mode: selected index {index}");
        }
        
        /// <summary>
        /// Cancela o modo de swap.
        /// </summary>
        public void CancelSwapMode()
        {
            if (_selectedSwapIndex >= 0 && _selectedSwapIndex < _activeViews.Count)
            {
                HighlightTradition(_selectedSwapIndex, false);
            }
            
            _isSwapMode = false;
            _selectedSwapIndex = -1;
        }
        
        /// <summary>
        /// Troca duas tradições de posição.
        /// </summary>
        public void SwapTraditions(int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= _activeInstances.Count) return;
            if (indexB < 0 || indexB >= _activeInstances.Count) return;
            if (indexA == indexB) return;
            
            // Swap nas listas internas
            (_activeInstances[indexA], _activeInstances[indexB]) = (_activeInstances[indexB], _activeInstances[indexA]);
            (_activeViews[indexA], _activeViews[indexB]) = (_activeViews[indexB], _activeViews[indexA]);
            
            // Swap na RunData
            (_runData.ActiveTraditionIDs[indexA], _runData.ActiveTraditionIDs[indexB]) = 
                (_runData.ActiveTraditionIDs[indexB], _runData.ActiveTraditionIDs[indexA]);
            
            // Recalcula layout com animação
            RecalculateLayout();
            
            OnTraditionsSwapped?.Invoke(indexA, indexB);
            OnTraditionsChanged?.Invoke();
            
            Debug.Log($"[TraditionManager] Swapped traditions at {indexA} <-> {indexB}");
        }
        
        // --- Helpers ---
        
        private void HighlightTradition(int index, bool highlight)
        {
            if (index < 0 || index >= _activeViews.Count) return;
            
            var view = _activeViews[index];
            if (view == null) return;
            
            // TODO: Implementar highlight visual no TraditionView
            // Por enquanto, apenas scale
            float targetScale = highlight ? _layoutConfig.scale * 1.15f : _layoutConfig.scale;
            view.transform.DOScale(targetScale, 0.15f).SetEase(Ease.OutBack);
        }
        
        private void SpawnTraditionView(TraditionInstance instance, TraditionData data, bool animate)
        {
            if (_traditionPrefab == null)
            {
                Debug.LogError("[TraditionManager] Tradition prefab not assigned!");
                return;
            }
            
            int slotIndex = _activeInstances.Count;
            Vector3 targetPos = _layoutConfig.GetWorldPosition(slotIndex, slotIndex + 1);
            
            var view = Instantiate(_traditionPrefab, _traditionContainer);
            view.Initialize(instance, data, slotIndex, _layoutConfig);
            
            _activeInstances.Add(instance);
            _activeViews.Add(view);
            
            if (animate)
            {
                view.PlaySpawnAnimation(targetPos);
            }
            else
            {
                view.MoveTo(targetPos, false);
            }
            
            // Recalcula posições de todos
            RecalculateLayout();
        }
        
        private void RecalculateLayout()
        {
            int total = _activeViews.Count;
            
            for (int i = 0; i < total; i++)
            {
                var view = _activeViews[i];
                view.SetSlotIndex(i);
                
                Vector3 targetPos = _layoutConfig.GetWorldPosition(i, total);
                view.MoveTo(targetPos, true);
            }
        }
        
        private void ClearAllTraditions()
        {
            foreach (var view in _activeViews)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }
            
            _activeViews.Clear();
            _activeInstances.Clear();
            CancelSwapMode();
        }
        
        // --- Debug ---
        
        [ContextMenu("Debug: Add Random Tradition")]
        private void DebugAddRandomTradition()
        {
            if (_library == null)
            {
                Debug.LogWarning("[TraditionManager] Library not configured");
                return;
            }
            
            var traditions = _library.GetRandomTraditions(1);
            if (traditions.Count > 0)
            {
                TryAddTradition(traditions[0]);
            }
        }
        
        [ContextMenu("Debug: Swap First Two")]
        private void DebugSwapFirstTwo()
        {
            if (_activeInstances.Count >= 2)
            {
                SwapTraditions(0, 1);
            }
        }
    }
}
