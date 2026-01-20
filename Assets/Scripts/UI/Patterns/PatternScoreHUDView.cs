using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// View do HUD que mostra os pontos de padrões separadamente.
/// 
/// PROPÓSITO:
/// - Mostrar score de padrões separado do score geral
/// - Útil para debug e para o jogador entender a contribuição dos padrões
/// 
/// EVENTOS ESCUTADOS:
/// - PatternEvents.OnPatternsDetected
/// - UIEvents.OnHUDModeChanged
/// 
/// NOTA: Este componente é OPCIONAL. O sistema funciona sem ele.
/// É principalmente para feedback visual e debug.
/// </summary>
public class PatternScoreHUDView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _countText;  // Opcional
    [SerializeField] private CanvasGroup _canvasGroup;
    
    [Header("Configuração")]
    [SerializeField] private string _scoreFormat = "Padrões: +{0}";
    [SerializeField] private string _countFormat = "({0} detectados)";
    [SerializeField] private bool _hideWhenZero = true;
    
    // Tracking do último resultado
    private int _lastPatternScore = 0;
    private int _lastPatternCount = 0;
    
    public int LastPatternScore => _lastPatternScore;
    public int LastPatternCount => _lastPatternCount;
    
    private void OnEnable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Pattern.OnPatternsDetected += OnPatternsDetected;
            AppCore.Instance.Events.UI.OnHUDModeChanged += HandleHUDMode;
            
            // Estado inicial
            UpdateDisplay(0, 0);
        }
    }
    
    private void OnDisable()
    {
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Pattern.OnPatternsDetected -= OnPatternsDetected;
            AppCore.Instance.Events.UI.OnHUDModeChanged -= HandleHUDMode;
        }
    }
    
    private void OnPatternsDetected(List<PatternMatch> matches, int totalPoints)
    {
        int count = matches?.Count ?? 0;
        UpdateDisplay(totalPoints, count);
    }
    
    private void UpdateDisplay(int score, int count)
    {
        _lastPatternScore = score;
        _lastPatternCount = count;
        
        // Atualizar texto de score
        if (_scoreText != null)
        {
            _scoreText.text = string.Format(_scoreFormat, score);
            
            // Cor baseada no score
            if (score >= 100)
                _scoreText.color = Color.green;
            else if (score > 0)
                _scoreText.color = Color.white;
            else
                _scoreText.color = Color.gray;
        }
        
        // Atualizar texto de contagem (opcional)
        if (_countText != null)
        {
            _countText.text = string.Format(_countFormat, count);
        }
        
        // Esconder se zero (opcional)
        if (_hideWhenZero && _canvasGroup != null)
        {
            _canvasGroup.alpha = (score > 0) ? 1f : 0.3f;
        }
    }
    
    private void HandleHUDMode(HUDMode mode)
    {
        // Mostrar apenas durante produção
        bool shouldShow = (mode == HUDMode.Production);
        
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = shouldShow ? 1f : 0f;
        }
    }
    
    /// <summary>
    /// Reset para novo dia (chamado no início de cada dia).
    /// </summary>
    public void ResetForNewDay()
    {
        UpdateDisplay(0, 0);
    }
}
