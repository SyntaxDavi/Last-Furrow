using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Controla o popup de texto que aparece quando um padrão é detectado.
/// HARDCODE: Usa animações manuais sem DOTween.
/// Referências diretas no Inspector, sem prefabs.
/// </summary>
public class PatternTextPopupController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _config;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _patternNameText;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private CanvasGroup _canvasGroup;
    
    [Header("Animation Settings")]
    [SerializeField] private float _animationDuration = 1.5f;
    [SerializeField] private float _startScale = 0.8f;
    [SerializeField] private float _endScale = 1.2f;
    
    private void Awake()
    {
        Debug.Log("[PatternTextPopup] ========== AWAKE ==========");
        
        if (_config == null)
        {
            _config = Resources.Load<PatternVisualConfig>("Patterns/PatternVisualConfig");
        }
        
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }
        
        HideImmediate();
    }
    
    /// <summary>
    /// MÉTODO PRINCIPAL: Mostra popup com animação completa.
    /// OBSOLETO: Use ShowPatternCoroutine() para chamada externa com coroutine.
    /// </summary>
    public void ShowPattern(PatternMatch match)
    {
        Debug.Log($"[PatternTextPopup] ?? ShowPattern: {match.DisplayName}");
        
        if (_patternNameText == null)
        {
            Debug.LogError("[PatternTextPopup] ? PatternNameText is NULL!");
            return;
        }
        
        StopAllCoroutines();
        StartCoroutine(ShowPatternRoutine(match));
    }
    
    /// <summary>
    /// MÉTODO PÚBLICO: Retorna coroutine para ser executada por um MonoBehaviour ativo.
    /// Resolve problema de GameObjects inativos tentando iniciar coroutines.
    /// </summary>
    public IEnumerator ShowPatternCoroutine(PatternMatch match)
    {
        Debug.Log($"[PatternTextPopup] ?? ShowPatternCoroutine: {match.DisplayName}");
        
        if (_patternNameText == null)
        {
            Debug.LogError("[PatternTextPopup] ? PatternNameText is NULL!");
            yield break;
        }
        
        yield return ShowPatternRoutine(match);
    }
    
    private IEnumerator ShowPatternRoutine(PatternMatch match)
    {
        // Configurar textos
        _patternNameText.text = match.DisplayName.ToUpper();
        
        int tier = CalculateTier(match.BaseScore);
        Color tierColor = _config != null ? _config.GetTierColor(tier) : Color.white;
        
        _patternNameText.color = tierColor;
        
        if (_scoreText != null)
        {
            _scoreText.text = $"+{match.BaseScore}";
            _scoreText.color = tierColor;
        }
        
        // Animar
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
        // Estado inicial
        transform.localScale = Vector3.one * _startScale;
        _canvasGroup.alpha = 0f;
        gameObject.SetActive(true);
        
        // FASE 1: Fade in + Scale up (25%)
        float fadeInDuration = _animationDuration * 0.25f;
        float elapsed = 0f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            
            _canvasGroup.alpha = t;
            transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _endScale, t);
            
            yield return null;
        }
        
        _canvasGroup.alpha = 1f;
        transform.localScale = Vector3.one * _endScale;
        
        // FASE 2: Hold (50%)
        yield return new WaitForSeconds(_animationDuration * 0.5f);
        
        // FASE 3: Fade out (25%)
        float fadeOutDuration = _animationDuration * 0.25f;
        elapsed = 0f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            
            _canvasGroup.alpha = 1f - t;
            
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
