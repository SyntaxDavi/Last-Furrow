using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Threading;
using DG.Tweening;

public class WeeklyGoalHUDView : UIView
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _goalText;

    // Formato do texto. {0} = Score Atual, {1} = Meta
    [SerializeField] private string _format = "Meta: {0} / {1}";
    
    [Header("Animation Settings")]
    [Tooltip("Duração da animação de contagem (segundos)")]
    [SerializeField] private float _countDuration = 1f;
    
    [Tooltip("Ativa animação de contador incremental")]
    [SerializeField] private bool _animateCount = true;

    [Tooltip("Suavização da contagem. Linear fica mais 'mecânico'.")]
    [SerializeField] private Ease _countEase = Ease.Linear; 
    
    [Header("Juice Settings")]
    [Tooltip("Escala do 'soco'. X > 1 estica, Y > 1 estica. Use valores altos para impacto.")]
    [SerializeField] private Vector3 _punchStrength = new Vector3(1.5f, 1.5f, 1f); // 50% maior
    [Tooltip("Duração do impacto")]
    [SerializeField] private float _punchDuration = 0.25f;
    [Tooltip("Vibrato: quanto maior, mais balança. Elastico: 0 a 1.")]
    [SerializeField] private int _punchVibrato = 10;
    [SerializeField] private float _punchElasticity = 1f;
    
    // Estado interno
    private int _currentDisplayScore = 0;
    private int _targetScore = 0;
    private int _goalTarget = 0;
    private CancellationTokenSource _animationCts;

    // Tween references
    private Tween _scoreTween;
    private Tween _scaleTween;

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
        
        _scoreTween?.Kill();
        _scaleTween?.Kill();

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
        UpdateDisplay(newTotal, goal);
    }
    
    /// <summary>
    /// ONDA 6.5: Callback de pontos passivos de crops (tempo real).
    /// Usa mesma animação que padrões.
    /// </summary>
    private void OnCropPassiveScore(int slotIndex, int cropPoints, int newTotal, int goal)
    {
        OnScoreIncremented(cropPoints, newTotal, goal);
    }
    
    private void UpdateDisplay(int currentScore, int targetScore)
    {
        // Atualização final (Move o target, o tween persegue)
        _targetScore = currentScore; // O valor final real
        _goalTarget = targetScore;

        if (_animateCount && gameObject.activeInHierarchy)
        {
            // Mata tween anterior de valor para iniciar um novo do ponto atual
            _scoreTween?.Kill();

            // Rola do valor visual atual até o novo target
            _scoreTween = DOTween.To(
                () => _currentDisplayScore, 
                x => {
                    _currentDisplayScore = x;
                    UpdateDisplayImmediate(_currentDisplayScore, _goalTarget);
                }, 
                _targetScore, 
                _countDuration
            )
            .SetEase(_countEase)
            .SetLink(gameObject)
            .OnComplete(() => {
                _currentDisplayScore = _targetScore;
                UpdateDisplayImmediate(_currentDisplayScore, _goalTarget);
            });

            // Punch Effect SUPER (Juice Update)
            if (_goalText != null)
            {
                // Completa o anterior para garantir que o novo soma/sobrescreve do tamanho normal
                _scaleTween?.Complete();
                
                // NOTA: DOPunchScale usa o valor 'punch' como o ADICIONAL.
                // Se Strength for (1.5, 1.5, 1), queremos adicionar (0.5, 0.5, 0).
                Vector3 punchVector = _punchStrength - Vector3.one;
                
                _scaleTween = _goalText.transform.DOPunchScale(punchVector, _punchDuration, _punchVibrato, _punchElasticity)
                    .SetLink(_goalText.gameObject);
                
                // Mantém o Flash Dourado
               _goalText.DOColor(new Color(1f, 0.8f, 0.2f), 0.1f).SetLoops(2, LoopType.Yoyo);
            }
        }
        else
        {
            // Atualização imediata se animação não estiver ativa
            _currentDisplayScore = currentScore;
            UpdateDisplayImmediate(currentScore, targetScore);
        }
    }
    
    // Context Menu para testar no Editor
    [ContextMenu("Test Punch")]
    public void TestPunch()
    {
        if (_goalText != null)
        {
            _scaleTween?.Complete();
            _goalText.transform.localScale = Vector3.one; // Reset base
            
            Vector3 punchVector = _punchStrength - Vector3.one;
            _scaleTween = _goalText.transform.DOPunchScale(punchVector, _punchDuration, _punchVibrato, _punchElasticity)
                .SetLink(_goalText.gameObject);
                
             _goalText.DOColor(new Color(1f, 0.8f, 0.2f), 0.1f).SetLoops(2, LoopType.Yoyo);
        }
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