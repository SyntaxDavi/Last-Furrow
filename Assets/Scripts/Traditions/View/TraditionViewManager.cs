using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// Gerencia a visualização das tradições na tela.
    /// 
    /// RESPONSABILIDADES (View Layer - APENAS visual):
    /// - Spawnar/destruir TraditionViews
    /// - Gerenciar layout e posicionamento
    /// - Animações de spawn/remoção
    /// - Estado de UI (swap mode visual)
    /// 
    /// NÃO FAZ:
    /// - Lógica de negócio (isso é TraditionService)
    /// - Persistência (isso é TraditionService)
    /// - Avaliação de efeitos (isso é TraditionEvaluator)
    /// 
    /// REAGE A:
    /// - Eventos do TraditionLoadout
    /// </summary>
    [ExecuteAlways]
    public class TraditionViewManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private TraditionLayoutConfig _layoutConfig;
        [SerializeField] private GameObject _viewPrefab;
        
        [Header("References")]
        [SerializeField] private Transform _container;
        
        // Mapeamento Instance -> View
        private Dictionary<string, TraditionView> _viewMap = new Dictionary<string, TraditionView>();
        private List<TraditionView> _views = new List<TraditionView>();
        
        // Estado de UI
        private int _selectedSwapIndex = -1;
        private bool _isSwapMode;
        
        // Referência ao loadout que estamos visualizando
        private TraditionLoadout _loadout;
        
        // Eventos de UI
        public event Action<int> OnTraditionClicked;
        public event Action<int, int> OnSwapRequested;
        
        // --- Propriedades ---
        
        public bool IsSwapMode => _isSwapMode;
        public int SelectedSwapIndex => _selectedSwapIndex;
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying && _views.Count > 0)
            {
                RecalculateLayout();
            }
        }
