using UnityEngine;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// Víncula o TraditionService (Global) ao TraditionViewManager (Cena).
    /// </summary>
    public class TraditionsUIBootstrapper : MonoBehaviour
    {
        [SerializeField] private TraditionViewManager _viewManager;
        
        private void Start()
        {
            if (_viewManager == null)
            {
                _viewManager = GetComponent<TraditionViewManager>();
            }
            
            if (_viewManager == null)
            {
                Debug.LogError("[TraditionsUIBootstrapper] TraditionViewManager not found!");
                return;
            }
            
            // Busca o serviço global do AppCore
            var service = AppCore.Instance?.Services?.Traditions;
            
            if (service != null && service.Loadout != null)
            {
                _viewManager.Bind(service.Loadout);
                
                // Subscreve a eventos de interação se necessário
                _viewManager.OnSwapRequested += (idxA, idxB) => {
                    service.Loadout.SwapExecutionPriority(idxA, idxB);
                    service.SaveToRunData();
                };
            }
            else
            {
                Debug.LogWarning("[TraditionsUIBootstrapper] TraditionService not available in AppCore yet.");
            }
        }
    }
}
