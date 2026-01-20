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
    
    [Header("Configuração")]
    [SerializeField] private float _displayDuration = 2.5f;
    [SerializeField] private float _fadeInDuration = 0.3f;
    [SerializeField] private float _fadeOutDuration = 0.5f;
    
    [Header("Cores por Tier de Pontos")]
    [SerializeField] private Color _lowScoreColor = new Color(0.8f, 0.8f, 0.8f);     // Cinza claro (< 50 pts)
    [SerializeField] private Color _mediumScoreColor = new Color(0.2f, 0.8f, 0.2f);  // Verde (50-150 pts)
    [SerializeField] private Color _highScoreColor = new Color(1f, 0.84f, 0f);       // Dourado (150-500 pts)
    [SerializeField] private Color _epicScoreColor = new Color(0.8f, 0.2f, 1f);      // Roxo (> 500 pts)
    
    private Coroutine _displayCoroutine;
    
    private void Awake()
    {
        // Garantir que começa invisível
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
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
        
        ShowFeedback(matches.Count, totalPoints);
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
        
        // Configurar texto
        string plural = patternCount > 1 ? "s" : "";
        _feedbackText.text = $"? {patternCount} Padrão{plural} = +{totalPoints} pts!";
        
        // Configurar cor baseada nos pontos
        _feedbackText.color = GetColorForScore(totalPoints);
        
        // Iniciar animação
        _displayCoroutine = StartCoroutine(DisplayRoutine());
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
        
        _displayCoroutine = null;
    }
}