#endif

        // --- Inicialização ---
        
        private void Awake()
        {
            if (_container == null)
                _container = transform;
        }
        
        /// <summary>
        /// Vincula o ViewManager a um TraditionLoadout.
        /// </summary>
        public void Bind(TraditionLoadout loadout)
        {
            // Desvincula anterior
            if (_loadout != null)
            {
                _loadout.OnTraditionAdded -= HandleTraditionAdded;
                _loadout.OnTraditionRemoved -= HandleTraditionRemoved;
                _loadout.OnLoadoutChanged -= HandleLoadoutChanged;
            }
            
            _loadout = loadout;
            
            if (_loadout != null)
            {
                _loadout.OnTraditionAdded += HandleTraditionAdded;
                _loadout.OnTraditionRemoved += HandleTraditionRemoved;
                _loadout.OnLoadoutChanged += HandleLoadoutChanged;
            }
            
            // Sincroniza views com estado atual
            SyncViews();
        }
        
        private void OnDestroy()
        {
            if (_loadout != null)
            {
                _loadout.OnTraditionAdded -= HandleTraditionAdded;
                _loadout.OnTraditionRemoved -= HandleTraditionRemoved;
                _loadout.OnLoadoutChanged -= HandleLoadoutChanged;
            }
        }
        
        // --- Manipulação de Views ---
        
        private void SyncViews()
        {
            ClearAllViews();
            
            if (_loadout == null) return;
            
            var traditions = _loadout.GetByDisplayOrder();
            for (int i = 0; i < traditions.Count; i++)
            {
                SpawnView(traditions[i], i, animate: false);
            }
            
            RecalculateLayout();
        }
        
        private void SpawnView(TraditionInstance instance, int displayIndex, bool animate)
        {
            if (_viewPrefab == null || instance.Data == null) return;
            
            int count = _loadout?.Count ?? 1;
            Vector3 targetPos = _layoutConfig.GetLocalPosition(displayIndex, count);
            
            var viewGo = Instantiate(_viewPrefab, _container);
            var view = viewGo.GetComponent<TraditionView>();
            
            if (view == null)
            {
                Debug.LogError($"[TraditionViewManager] Prefab {_viewPrefab.name} does not have a TraditionView component!");
                Destroy(viewGo);
                return;
            }
            
            view.Initialize(instance, instance.Data, displayIndex, _layoutConfig);
            view.OnClicked += HandleViewClicked;
            
            _viewMap[instance.InstanceID] = view;
            _views.Add(view);
            
            if (animate)
            {
                view.PlaySpawnAnimation(targetPos);
            }
            else
            {
                view.MoveTo(targetPos, animate: false);
            }
        }
        
        private void DestroyView(TraditionInstance instance)
        {
            if (!_viewMap.TryGetValue(instance.InstanceID, out var view))
                return;
            
            view.OnClicked -= HandleViewClicked;
            _viewMap.Remove(instance.InstanceID);
            _views.Remove(view);
            
            // Animação de saída
            view.transform.DOScale(0, 0.2f).SetEase(Ease.InBack)
                .OnComplete(() => {
                    if (view != null) Destroy(view.gameObject);
                });
        }
        
        private void ClearAllViews()
        {
            foreach (var view in _views)
            {
                if (view != null)
                {
                    view.OnClicked -= HandleViewClicked;
                    Destroy(view.gameObject);
                }
            }
            
            _views.Clear();
            _viewMap.Clear();
            CancelSwapMode();
        }
        
        private void RecalculateLayout()
        {
            int total = _views.Count;
            
            for (int i = 0; i < total; i++)
            {
                var view = _views[i];
                view.SetSlotIndex(i);
                
                Vector3 targetPos = _layoutConfig.GetLocalPosition(i, total);
                view.MoveTo(targetPos, animate: true);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_layoutConfig == null) return;

            Gizmos.color = Color.cyan;
            int previewCount = Application.isPlaying && _loadout != null ? _loadout.Count : 3;
            
            for (int i = 0; i < previewCount; i++)
            {
                Vector3 localPos = _layoutConfig.GetLocalPosition(i, previewCount);
                Vector3 worldPos = (_container != null ? _container : transform).TransformPoint(localPos);
                
                // Desenha um retângulo simulando a tradição
                Gizmos.matrix = Matrix4x4.TRS(worldPos, transform.rotation, transform.lossyScale);
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(1f, 1.4f, 0.1f) * _layoutConfig.scale);
                
                // Bolinha no centro
                Gizmos.DrawSphere(Vector3.zero, 0.05f);
            }
            
            Gizmos.matrix = Matrix4x4.identity;
        }
        
        // --- Event Handlers ---
        
        private void HandleTraditionAdded(TraditionInstance instance)
        {
            int displayIndex = _loadout.Count - 1;
            SpawnView(instance, displayIndex, animate: true);
            RecalculateLayout();
        }
        
        private void HandleTraditionRemoved(TraditionInstance instance)
        {
            DestroyView(instance);
            RecalculateLayout();
            CancelSwapMode();
        }
        
        private void HandleLoadoutChanged()
        {
            // Re-sincroniza ordem visual
            SyncViewOrder();
        }
        
        private void SyncViewOrder()
        {
            if (_loadout == null) return;
            
            var traditions = _loadout.GetByDisplayOrder();
            _views.Clear();
            
            foreach (var tradition in traditions)
            {
                if (_viewMap.TryGetValue(tradition.InstanceID, out var view))
                {
                    _views.Add(view);
                }
            }
            
            RecalculateLayout();
        }
        
        private void HandleViewClicked(TraditionView view)
        {
            int index = _views.IndexOf(view);
            if (index < 0) return;
            
            OnTraditionClicked?.Invoke(index);
            
            // Lógica de Swap Mode
            ProcessSwapClick(index);
        }
        
        // --- Swap Mode ---
        
        private void ProcessSwapClick(int index)
        {
            if (_isSwapMode && _selectedSwapIndex == index)
            {
                CancelSwapMode();
                return;
            }
            
            if (_isSwapMode && _selectedSwapIndex != index)
            {
                OnSwapRequested?.Invoke(_selectedSwapIndex, index);
                CancelSwapMode();
                return;
            }
            
            _isSwapMode = true;
            _selectedSwapIndex = index;
            HighlightView(index, true);
        }
        
        public void CancelSwapMode()
        {
            if (_selectedSwapIndex >= 0 && _selectedSwapIndex < _views.Count)
            {
                HighlightView(_selectedSwapIndex, false);
            }
            
            _isSwapMode = false;
            _selectedSwapIndex = -1;
        }
        
        private void HighlightView(int index, bool highlight)
        {
            if (index < 0 || index >= _views.Count) return;
            
            var view = _views[index];
            if (view == null) return;
            
            float targetScale = highlight 
                ? _layoutConfig.scale * 1.15f 
                : _layoutConfig.scale;
                
            view.transform.DOScale(targetScale, 0.15f).SetEase(Ease.OutBack);
        }
    }
}
