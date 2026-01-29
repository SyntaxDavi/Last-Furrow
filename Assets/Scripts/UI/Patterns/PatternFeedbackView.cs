using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// View respons�vel por mostrar feedback visual quando padr�es s�o detectados.
/// 
/// FUNCIONALIDADES:
/// - Toast notification com total de pontos de padr�es
/// - Fade in/out autom�tico
/// - Cor diferente baseada na quantidade de pontos
/// - ONDA 4: Indicadores de decay e recreation bonus
/// 
/// EVENTOS ESCUTADOS:
/// - PatternEvents.OnPatternsDetected
/// 
/// POSI��O: Deve ser colocado em um Canvas, preferencialmente no centro-topo da tela.
/// </summary>
public class PatternFeedbackView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _feedbackText;
    [SerializeField] private CanvasGroup _canvasGroup;
    
    [Header("UI References - Decay Info (Opcional)")]
    [SerializeField] private TextMeshProUGUI _decayInfoText;
    
    [Header("Configura��o")]
    [SerializeField] private float _displayDuration = 2.5f;
    [SerializeField] private float _fadeInDuration = 0.3f;
    [SerializeField] private float _fadeOutDuration = 0.5f;
    
    [Header("Cores por Tier de Pontos")]
    [SerializeField] private Color _lowScoreColor = new Color(0.8f, 0.8f, 0.8f);     // Cinza claro (< 50 pts)
    [SerializeField] private Color _mediumScoreColor = new Color(0.2f, 0.8f, 0.2f);  // Verde (50-150 pts)
    [SerializeField] private Color _highScoreColor = new Color(1f, 0.84f, 0f);       // Dourado (150-500 pts)
    [SerializeField] private Color _epicScoreColor = new Color(0.8f, 0.2f, 1f);      // Roxo (> 500 pts)
    
    [Header("Cores - Decay (ONDA 4)")]
    [SerializeField] private Color _decayWarningColor = new Color(1f, 0.6f, 0.2f);   // Laranja (decay ativo)
    [SerializeField] private Color _recreationBonusColor = new Color(0.2f, 1f, 0.6f); // Verde brilhante (+10%)
    
    private Coroutine _displayCoroutine;
    
    // Tracking de decay para exibi��o
    private int _patternsWithDecay = 0;
    private int _patternsRecreated = 0;
    private float _averageDecayMultiplier = 1f;
    
    private void Awake()
    {
        // Garantir que come�a invis�vel e DESATIVADO
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }
        
        if (_feedbackText != null)
        {
            _feedbackText.text = "";
        }
        
        if (_decayInfoText != null)
        {
            _decayInfoText.text = "";
        }
        
        // Desativar o GameObject no in�cio
        gameObject.SetActive(false);
    }
    
    private void OnEnable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Pattern.OnPatternsDetected += OnPatternsDetected;
        }
    }
    
    private void OnDisable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Pattern.OnPatternsDetected -= OnPatternsDetected;
        }
    }
    
    private void OnPatternsDetected(List<PatternMatch> matches, int totalPoints)
    {
        
        
        // No mostrar se no houver padres
        if (matches == null || matches.Count == 0 || totalPoints <= 0)
        {
            
            return;
        }
        
        // ONDA 4: Analisar decay e recreation
        AnalyzeDecayStatus(matches);
        
        ShowFeedback(matches.Count, totalPoints);
    }
    private void AnalyzeDecayStatus(List<PatternMatch> matches)
    {
        _patternsWithDecay = 0;
        _patternsRecreated = 0;
        float totalDecay = 0f;
        
        foreach (var match in matches)
        {
            if (match.DaysActive > 1)
            {
                _patternsWithDecay++;
                // Calcular decay multiplier: 0.9^(DaysActive - 1)
                float decay = Mathf.Pow(0.9f, match.DaysActive - 1);
                totalDecay += decay;
            }
            
            if (match.HasRecreationBonus)
            {
                _patternsRecreated++;
            }
        }
        
        _averageDecayMultiplier = _patternsWithDecay > 0 
            ? totalDecay / _patternsWithDecay 
            : 1f;
    }
    
    /// <summary>
    /// Mostra o feedback de padr�es detectados.
    /// </summary>
    public void ShowFeedback(int patternCount, int totalPoints)
    {
        if (_feedbackText == null || _canvasGroup == null)
        {
            Debug.LogWarning("[PatternFeedbackView] UI References n�o configuradas!");
            return;
        }
        
        // Ativar o GameObject antes de mostrar
        gameObject.SetActive(true);
        
        // Parar anima��o anterior se existir
        if (_displayCoroutine != null)
        {
            StopCoroutine(_displayCoroutine);
        }
        
        // Configurar texto principal
        string plural = patternCount > 1 ? "s" : "";
        _feedbackText.text = $"? {patternCount} Padr�o{plural} = +{totalPoints} pts!";
        
        // Configurar cor baseada nos pontos
        _feedbackText.color = GetColorForScore(totalPoints);
        
        // ONDA 4: Configurar texto de decay info
        UpdateDecayInfoText();
        
        // Iniciar anima��o
        _displayCoroutine = StartCoroutine(DisplayRoutine());
    }
    
    /// <summary>
    /// ONDA 4: Atualiza texto secund�rio com informa��es de decay.
    /// </summary>
    private void UpdateDecayInfoText()
    {
        if (_decayInfoText == null) return;
        
        var infoParts = new List<string>();
        
        // Mostrar padr�es com decay
        if (_patternsWithDecay > 0)
        {
            int decayPercent = Mathf.RoundToInt((1f - _averageDecayMultiplier) * 100);
            string decayText = _patternsWithDecay == 1 
                ? $"? 1 padr�o com decay (-{decayPercent}%)"
                : $"? {_patternsWithDecay} padr�es com decay (m�dia -{decayPercent}%)";
            infoParts.Add(decayText);
            _decayInfoText.color = _decayWarningColor;
        }
        
        // Mostrar padr�es recriados
        if (_patternsRecreated > 0)
        {
            string recreatedText = _patternsRecreated == 1
                ? "?? 1 padr�o recriado (+10%!)"
                : $"?? {_patternsRecreated} padr�es recriados (+10%!)";
            infoParts.Add(recreatedText);
            
            // Se tem recreation, priorizar cor verde
            if (_patternsWithDecay == 0)
            {
                _decayInfoText.color = _recreationBonusColor;
            }
        }

        if (_decayInfoText != null && infoParts.Count > 0)
        {
            _decayInfoText.text = string.Join("\n", infoParts);
        }

        
        
    }
    
    private Color GetColorForScore(int points)
    {
        if (points >= 500) return _epicScoreColor;
        if (points >= 150) return _highScoreColor;
        if (points >= 50) return _mediumScoreColor;
        return _lowScoreColor;
    }
    
    private IEnumerator DisplayRoutine()
    {
        // Fade In
        float elapsed = 0f;
        while (elapsed < _fadeInDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / _fadeInDuration);
            yield return null;
        }
        _canvasGroup.alpha = 1f;
        
        // Aguardar dura��o
        yield return new WaitForSeconds(_displayDuration);
        
        // Fade Out
        elapsed = 0f;
        while (elapsed < _fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / _fadeOutDuration);
            yield return null;
        }
        _canvasGroup.alpha = 0f;
        
        // Limpar texto de decay
        if (_decayInfoText != null)
        {
            _decayInfoText.text = "";
        }
        
        // Desativar GameObject ap�s anima��o
        gameObject.SetActive(false);
        
        _displayCoroutine = null;
    }
}
