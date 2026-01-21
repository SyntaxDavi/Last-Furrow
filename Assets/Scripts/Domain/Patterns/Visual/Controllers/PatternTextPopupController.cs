using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Exibe pop-ups de texto para padrões detectados.
/// Usa PatternVisualConfig para todas as configurações.
/// </summary>
public class PatternTextPopupController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _config;
    
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _patternNameText;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private CanvasGroup _canvasGroup;
    
    private void Awake()
    {
        if (_config == null)
        {
            _config = Resources.Load<PatternVisualConfig>("Patterns/PatternVisualConfig");
        }
        
        if (_patternNameText == null)
        {
            _patternNameText = transform.Find("PatternNameText")?.GetComponent<TextMeshProUGUI>();
        }
        
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }
        
        HideImmediate();
    }
    
    
    public IEnumerator ShowPatternName(PatternMatch match)
    {
        if (_patternNameText == null || _config == null)
        {
            Debug.LogError("[PatternTextPopup] Missing references!");
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
        _canvasGroup.alpha = 0f;
        gameObject.SetActive(true);
        
        float duration = _config.popupAnimationDuration;
        float fadeInDuration = duration * 0.25f;
        float holdDuration = duration * 0.5f;
        float fadeOutDuration = duration * 0.25f;
        
        // Fade in + Scale up
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            
            _canvasGroup.alpha = t;
            transform.localScale = Vector3.one * Mathf.Lerp(_config.popupStartScale, _config.popupEndScale, easeT);
            
            yield return null;
        }
        
        _canvasGroup.alpha = 1f;
        transform.localScale = Vector3.one * _config.popupEndScale;
        
        // Hold
        yield return new WaitForSeconds(holdDuration);
        
        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = 1f - (elapsed / fadeOutDuration);
            yield return null;
        }
        
        HideImmediate();
    }
    
    private void HideImmediate()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }
        gameObject.SetActive(false);
    }
}


