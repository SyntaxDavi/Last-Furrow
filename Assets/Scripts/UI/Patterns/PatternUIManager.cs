using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks; 

/// <summary>
/// Gerenciador central de toda UI de patterns.
/// Coordena pop-ups, feedback de debug, highlights e eventos de decay.
/// Versão: Migrada para UniTask (Async/Await)
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
        
        // ONDA 6.1: Remover FindFirstObjectByType - Validar apenas (atribuir no Inspector)
        if (_popupController == null)
        {
            _popupController = GetComponentInChildren<PatternTextPopupController>();
            if (_popupController == null)
            {
                Debug.LogError("[PatternUIManager] PatternTextPopupController não encontrado! Atribua no Inspector.");
            }
        }
        
        Debug.Log($"[PatternUIManager] PopupController found: {_popupController != null}");
    }

    private void OnEnable()
    {
        Debug.Log("[PatternUIManager] ========== ON ENABLE ==========");
        TrySubscribeToEvents();
    }

    // Mudança 1: Start agora é async UniTaskVoid para aguardar inicialização sem bloquear
    private async UniTaskVoid Start()
    {
        Debug.Log("[PatternUIManager] ========== START (waiting for AppCore) ==========");

        // Aguarda AppCore estar pronto de forma assíncrona
        await UniTask.WaitUntil(() => AppCore.Instance?.Events?.Pattern != null);

        Debug.Log("[PatternUIManager] AppCore ready! Subscribing to events...");
        TrySubscribeToEvents();
    }

    private void TrySubscribeToEvents()
    {
        if (_isSubscribed) return;

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
    }

    private void OnPatternCompleted(PatternMatch match)
    {
        // WRAPPER: Fire-and-forget usando .Forget() do UniTask
        // Não precisamos awaitar aqui pois é resposta a um evento
        ShowPatternPopupDirect(match);
    }

    /// <summary>
    /// WRAPPER: Método público para chamada direta (fire-and-forget).
    /// </summary>
    public void ShowPatternPopupDirect(PatternMatch match)
    {
        if (_popupController != null)
        {
            if (!_popupController.gameObject.activeInHierarchy)
            {
                _popupController.gameObject.SetActive(true);
            }

            // Inicia a task e esquece (não bloqueia execução)
            ShowPatternRoutine(match).Forget();
        }
        else
        {
            Debug.LogError("[PatternUIManager] ? PopupController is NULL!");
        }
    }

    /// <summary>
    /// PIPELINE-SAFE: Retorna UniTask para caller aguardar animação completa.
    /// Use este método no GridSlotScanner.
    /// </summary>
    public async UniTask ShowPatternPopupRoutine(PatternMatch match)
    {
        // Debug.Log($"[PatternUIManager] ?? PIPELINE call (awaitable) for {match.DisplayName}");

        if (_popupController != null)
        {
            if (!_popupController.gameObject.activeInHierarchy)
            {
                _popupController.gameObject.SetActive(true);
            }

            await ShowPatternRoutine(match);
        }
        else
        {
            Debug.LogError("[PatternUIManager] ? PopupController is NULL!");
        }
    }

    /// <summary>
    /// Wrapper interno que converte a Coroutine do Controller legado para UniTask.
    /// </summary>
    private async UniTask ShowPatternRoutine(PatternMatch match)
    {
        await _popupController.ShowPatternAsync(match);
    }

    private void OnPatternsDetected(List<PatternMatch> matches, int totalPoints)
    {
        if (_enableDebugFeedback && _debugFeedbackView != null)
        {
            _debugFeedbackView.gameObject.SetActive(true);
        }
    }

    private void OnDecayApplied(PatternMatch match, int daysActive, float multiplier)
    {
        // Opcional: Logs apenas se debug estiver ativado para limpar console
        if (_enableDebugFeedback)
            _config.DebugLog($"[Decay] {match.DisplayName}: {daysActive} days, {multiplier:F2}x");
    }

    private void OnRecreated(PatternMatch match)
    {
        if (_enableDebugFeedback)
            _config.DebugLog($"[Recreation] {match.DisplayName}");
    }
}