using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Escuta eventos de padrões e aplica highlights visuais nos slots.
/// </summary>
public class PatternHighlightController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _config;
    
    [Header("References")]
    [SerializeField] private GridManager _gridManager;
    
    private Dictionary<int, GridSlotView> _slotCache = new Dictionary<int, GridSlotView>();
    
    private void Awake()
    {
        if (_config == null)
        {
            _config = Resources.Load<PatternVisualConfig>("Patterns/PatternVisualConfig");
        }
    }
    
    private void Start()
    {
        CacheSlots();
        SubscribeToEvents();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    private void CacheSlots()
    {
        if (_gridManager == null) return;
        
        _slotCache.Clear();
        var slots = _gridManager.GetComponentsInChildren<GridSlotView>();
        
        foreach (var slot in slots)
        {
            if (slot != null)
            {
                _slotCache[slot.SlotIndex] = slot;
            }
        }
    }
    
    private void SubscribeToEvents()
    {
        if (AppCore.Instance?.Events?.Pattern != null)
        {
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
    
    private void OnPatternSlotCompleted(PatternMatch match)
    {
        if (match == null || _config == null || match.SlotIndices == null) return;
        
        int tier = CalculateTier(match.BaseScore);
        Color tierColor = _config.GetTierColor(tier);
        Color finalColor = _config.ApplyDecayToColor(tierColor, match.DaysActive);
        
        foreach (int slotIndex in match.SlotIndices)
        {
            if (_slotCache.TryGetValue(slotIndex, out GridSlotView slot))
            {
                StartCoroutine(HighlightSlot(slot, finalColor));
            }
        }
    }
    
    private IEnumerator HighlightSlot(GridSlotView slot, Color color)
    {
        if (slot == null) yield break;
        
        float elapsed = 0f;
        float duration = _config.highlightDuration;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            float t = Mathf.PingPong(elapsed * _config.highlightPulseSpeed, 1f);
            Color pulsedColor = color;
            pulsedColor.a = Mathf.Lerp(0.3f, 0.8f, t);
            
            slot.SetPatternHighlight(pulsedColor, true);
            
            yield return null;
        }
        
        // Fade out
        float fadeElapsed = 0f;
        float fadeDuration = _config.highlightFadeOutDuration;
        
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            float t = fadeElapsed / fadeDuration;
            
            Color fadedColor = color;
            fadedColor.a = Mathf.Lerp(0.8f, 0f, t);
            
            slot.SetPatternHighlight(fadedColor, true);
            yield return null;
        }
        
        slot.ClearPatternHighlight();
    }
    
    private int CalculateTier(int baseScore)
    {
        if (baseScore >= 80) return 4;
        if (baseScore >= 35) return 3;
        if (baseScore >= 15) return 2;
        return 1;
    }
}
