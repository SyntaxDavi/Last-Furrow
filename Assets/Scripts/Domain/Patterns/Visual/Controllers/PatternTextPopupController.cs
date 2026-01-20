using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Controller responsável por exibir pop-ups de texto dos padrões detectados.
/// 
/// RESPONSABILIDADE:
/// - Mostrar nome do padrão (ex: "CRUZ SIMPLES!")
/// - Mostrar pontos ganhos (ex: "+15")
/// - Animações: Scale up + Fade in/out (MANUAL - sem DOTween)
/// 
/// CONFIGURAÇÃO:
/// - TextMeshProUGUI (anexado ao mesmo GameObject ou filhos)
/// - CanvasGroup para fade
/// - Segue o mesmo padrão do PatternScoreHUDView
/// 
/// SETUP NO UNITY:
/// 1. Criar GameObject vazio no MainPanel: "PatternVisualSystem"
/// 2. Anexar este script
/// 3. Adicionar filhos com TextMeshProUGUI:
///    - "PatternNameText" (grande, centralizado)
///    - "PatternScoreText" (menor, abaixo do nome)
/// 4. CanvasGroup no GameObject principal
/// 
/// COMPORTAMENTO:
/// - Texto aparece DURANTE o highlight dos slots
/// - Cor baseada no Tier do padrão (calculado via BaseScore)
/// - Decay afeta a cor/opacidade
/// </summary>
public class PatternTextPopupController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _config;
    
    [Header("Text References (como PatternScoreHUDView)")]
    [Tooltip("Texto do nome do padrão (ex: 'LINHA!')")]
    [SerializeField] private TextMeshProUGUI _patternNameText;
    
    [Tooltip("Texto do score (ex: '+15')")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    
    [Tooltip("CanvasGroup para fade in/out")]
    [SerializeField] private CanvasGroup _canvasGroup;
    
    [Header("Animation Settings")]
    [Tooltip("Duração da animação completa (scale up + fade)")]
    [Range(0.5f, 3f)]
    [SerializeField] private float _animationDuration = 1.5f;
    
    [Tooltip("Escala inicial (antes do scale up)")]
    [Range(0f, 1f)]
    [SerializeField] private float _startScale = 0.5f;
    
    [Tooltip("Escala final")]
    [Range(1f, 2f)]
    [SerializeField] private float _endScale = 1f;
    
    private void Awake()
    {
        // Validações
        if (_config == null)
        {
            Debug.LogWarning("[PatternTextPopup] PatternVisualConfig não atribuído! Procurando...");
            _config = Resources.Load<PatternVisualConfig>("Patterns/PatternVisualConfig");
        }
        
        if (_patternNameText == null)
        {
            Debug.LogError("[PatternTextPopup] PatternNameText não atribuído! Procurando em filhos...");
            _patternNameText = transform.Find("PatternNameText")?.GetComponent<TextMeshProUGUI>();
        }
        
        if (_scoreText == null)
        {
            Debug.LogWarning("[PatternTextPopup] ScoreText não atribuído (opcional)");
        }
        
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                Debug.LogWarning("[PatternTextPopup] CanvasGroup não encontrado, adicionando...");
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // Estado inicial: invisível
        HideImmediate();
    }
    
    /// <summary>
    /// Mostra o nome do padrão com animação.
    /// </summary>
    public IEnumerator ShowPatternName(PatternMatch match)
    {
        if (_patternNameText == null)
        {
            Debug.LogError("[PatternTextPopup] PatternNameText é NULL! Configure no Inspector.");
            yield break;
        }
        
        _config?.DebugLog($"[PatternTextPopup] Mostrando nome: {match.DisplayName}");
        
        // Configurar texto
        _patternNameText.text = match.DisplayName.ToUpper();
        
        // Calcular Tier baseado no BaseScore
        int tier = CalculateTier(match.BaseScore);
        
        // Cor baseada no Tier
        Color tierColor = _config != null ? _config.GetTierColor(tier) : Color.white;
        
        // Aplicar decay na cor se necessário
        if (_config != null && match.DaysActive > 1)
        {
            tierColor = _config.ApplyDecayToColor(tierColor, match.DaysActive);
        }
        
        _patternNameText.color = tierColor;
        
        // Esconder score text por padrão
        if (_scoreText != null)
        {
            _scoreText.gameObject.SetActive(false);
        }
        
        // Animar
        yield return AnimatePopup();
    }
    
    /// <summary>
    /// Mostra o score do padrão com animação.
    /// </summary>
    public IEnumerator ShowPatternScore(PatternMatch match, PatternScoreResult scoreResult)
    {
        if (_scoreText == null)
        {
            _config?.DebugLog("[PatternTextPopup] ScoreText não configurado, pulando...");
            yield break;
        }
        
        _config?.DebugLog($"[PatternTextPopup] Mostrando score: +{scoreResult.FinalScore}");
        
        // Configurar texto
        string scoreTextString = $"+{scoreResult.FinalScore}";
        
        // Adicionar indicador de decay se aplicável
        if (scoreResult.HasDecay)
        {
            scoreTextString += $" (Dia {scoreResult.DaysActive})";
        }
        
        _scoreText.text = scoreTextString;
        _scoreText.gameObject.SetActive(true);
        
        // Calcular Tier
        int tier = CalculateTier(match.BaseScore);
        
        // Cor baseada no Tier
        Color tierColor = _config != null ? _config.GetTierColor(tier) : Color.white;
        
        if (_config != null && match.DaysActive > 1)
        {
            tierColor = _config.ApplyDecayToColor(tierColor, match.DaysActive);
        }
        
        _scoreText.color = tierColor;
        
        // Score text aparece junto com o nome (não precisa animar novamente)
        yield break;
    }
    
    /// <summary>
    /// Calcula o Tier baseado no BaseScore do padrão.
    /// RANGES (baseado no PatternVisualConfig):
    /// - Tier 1: 5-14 pts (Prata/Cinza)
    /// - Tier 2: 15-34 pts (Verde)
    /// - Tier 3: 35-79 pts (Dourado)
    /// - Tier 4: 80+ pts (Roxo Místico)
    /// </summary>
    private int CalculateTier(int baseScore)
    {
        if (baseScore >= 80) return 4;
        if (baseScore >= 35) return 3;
        if (baseScore >= 15) return 2;
        return 1;
    }
    
    /// <summary>
    /// Animação principal: Scale up + Fade in/out (MANUAL - sem DOTween).
    /// </summary>
    private IEnumerator AnimatePopup()
    {
        // Estado inicial
        transform.localScale = Vector3.one * _startScale;
        _canvasGroup.alpha = 0f;
        gameObject.SetActive(true);
        
        // FASE 1: Fade in + Scale up (25% da duração)
        float fadeInDuration = _animationDuration * 0.25f;
        float elapsed = 0f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            
            // Ease OutBack manual
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            
            _canvasGroup.alpha = t;
            transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _endScale, easeT);
            
            yield return null;
        }
        
        // Garantir valores finais
        _canvasGroup.alpha = 1f;
        transform.localScale = Vector3.one * _endScale;
        
        // FASE 2: Hold (50% da duração)
        float holdDuration = _animationDuration * 0.5f;
        yield return new WaitForSeconds(holdDuration);
        
        // FASE 3: Fade out (25% da duração)
        float fadeOutDuration = _animationDuration * 0.25f;
        elapsed = 0f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            
            _canvasGroup.alpha = 1f - t;
            
            yield return null;
        }
        
        // Esconder no final
        HideImmediate();
    }
    
    /// <summary>
    /// Esconde imediatamente sem animação.
    /// </summary>
    private void HideImmediate()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }
        
        gameObject.SetActive(false);
    }
}


