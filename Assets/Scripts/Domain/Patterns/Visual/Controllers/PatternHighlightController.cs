using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controller responsável APENAS por animações visuais de highlights.
/// 
/// RESPONSABILIDADE (SOLID - ONDA 5.5 REFACTORED):
/// - Escutar evento OnPatternSlotCompleted (scanner incremental)
/// - Aplicar cores e animações de pulse
/// - ZERO lógica de negócio (tier, score, etc vem nos dados)
/// 
/// FILOSOFIA:
/// - Apenas UI/Visual
/// - Recebe dados prontos (PatternMatch com tier/cor calculados)
/// - Sistema de cleanup para evitar vazamento entre dias
/// 
/// ANTI-PATTERN REMOVIDO:
/// - ? Não escuta OnPatternsDetected (lógica antiga)
/// - ? Não calcula tier/score
/// - ? Não usa VisualQueueSystem (scanner já faz isso)
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
    
    // Coroutines ativas (para cleanup/cancelamento)
    private List<Coroutine> _activeCoroutines = new List<Coroutine>();
    
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
        StopAllHighlights(); // Cleanup ao destruir
    }
    
    private void OnDisable()
    {
        StopAllHighlights(); // Cleanup ao desabilitar
    }
    
    private void SubscribeToEvents()
    {
        if (AppCore.Instance?.Events?.Pattern != null)
        {
            // ONDA 5.5: Escutar APENAS OnPatternSlotCompleted (scanner incremental)
            AppCore.Instance.Events.Pattern.OnPatternSlotCompleted += OnPatternSlotCompleted;
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        if (AppCore.Instance?.Events?.Pattern != null)
        {
            AppCore.Instance.Events.Pattern.OnPatternSlotCompleted -= OnPatternSlotCompleted;
        }
    }
    
    /// <summary>
    /// ONDA 5.5 (NOVO): Event handler quando scanner incremental encontra padrão.
    /// APENAS VISUAL - recebe dados prontos, não calcula nada.
    /// </summary>
    private void OnPatternSlotCompleted(PatternMatch match)
    {
        if (match == null || match.SlotIndices == null)
        {
            return;
        }
        
        if (_config == null)
        {
            Debug.LogWarning("[PatternHighlightController] Config não atribuído!");
            return;
        }
        
        // Obter cor baseada em tier + decay (cálculo centralizado no config)
        int tier = GetTierFromScore(match.BaseScore);
        Color baseColor = _config.GetTierColor(tier);
        Color finalColor = _config.ApplyDecayToColor(baseColor, match.DaysActive);
        
        _config?.DebugLog($"[PatternHighlight] Destacando {match.DisplayName} (Tier {tier})");
        
        // Aplicar highlight em todos os slots do padrão (paralelo)
        StartCoroutine(HighlightPatternSlots(match, finalColor));
    }
    
    /// <summary>
    /// Destaca visualmente os slots de um padrão específico.
    /// Todos os slots piscam JUNTOS (em paralelo).
    /// </summary>
    private IEnumerator HighlightPatternSlots(PatternMatch match, Color highlightColor)
    {
        if (match == null || match.SlotIndices == null)
        {
            yield break;
        }
        
        // Recachear se necessário
        if (!_slotsCached || _slotCache.Count == 0)
        {
            CacheSlots();
        }
        
        List<Coroutine> slotCoroutines = new List<Coroutine>();
        
        // Iniciar highlight em cada slot (paralelo)
        foreach (int slotIndex in match.SlotIndices)
        {
            if (_slotCache.TryGetValue(slotIndex, out GridSlotView slotView))
            {
                var coroutine = StartCoroutine(HighlightSlotRoutine(slotView, highlightColor));
                slotCoroutines.Add(coroutine);
                _activeCoroutines.Add(coroutine); // Track para cleanup
            }
            else
            {
                Debug.LogWarning($"[PatternHighlight] Slot {slotIndex} não encontrado no cache");
            }
        }
        
        // Aguardar todos os highlights terminarem
        foreach (var coroutine in slotCoroutines)
        {
            yield return coroutine;
            _activeCoroutines.Remove(coroutine); // Remover após concluir
        }
        
        _config?.DebugLog($"[PatternHighlight] Padrão {match.DisplayName} concluído");
    }
    
    /// <summary>
    /// Coroutine de highlight com pulse animation.
    /// USA A API DO GRIDSLOTVIEW (SetPatternHighlight).
    /// </summary>
    private IEnumerator HighlightSlotRoutine(GridSlotView slotView, Color highlightColor)
    {
        if (slotView == null) yield break;
        
        float elapsed = 0f;
        float duration = _config != null ? _config.highlightDuration : 1.5f;
        float pulseSpeed = _config != null ? _config.pulseSpeed : 2f;
        
        _config?.DebugLog($"[PatternHighlight] Slot {slotView.SlotIndex} pulsando por {duration}s");
        
        // FASE 1: Pulse animation
        while (elapsed < duration)
        {
            // Debug: freeze animations
            if (_config != null && _config.freezeAnimations)
            {
                yield return null;
                continue;
            }
            
            // PingPong para pulse
            float t = Mathf.PingPong(elapsed * pulseSpeed, 1f);
            
            // Modular alpha
            Color pulsedColor = highlightColor;
            pulsedColor.a = Mathf.Lerp(0.3f, 0.8f, t);
            
            // Aplicar via API
            slotView.SetPatternHighlight(pulsedColor, true);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // FASE 2: Fade out
        float fadeElapsed = 0f;
        float fadeDuration = _config != null ? _config.highlightFadeDuration : 0.3f;
        
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            float t = fadeElapsed / fadeDuration;
            
            Color fadedColor = highlightColor;
            fadedColor.a = Mathf.Lerp(0.8f, 0f, t);
            
            slotView.SetPatternHighlight(fadedColor, true);
            yield return null;
        }
        
        // Limpar highlight
        slotView.ClearPatternHighlight();
        
        _config?.DebugLog($"[PatternHighlight] Slot {slotView.SlotIndex} concluído");
    }
    
    /// <summary>
    /// CLEANUP: Para todas as animações em andamento.
    /// Chamado ao fim do dia ou ao desabilitar controller.
    /// PREVINE vazamento de animações entre dias.
    /// </summary>
    public void StopAllHighlights()
    {
        Debug.Log("[PatternHighlight] Parando todas as animações (cleanup)");
        
        // Parar todas as coroutines ativas
        foreach (var coroutine in _activeCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        
        _activeCoroutines.Clear();
        
        // Limpar highlights visuais de todos os slots
        if (_slotCache != null)
        {
            foreach (var kvp in _slotCache)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.ClearPatternHighlight();
                }
            }
        }
        
        Debug.Log("[PatternHighlight] Cleanup concluído");
    }
    
    /// <summary>
    /// Determina tier baseado no score base do padrão.
    /// (Helper simples - lógica real está no PatternVisualConfig)
    /// </summary>
    private int GetTierFromScore(int baseScore)
    {
        if (baseScore >= 80) return 4;
        if (baseScore >= 35) return 3;
        if (baseScore >= 15) return 2;
        return 1;
    }
}
