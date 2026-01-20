using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controller responsável por highlights de slots quando padrões são detectados.
/// 
/// RESPONSABILIDADE:
/// - Escutar evento OnPatternsDetected
/// - Enfileirar highlights via VisualQueueSystem
/// - Aplicar cores baseadas em Tier + Decay
/// - Animação de pulse nos slots
/// 
/// FILOSOFIA: Mostra VISUALMENTE quais slots formam padrões.
/// Usa VisualQueueSystem para evitar explosão visual.
/// </summary>
public class PatternHighlightController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _config;
    
    [Header("Grid Reference")]
    [Tooltip("Referência ao GridManager para acessar slots")]
    [SerializeField] private GridManager _gridManager;
    
    // Coroutines ativas por slot (para cancelar se necessário)
    private Dictionary<int, Coroutine> _activeHighlights = new Dictionary<int, Coroutine>();
    
    private void Start()
    {
        SubscribeToEvents();
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
    /// Aplica highlight em um slot específico.
    /// </summary>
    private void HighlightSlot(int slotIndex, Color highlightColor)
    {
        if (_gridManager == null)
        {
            Debug.LogWarning("[PatternHighlightController] GridManager não atribuído!");
            return;
        }
        
        // Obter GridSlotView via GetSlotViewAtIndex (API correta)
        var slots = _gridManager.GetComponentsInChildren<GridSlotView>();
        GridSlotView slotView = null;
        
        foreach (var slot in slots)
        {
            if (slot.SlotIndex == slotIndex)
            {
                slotView = slot;
                break;
            }
        }
        
        if (slotView == null)
        {
            Debug.LogWarning($"[PatternHighlightController] SlotView não encontrado para index {slotIndex}");
            return;
        }
        
        // Cancelar highlight anterior neste slot (se existir)
        if (_activeHighlights.TryGetValue(slotIndex, out Coroutine oldCoroutine))
        {
            StopCoroutine(oldCoroutine);
        }
        
        // Iniciar novo highlight
        Coroutine highlightCoroutine = StartCoroutine(HighlightSlotRoutine(slotView, highlightColor));
        _activeHighlights[slotIndex] = highlightCoroutine;
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
        
        // Fade out suave
        float fadeElapsed = 0f;
        float fadeDuration = 0.3f;
        
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            float t = 1f - (fadeElapsed / fadeDuration);
            
            Color fadedColor = highlightColor;
            fadedColor.a = Mathf.Lerp(0f, 0.8f, t);
            
            slotView.SetPatternHighlight(fadedColor, true);
            yield return null;
        }
        
        // Limpar highlight
        slotView.ClearPatternHighlight();
        
        // Remover do dicionário de highlights ativos
        _activeHighlights.Remove(slotView.SlotIndex);
        
        _config.DebugLog($"Highlight finished for slot {slotView.SlotIndex}");
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
