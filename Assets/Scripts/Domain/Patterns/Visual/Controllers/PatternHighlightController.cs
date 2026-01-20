using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controller responsável por highlights de slots quando padrões são detectados.
/// 
/// RESPONSABILIDADE:
/// - Escutar evento OnPatternsDetected
/// - Aplicar cores baseadas em Tier + Decay
/// - Animação de pulse nos slots
/// - API pública para PatternVisualReplayController
/// 
/// FILOSOFIA: Mostra VISUALMENTE quais slots formam padrões.
/// 
/// ONDA 5.5 REFACTORED:
/// - Cache de slots em Dictionary (performance)
/// - Método público HighlightPatternSlots para replay
/// - FindFirstObjectByType em vez de FindObjectOfType (Unity 2023+)
/// </summary>
public class PatternHighlightController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _config;
    
    [Header("Grid Reference")]
    [Tooltip("Referência ao GridManager para acessar slots")]
    [SerializeField] private GridManager _gridManager;
    
    // Cache de slots para performance (O(1) lookup)
    private Dictionary<int, GridSlotView> _slotCache = new Dictionary<int, GridSlotView>();
    private bool _slotsCached = false;
    
    // Coroutines ativas por slot (para cancelar se necessário)
    private Dictionary<int, Coroutine> _activeHighlights = new Dictionary<int, Coroutine>();
    
    private void Start()
    {
        CacheSlots();
        SubscribeToEvents();
    }
    
    /// <summary>
    /// Cacheia todos os slots em um Dictionary para acesso O(1).
    /// </summary>
    private void CacheSlots()
    {
        if (_gridManager == null)
        {
            Debug.LogError("[PatternHighlightController] GridManager não atribuído!");
            return;
        }
        
        _slotCache.Clear();
        var slots = _gridManager.GetComponentsInChildren<GridSlotView>();
        
        foreach (var slot in slots)
        {
            if (slot != null)
            {
                _slotCache[slot.SlotIndex] = slot;
            }
        }
        
        _slotsCached = true;
        _config?.DebugLog($"[PatternHighlight] {_slotCache.Count} slots cacheados");
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    private void SubscribeToEvents()
    {
        if (AppCore.Instance?.Events?.Pattern != null)
        {
            AppCore.Instance.Events.Pattern.OnPatternsDetected += OnPatternsDetected;
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        if (AppCore.Instance?.Events?.Pattern != null)
        {
            AppCore.Instance.Events.Pattern.OnPatternsDetected -= OnPatternsDetected;
        }
    }
    
    /// <summary>
    /// Event handler: Padrões detectados.
    /// </summary>
    private void OnPatternsDetected(List<PatternMatch> matches, int totalPoints)
    {
        if (matches == null || matches.Count == 0) return;
        if (_config == null)
        {
            Debug.LogWarning("[PatternHighlightController] Config não atribuído!");
            return;
        }
        
        _config.DebugLog($"Highlighting {matches.Count} patterns");
        
        // Enfileirar highlights sequencialmente via VisualQueueSystem
        for (int i = 0; i < matches.Count; i++)
        {
            PatternMatch match = matches[i];
            
            // Determinar prioridade baseada em Tier
            int tier = GetTierFromScore(match.BaseScore);
            VisualEventPriority priority = tier >= 3 
                ? VisualEventPriority.High 
                : VisualEventPriority.Normal;
            
            // Criar evento visual
            var visualEvent = new VisualEvent(
                VisualEventType.Highlight,
                priority,
                () => HighlightPattern(match),
                match
            );
            
            // Enfileirar
            VisualQueueSystem.Instance?.Enqueue(visualEvent);
        }
    }
    
    /// <summary>
    /// API PÚBLICA: Destaca visualmente os slots de um padrão específico.
    /// Usada pelo PatternVisualReplayController.
    /// </summary>
    public IEnumerator HighlightPatternSlots(PatternMatch match)
    {
        if (match == null || match.SlotIndices == null)
        {
            yield break;
        }
        
        // Obter cor baseada em tier + decay
        int tier = GetTierFromScore(match.BaseScore);
        Color baseColor = _config.GetTierColor(tier);
        Color finalColor = _config.ApplyDecayToColor(baseColor, match.DaysActive);
        
        _config?.DebugLog($"[PatternHighlight] Highlighting {match.DisplayName}: Tier {tier}, Color {finalColor}");
        
        // Aplicar highlight em cada slot do padrão (em paralelo)
        List<Coroutine> activeCoroutines = new List<Coroutine>();
        
        foreach (int slotIndex in match.SlotIndices)
        {
            var coroutine = StartCoroutine(HighlightSlotCoroutine(slotIndex, finalColor));
            activeCoroutines.Add(coroutine);
        }
        
        // Aguardar todos os highlights terminarem
        foreach (var coroutine in activeCoroutines)
        {
            yield return coroutine;
        }
        
        _config?.DebugLog($"[PatternHighlight] Pattern {match.DisplayName} highlight concluído");
    }
    
    /// <summary>
    /// Aplica highlight em um padrão específico.
    /// REMOVIDO: Levitação do grid (agora é por slot no AnalyzingPhase).
    /// </summary>
    private void HighlightPattern(PatternMatch match)
    {
        if (match == null || match.SlotIndices == null) return;
        
        // Obter cor baseada em tier + decay
        int tier = GetTierFromScore(match.BaseScore);
        Color baseColor = _config.GetTierColor(tier);
        Color finalColor = _config.ApplyDecayToColor(baseColor, match.DaysActive);
        
        // Verificar se deve mostrar decay warning
        bool showDecay = _config.ShouldShowDecayWarning(match.BaseScore, match.DaysActive);
        
        _config.DebugLog($"Highlighting {match.DisplayName}: Tier {tier}, Decay {match.DaysActive}, Color {finalColor}");
        
        // Aplicar highlight em cada slot do padrão
        foreach (int slotIndex in match.SlotIndices)
        {
            HighlightSlot(slotIndex, finalColor);
        }
    }
    
    /// <summary>
    /// Aplica highlight em um slot específico (usa cache).
    /// </summary>
    private void HighlightSlot(int slotIndex, Color highlightColor)
    {
        StartCoroutine(HighlightSlotCoroutine(slotIndex, highlightColor));
    }
    
    /// <summary>
    /// Coroutine de highlight em um slot específico (versão cacheada).
    /// </summary>
    private IEnumerator HighlightSlotCoroutine(int slotIndex, Color highlightColor)
    {
        // Recachear se necessário
        if (!_slotsCached || _slotCache.Count == 0)
        {
            CacheSlots();
        }
        
        // Buscar slot no cache (O(1))
        if (!_slotCache.TryGetValue(slotIndex, out GridSlotView slotView))
        {
            Debug.LogWarning($"[PatternHighlightController] SlotView não encontrado no cache para index {slotIndex}");
            yield break;
        }
        
        if (slotView == null)
        {
            yield break;
        }
        
        // Cancelar highlight anterior neste slot (se existir)
        if (_activeHighlights.TryGetValue(slotIndex, out Coroutine oldCoroutine))
        {
            StopCoroutine(oldCoroutine);
        }
        
        // Executar animação de highlight
        yield return HighlightSlotRoutine(slotView, highlightColor);
        
        // Remover do dicionário de highlights ativos
        _activeHighlights.Remove(slotIndex);
    }
    
    /// <summary>
    /// Coroutine de highlight com pulse animation.
    /// USA A NOVA API DO GRIDSLOTVIEW (SetPatternHighlight).
    /// </summary>
    private IEnumerator HighlightSlotRoutine(GridSlotView slotView, Color highlightColor)
    {
        if (slotView == null) yield break;
        
        float elapsed = 0f;
        float duration = _config.highlightDuration;
        float pulseSpeed = _config.pulseSpeed;
        
        _config.DebugLog($"Highlighting slot {slotView.SlotIndex} for {duration}s with color {highlightColor}");
        
        // Pulse animation com cor modulada
        while (elapsed < duration)
        {
            // Debug: freeze animations
            if (_config.freezeAnimations)
            {
                yield return null;
                continue;
            }
            
            // PingPong para pulse
            float t = Mathf.PingPong(elapsed * pulseSpeed, 1f);
            
            // Modular alpha ou intensidade
            Color pulsedColor = highlightColor;
            pulsedColor.a = Mathf.Lerp(0.3f, 0.8f, t);
            
            // Aplicar via API pública
            slotView.SetPatternHighlight(pulsedColor, true);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Fade out suave (corrigido)
        float fadeElapsed = 0f;
        float fadeDuration = _config != null ? _config.highlightFadeDuration : 0.3f;
        
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            float t = fadeElapsed / fadeDuration; // 0 ? 1
            
            // Fade: 0.8 ? 0 (alpha)
            Color fadedColor = highlightColor;
            fadedColor.a = Mathf.Lerp(0.8f, 0f, t);
            
            slotView.SetPatternHighlight(fadedColor, true);
            yield return null;
        }
        
        // Limpar highlight
        slotView.ClearPatternHighlight();
        
        _config?.DebugLog($"Highlight finished for slot {slotView.SlotIndex}");
    }
    
    /// <summary>
    /// Determina tier baseado no score base do padrão.
    /// </summary>
    private int GetTierFromScore(int baseScore)
    {
        if (baseScore >= 80) return 4;  // Tier 4: 80-150 pts
        if (baseScore >= 35) return 3;  // Tier 3: 35-60 pts
        if (baseScore >= 15) return 2;  // Tier 2: 15-35 pts
        return 1;                        // Tier 1: 5-15 pts
    }
}
