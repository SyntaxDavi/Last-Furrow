using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Gerenciador central de toda UI de patterns.
/// Coordena pop-ups, feedback de debug, highlights e eventos de decay.
/// </summary>
public class PatternUIManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _config;
    
    [Header("Pop-up References")]
    [SerializeField] private TextMeshProUGUI _patternNameText;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private CanvasGroup _popupCanvasGroup;
    
    [Header("Sub-Controllers")]
    [SerializeField] private PatternFeedbackView _debugFeedbackView;
    [SerializeField] private PatternHighlightController _highlightController;
    
    [Header("Debug")]
    [SerializeField] private bool _enableDebugFeedback = false;
    
    private void Awake()
    {
        if (_config == null)
        {
            _config = Resources.Load<PatternVisualConfig>("Patterns/PatternVisualConfig");
        }
        
        if (_popupCanvasGroup == null)
        {
            _popupCanvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }
        
        HidePopupImmediate();
    }
    
    private void OnEnable()
    {
        if (AppCore.Instance?.Events?.Pattern != null)
        {
            AppCore.Instance.Events.Pattern.OnPatternSlotCompleted += OnPatternCompleted;
            AppCore.Instance.Events.Pattern.OnPatternsDetected += OnPatternsDetected;
            AppCore.Instance.Events.Pattern.OnPatternDecayApplied += OnDecayApplied;
            AppCore.Instance.Events.Pattern.OnPatternRecreated += OnRecreated;
        }
    }
    
    private void OnDisable()
    {
        if (AppCore.Instance?.Events?.Pattern != null)
        {
            AppCore.Instance.Events.Pattern.OnPatternSlotCompleted -= OnPatternCompleted;
            AppCore.Instance.Events.Pattern.OnPatternsDetected -= OnPatternsDetected;
            AppCore.Instance.Events.Pattern.OnPatternDecayApplied -= OnDecayApplied;
            AppCore.Instance.Events.Pattern.OnPatternRecreated -= OnRecreated;
        }
    }
    
    private void OnPatternCompleted(PatternMatch match)
    {
        StartCoroutine(ShowPatternPopup(match));
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
    
    private IEnumerator ShowPatternPopup(PatternMatch match)
    {
        if (_patternNameText == null || _config == null)
        {
            Debug.LogError("[PatternUIManager] Missing popup references!");
            yield break;
        }
        
        _config.DebugLog($"Showing pattern: {match.DisplayName}");
        
        _patternNameText.text = match.DisplayName.ToUpper();
        
        int tier = CalculateTier(match.BaseScore);
        Color tierColor = _config.GetTierColor(tier);
        
        if (match.DaysActive > 1)
        {
            tierColor = _config.ApplyDecayToColor(tierColor, match.DaysActive);
        }
        
        _patternNameText.color = tierColor;
        
        if (_scoreText != null)
        {
            _scoreText.text = $"+{match.BaseScore}";
            _scoreText.color = tierColor;
            _scoreText.gameObject.SetActive(true);
        }
        
        yield return AnimatePopup();
    }
    
    private int CalculateTier(int baseScore)
    {
        if (baseScore >= 80) return 4;
        if (baseScore >= 35) return 3;
        if (baseScore >= 15) return 2;
        return 1;
    }
    
    private IEnumerator AnimatePopup()
    {
        transform.localScale = Vector3.one * _config.popupStartScale;
        _popupCanvasGroup.alpha = 0f;
        gameObject.SetActive(true);
        
        float duration = _config.popupAnimationDuration;
        float fadeInDuration = duration * 0.25f;
        float holdDuration = duration * 0.5f;
        float fadeOutDuration = duration * 0.25f;
        
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            
            _popupCanvasGroup.alpha = t;
            transform.localScale = Vector3.one * Mathf.Lerp(_config.popupStartScale, _config.popupEndScale, easeT);
            
            yield return null;
        }
        
        _popupCanvasGroup.alpha = 1f;
        transform.localScale = Vector3.one * _config.popupEndScale;
        
        yield return new WaitForSeconds(holdDuration);
        
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            _popupCanvasGroup.alpha = 1f - (elapsed / fadeOutDuration);
            yield return null;
        }
        
        HidePopupImmediate();
    }
    
    private void HidePopupImmediate()
    {
        if (_popupCanvasGroup != null)
        {
            _popupCanvasGroup.alpha = 0f;
        }
        gameObject.SetActive(false);
    }
}
