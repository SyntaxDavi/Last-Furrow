using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// View responsável por mostrar feedback visual quando padrões são detectados.
/// 
/// FUNCIONALIDADES:
/// - Toast notification com total de pontos de padrões
/// - Fade in/out automático
/// - Cor diferente baseada na quantidade de pontos
/// - ONDA 4: Indicadores de decay e recreation bonus
/// 
/// EVENTOS ESCUTADOS:
/// - PatternEvents.OnPatternsDetected
/// 
/// POSIÇÃO: Deve ser colocado em um Canvas, preferencialmente no centro-topo da tela.
/// </summary>
public class PatternFeedbackView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _feedbackText;
    [SerializeField] private CanvasGroup _canvasGroup;
    
    [Header("UI References - Decay Info (Opcional)")]
    [SerializeField] private TextMeshProUGUI _decayInfoText;
    
    [Header("Configuração")]
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
    
    // Tracking de decay para exibição
    private int _patternsWithDecay = 0;
    private int _patternsRecreated = 0;
    private float _averageDecayMultiplier = 1f;
    
    private void Awake()
    {
        // Garantir que começa invisível
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }
        
        if (_decayInfoText != null)
        {
            _decayInfoText.text = "";
        }
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
        // Não mostrar se não houver padrões
        if (matches == null || matches.Count == 0 || totalPoints <= 0)
            return;
        
        // ONDA 4: Analisar decay e recreation
        AnalyzeDecayStatus(matches);
        
        ShowFeedback(matches.Count, totalPoints);
    }
    
    /// <summary>
    /// ONDA 4: Analisa status de decay dos padrões para exibição.
    /// </summary>
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
    /// Mostra o feedback de padrões detectados.
    /// </summary>
    public void ShowFeedback(int patternCount, int totalPoints)
    {
        if (_feedbackText == null || _canvasGroup == null)
        {
            Debug.LogWarning("[PatternFeedbackView] UI References não configuradas!");
            return;
        }
        
        // Parar animação anterior se existir
        if (_displayCoroutine != null)
        {
            StopCoroutine(_displayCoroutine);
        }
        
        // Configurar texto principal
        string plural = patternCount > 1 ? "s" : "";
        _feedbackText.text = $"? {patternCount} Padrão{plural} = +{totalPoints} pts!";
        
        // Configurar cor baseada nos pontos
        _feedbackText.color = GetColorForScore(totalPoints);
        
        // ONDA 4: Configurar texto de decay info
        UpdateDecayInfoText();
        
        // Iniciar animação
        _displayCoroutine = StartCoroutine(DisplayRoutine());
    }
    
    /// <summary>
    /// ONDA 4: Atualiza texto secundário com informações de decay.
    /// </summary>
    private void UpdateDecayInfoText()
    {
        if (_decayInfoText == null) return;
        
        var infoParts = new List<string>();
        
        // Mostrar padrões com decay
        if (_patternsWithDecay > 0)
        {
            int decayPercent = Mathf.RoundToInt((1f - _averageDecayMultiplier) * 100);
            string decayText = _patternsWithDecay == 1 
                ? $"? 1 padrão com decay (-{decayPercent}%)"
                : $"? {_patternsWithDecay} padrões com decay (média -{decayPercent}%)";
            infoParts.Add(decayText);
            _decayInfoText.color = _decayWarningColor;
        }
        
        // Mostrar padrões recriados
        if (_patternsRecreated > 0)
        {
            string recreatedText = _patternsRecreated == 1
                ? "?? 1 padrão recriado (+10%!)"
                : $"?? {_patternsRecreated} padrões recriados (+10%!)";
            infoParts.Add(recreatedText);
            
            // Se tem recreation, priorizar cor verde
            if (_patternsWithDecay == 0)
            {
                _decayInfoText.color = _recreationBonusColor;
            }
        }
        
        _decayInfoText.text = string.Join(" | ", infoParts);
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
        
        // Aguardar duração
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
        
        _displayCoroutine = null;
    }
}
