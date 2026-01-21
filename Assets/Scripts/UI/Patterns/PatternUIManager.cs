using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerenciador central de toda UI de patterns.
/// Coordena pop-ups, feedback de debug, highlights e eventos de decay.
/// </summary>
public class PatternUIManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _config;
    
    [Header("Sub-Controllers")]
    [SerializeField] private PatternTextPopupController _popupController;
    [SerializeField] private PatternFeedbackView _debugFeedbackView;
    [SerializeField] private PatternHighlightController _highlightController;
    
    [Header("Debug")]
    [SerializeField] private bool _enableDebugFeedback = false;
    
    private bool _isSubscribed = false;
    
    private void Awake()
    {
        Debug.Log("[PatternUIManager] ========== AWAKE (Wrapper) ==========");
        
        if (_config == null)
        {
            _config = Resources.Load<PatternVisualConfig>("Patterns/PatternVisualConfig");
        }
        
        if (_popupController == null)
        {
            _popupController = GetComponentInChildren<PatternTextPopupController>();
            if (_popupController == null)
            {
                _popupController = FindFirstObjectByType<PatternTextPopupController>();
            }
        }
        
        Debug.Log($"[PatternUIManager] PopupController found: {_popupController != null}");
    }
    
    private void OnEnable()
    {
        Debug.Log("[PatternUIManager] ========== ON ENABLE ==========");
        TrySubscribeToEvents();
    }
    
    private IEnumerator Start()
    {
        Debug.Log("[PatternUIManager] ========== START (waiting for AppCore) ==========");
        
        // Aguarda AppCore estar pronto
        yield return new WaitUntil(() => AppCore.Instance?.Events?.Pattern != null);
        
        Debug.Log("[PatternUIManager] AppCore ready! Subscribing to events...");
        TrySubscribeToEvents();
    }
    
    private void TrySubscribeToEvents()
    {
        if (_isSubscribed)
        {
            Debug.Log("[PatternUIManager] Already subscribed, skipping...");
            return;
        }
        
        if (AppCore.Instance?.Events?.Pattern == null)
        {
            Debug.LogWarning("[PatternUIManager] AppCore not ready yet, will retry in Start()");
            return;
        }
        
        AppCore.Instance.Events.Pattern.OnPatternSlotCompleted += OnPatternCompleted;
        AppCore.Instance.Events.Pattern.OnPatternsDetected += OnPatternsDetected;
        AppCore.Instance.Events.Pattern.OnPatternDecayApplied += OnDecayApplied;
        AppCore.Instance.Events.Pattern.OnPatternRecreated += OnRecreated;
        
        _isSubscribed = true;
        Debug.Log("[PatternUIManager] ? All event listeners subscribed!");
    }
    
    private void OnDisable()
    {
        if (!_isSubscribed) return;
        
        if (AppCore.Instance?.Events?.Pattern != null)
        {
            AppCore.Instance.Events.Pattern.OnPatternSlotCompleted -= OnPatternCompleted;
            AppCore.Instance.Events.Pattern.OnPatternsDetected -= OnPatternsDetected;
            AppCore.Instance.Events.Pattern.OnPatternDecayApplied -= OnDecayApplied;
            AppCore.Instance.Events.Pattern.OnPatternRecreated -= OnRecreated;
        }
        
        _isSubscribed = false;
        Debug.Log("[PatternUIManager] Event listeners unsubscribed");
    }
    
    private void OnPatternCompleted(PatternMatch match)
    {
        // WRAPPER: Ativa controller e delega popup (coroutine roda aqui)
        if (_popupController != null)
        {
            Debug.Log($"[PatternUIManager] ?? Delegating popup to controller: {match.DisplayName}");
            
            // Garantir que o GameObject está ativo antes de iniciar coroutine
            if (!_popupController.gameObject.activeInHierarchy)
            {
                _popupController.gameObject.SetActive(true);
            }
            
            StartCoroutine(ShowPatternRoutine(match));
        }
        else
        {
            Debug.LogError("[PatternUIManager] ? PopupController is NULL! Cannot show popup.");
        }
    }
    
    /// <summary>
    /// WRAPPER: Método público para chamada direta (fire-and-forget, não aguarda).
    /// Use ShowPatternPopupRoutine() se precisar aguardar no pipeline.
    /// </summary>
    public void ShowPatternPopupDirect(PatternMatch match)
    {
        Debug.Log($"[PatternUIManager] ?? WRAPPER: Direct call (fire-and-forget) for {match.DisplayName}");
        
        if (_popupController != null)
        {
            // Garantir que o GameObject está ativo
            if (!_popupController.gameObject.activeInHierarchy)
            {
                _popupController.gameObject.SetActive(true);
            }
            
            StartCoroutine(ShowPatternRoutine(match));
        }
        else
        {
            Debug.LogError("[PatternUIManager] ? PopupController is NULL!");
        }
    }
    
    /// <summary>
    /// PIPELINE-SAFE: Retorna IEnumerator para caller aguardar animação completa.
    /// Use este método quando o pipeline precisa sincronizar (yield return).
    /// </summary>
    public IEnumerator ShowPatternPopupRoutine(PatternMatch match)
    {
        Debug.Log($"[PatternUIManager] ?? PIPELINE call (awaitable) for {match.DisplayName}");
        
        if (_popupController != null)
        {
            // Garantir que o GameObject está ativo
            if (!_popupController.gameObject.activeInHierarchy)
            {
                _popupController.gameObject.SetActive(true);
            }
            
            yield return ShowPatternRoutine(match);
        }
        else
        {
            Debug.LogError("[PatternUIManager] ? PopupController is NULL!");
        }
    }
    
    /// <summary>
    /// Wrapper que roda no contexto ativo do UIManager (resolve problema de GameObject inativo).
    /// </summary>
    private IEnumerator ShowPatternRoutine(PatternMatch match)
    {
        yield return _popupController.ShowPatternCoroutine(match);
    }
    
    private void OnPatternsDetected(List<PatternMatch> matches, int totalPoints)
    {
        if (_enableDebugFeedback && _debugFeedbackView != null)
        {
            _debugFeedbackView.gameObject.SetActive(true);
        }
        
        _config.DebugLog($"[PatternUIManager] {matches.Count} patterns detected, {totalPoints} points");
    }
    
    private void OnDecayApplied(PatternMatch match, int daysActive, float multiplier)
    {
        _config.DebugLog($"[PatternUIManager] Decay: {match.DisplayName}, Days: {daysActive}, Multiplier: {multiplier:F2}");
    }
    
    private void OnRecreated(PatternMatch match)
    {
        _config.DebugLog($"[PatternUIManager] Recreation bonus: {match.DisplayName}");
    }
}
