using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Threading;

public class WeeklyGoalHUDView : UIView
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _goalText;

    // Formato do texto. {0} = Score Atual, {1} = Meta
    [SerializeField] private string _format = "Meta: {0} / {1}";
    
    [Header("Animation Settings")]
    [Tooltip("Duração da animação de contagem (segundos)")]
    [SerializeField] private float _countDuration = 0.5f;
    
    [Tooltip("Ativa animação de contador incremental")]
    [SerializeField] private bool _animateCount = true;
    
    // Estado interno
    private int _currentDisplayScore = 0;
    private int _targetScore = 0;
    private int _goalTarget = 0;
    private CancellationTokenSource _animationCts;

    private void OnEnable()
    {
        if (AppCore.Instance != null)
        {
            // 1. Escuta mudanças nos pontos (final do dia)
            AppCore.Instance.Events.Progression.OnScoreUpdated += UpdateDisplay;

            // 2. Escuta pontos incrementais de padrões (tempo real)
            AppCore.Instance.Events.Pattern.OnScoreIncremented += OnScoreIncremented;
            
            // 3. Escuta pontos passivos de crops (tempo real)
            AppCore.Instance.Events.Grid.OnCropPassiveScore += OnCropPassiveScore;

            // 4. Escuta mudanças de modo (Esconder em cutscenes, mostrar em jogo)
            AppCore.Instance.Events.UI.OnHUDModeChanged += HandleHUDMode;

            // 5. Atualização Inicial (Crucial para não começar vazio)
            RefreshImmediate();
        }
    }

    private void OnDisable()
    {
        // Cancela animação se estiver rodando
        _animationCts?.Cancel();
        _animationCts?.Dispose();
        _animationCts = null;
        
        if (AppCore.Instance != null)
        {
            AppCore.Instance.Events.Progression.OnScoreUpdated -= UpdateDisplay;
            AppCore.Instance.Events.Pattern.OnScoreIncremented -= OnScoreIncremented;
            AppCore.Instance.Events.Grid.OnCropPassiveScore -= OnCropPassiveScore;
            AppCore.Instance.Events.UI.OnHUDModeChanged -= HandleHUDMode;
        }
    }

    private void RefreshImmediate()
    {
        var run = AppCore.Instance.SaveManager.Data.CurrentRun;
        if (run != null)
        {
            _currentDisplayScore = run.CurrentWeeklyScore;
            _targetScore = run.CurrentWeeklyScore;
            _goalTarget = run.WeeklyGoalTarget;
            UpdateDisplay(run.CurrentWeeklyScore, run.WeeklyGoalTarget);
        }
    }
    
    /// <summary>
    /// Callback de pontos incrementais de padrões (tempo real).
    /// Anima contador enquanto padrões são detectados.
    /// </summary>
    private void OnScoreIncremented(int pointsAdded, int newTotal, int goal)
    {
        _targetScore = newTotal;
        _goalTarget = goal;
        
        if (_animateCount)
        {
            AnimateScoreCount().Forget();
        }
        else
        {
            // Atualização imediata (sem animação)
            _currentDisplayScore = newTotal;
            UpdateDisplayImmediate(newTotal, goal);
        }
    }
    
    /// <summary>
    /// ONDA 6.5: Callback de pontos passivos de crops (tempo real).
    /// Usa mesma animação que padrões.
    /// </summary>
    private void OnCropPassiveScore(int slotIndex, int cropPoints, int newTotal, int goal)
    {
        // Delega para mesmo método de animação
        OnScoreIncremented(cropPoints, newTotal, goal);
    }
    
    /// <summary>
    /// Anima contador de pontos suavemente (counter effect).
    /// </summary>
    private async UniTaskVoid AnimateScoreCount()
    {
        // Cancela animação anterior se estiver rodando
        _animationCts?.Cancel();
        _animationCts?.Dispose();
        _animationCts = new CancellationTokenSource();
        
        int startScore = _currentDisplayScore;
        int endScore = _targetScore;
        float elapsed = 0f;
        
        try
        {
            while (elapsed < _countDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _countDuration;
                
                // Interpolação linear (pode usar Ease para efeito melhor)
                int displayScore = (int)Mathf.Lerp(startScore, endScore, t);
                _currentDisplayScore = displayScore;
                
                UpdateDisplayImmediate(displayScore, _goalTarget);
                
                await UniTask.Yield(_animationCts.Token);
            }
            
            // Garante valor final exato
            _currentDisplayScore = endScore;
            UpdateDisplayImmediate(endScore, _goalTarget);
        }
        catch (System.OperationCanceledException)
        {
            // Animação cancelada (objeto desativado)
        }
    }

    private void UpdateDisplay(int currentScore, int targetScore)
    {
        // Atualização final (fim do dia)
        _currentDisplayScore = currentScore;
        _targetScore = currentScore;
        _goalTarget = targetScore;
        UpdateDisplayImmediate(currentScore, targetScore);
    }
    
    private void UpdateDisplayImmediate(int currentScore, int targetScore)
    {
        if (_goalText != null)
        {
            _goalText.text = string.Format(_format, currentScore, targetScore);

            // Opcional: Mudar cor se atingiu a meta
            if (currentScore >= targetScore)
                _goalText.color = Color.green;
            else
                _goalText.color = Color.white;
        }
    }

    private void HandleHUDMode(HUDMode mode)
    {
        // Regra visual: Mostra na Produção e na Loja. Esconde em Cutscenes/Menus.
        bool shouldShow = (mode == HUDMode.Production || mode == HUDMode.Shopping);

        if (shouldShow) Show();
        else Hide();
    }
}