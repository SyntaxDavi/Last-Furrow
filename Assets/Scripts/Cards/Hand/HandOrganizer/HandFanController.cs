using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

/// <summary>
/// Controlador responsável pela animação de Fan Out/Fan In das cartas durante a fase de análise.
/// Desacoplado do HandManager para melhor organização e separação de responsabilidades.
/// 
/// SENIOR FIX v2.0:
/// - Sistema de convergência híbrido (polling + timeout inteligente)
/// - Fallback por tempo máximo configurável
/// - Threshold adaptativo baseado na velocidade das cartas
/// - Logs detalhados para debugging
/// </summary>
public class HandFanController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private CardVisualConfig _visualConfig;
    [SerializeField] private HandLayoutConfig _layoutConfig;
    
    [Header("Convergence Settings (Override)")]
    [Tooltip("Threshold de distância para considerar carta convergida (0 = usa config)")]
    [SerializeField] private float _convergenceThresholdOverride = 0f;
    
    [Tooltip("Tempo máximo de espera por convergência em segundos")]
    [SerializeField] private float _maxConvergenceTime = 1.5f;
    
    [Tooltip("Tempo mínimo garantido antes de checar convergência")]
    [SerializeField] private float _minConvergenceTime = 0.3f;
    
    // Referência ao HandManager (injetada via Initialize)
    private HandManager _handManager;
    
    // Estado da transição
    private HandFanState _fanState = HandFanState.Normal;
    private Vector3 _fanVisualOffset = Vector3.zero;
    
    // Métricas de performance (para debugging)
    private float _lastConvergenceTime;
    private int _lastConvergenceIterations;
    
    // Propriedades públicas
    public HandFanState CurrentFanState => _fanState;
    public bool IsFannedOut => _fanState == HandFanState.FannedOut;
    public bool IsTransitioning => _fanState == HandFanState.FanningIn || _fanState == HandFanState.FanningOut;
    
    // Métricas expostas para debugging
    public float LastConvergenceTime => _lastConvergenceTime;
    public int LastConvergenceIterations => _lastConvergenceIterations;
    
    /// <summary>
    /// Inicializa o controller com referência ao HandManager.
    /// </summary>
    public void Initialize(HandManager handManager)
    {
        _handManager = handManager;
        
        // Obtém configs do HandManager se não foram atribuídos no Inspector
        if (_visualConfig == null)
            _visualConfig = handManager.GetVisualConfig();
        if (_layoutConfig == null)
            _layoutConfig = handManager.GetLayoutConfig();
    }
    
    /// <summary>
    /// Move todas as cartas para fora da tela (usado durante análise).
    /// Usa offset visual separado do layout lógico.
    /// Animação sequencial: uma carta por vez.
    /// </summary>
    public async UniTask FanOut()
    {
        // Guard: só pode iniciar FanOut se estiver em estado Normal
        var cards = _handManager.GetActiveCardsReadOnly();
        if (_fanState != HandFanState.Normal || cards.Count == 0) 
        {
            Debug.LogWarning($"[HandFanController] FanOut ignorado - estado atual: {_fanState}");
            return;
        }
        
        _fanState = HandFanState.FanningOut;
        
        // Notifica as cartas que estão em transição (desativa efeitos visuais)
        SetCardsTransitionMode(cards, true);
        
        // Usa offset do config (Inspector) ou fallback
        Vector3 fanOutOffset = _visualConfig?.FanOutOffset ?? new Vector3(-15f, -10f, 0f);
        _fanVisualOffset = fanOutOffset;
        
        // Delay entre cada carta (animação sequencial)
        float sequenceDelay = _visualConfig?.FanOutSequenceDelay ?? 0.12f;
        int count = cards.Count;
        
        // Animação sequencial: uma carta por vez (da direita para esquerda)
        for (int i = count - 1; i >= 0; i--)
        {
            var card = cards[i];
            if (card == null) continue;
            
            ApplyFanOffset(card, fanOutOffset);
            
            // Som com pitch variável (reutiliza o evento de draw)
            int sequenceIndex = count - 1 - i;
            _handManager.TriggerCardVisuallySpawned(sequenceIndex);
            
            // Delay entre cada carta
            if (sequenceDelay > 0)
                await UniTask.Delay((int)(sequenceDelay * 1000));
        }
        
        // Aguarda convergência com sistema híbrido robusto
        await WaitForCardsConvergenceRobust();
        
        _fanState = HandFanState.FannedOut;
        Debug.Log($"[HandFanController] FanOut complete em {_lastConvergenceTime:F2}s ({_lastConvergenceIterations} frames)");
    }
    
    /// <summary>
    /// Retorna as cartas para suas posições normais na mão, uma por uma (fan sequencial).
    /// </summary>
    public async UniTask FanIn()
    {
        // Guard: só pode iniciar FanIn se estiver em estado FannedOut
        if (_fanState != HandFanState.FannedOut) 
        {
            Debug.LogWarning($"[HandFanController] FanIn ignorado - estado atual: {_fanState}");
            return;
        }
        
        _fanState = HandFanState.FanningIn;
        
        // Delay inicial para dar o "respiro" visual (configurável)
        float preDelay = _visualConfig?.FanInPreDelay ?? 0.5f;
        await UniTask.Delay((int)(preDelay * 1000));
        
        var cards = _handManager.GetActiveCardsReadOnly();
        int count = cards.Count;
        if (count == 0)
        {
            _fanState = HandFanState.Normal;
            return;
        }
        
        // Animação sequencial: uma carta por vez
        float sequenceDelay = _visualConfig?.FanInSequenceDelay ?? 0.08f;
        
        for (int i = 0; i < count; i++)
        {
            var card = cards[i];
            if (card == null) continue;
            
            // Remove o offset visual desta carta (volta ao layout lógico)
            RemoveFanOffset(card, i, count);
            
            // Som com pitch variável
            _handManager.TriggerCardVisuallySpawned(i);
            
            // Delay entre cada carta (efeito fan)
            if (sequenceDelay > 0)
                await UniTask.Delay((int)(sequenceDelay * 1000));
        }
        
        // Aguarda convergência com sistema híbrido robusto
        await WaitForCardsConvergenceRobust();
        
        // Restaura efeitos visuais das cartas
        SetCardsTransitionMode(cards, false);
        
        _fanVisualOffset = Vector3.zero;
        _fanState = HandFanState.Normal;
        Debug.Log($"[HandFanController] FanIn complete em {_lastConvergenceTime:F2}s ({_lastConvergenceIterations} frames)");
    }
    
    /// <summary>
    /// Aplica offset visual a uma carta (separado do layout lógico).
    /// </summary>
    private void ApplyFanOffset(CardView card, Vector3 offset)
    {
        var baseTarget = card.BaseLayoutTarget;
        var offsetTarget = new HandLayoutCalculator.CardTransformTarget
        {
            Position = baseTarget.Position + offset,
            Rotation = baseTarget.Rotation,
            SortingOrder = baseTarget.SortingOrder
        };
        card.UpdateLayoutTarget(offsetTarget);
    }
    
    /// <summary>
    /// Remove offset visual de uma carta (volta ao layout lógico calculado).
    /// </summary>
    private void RemoveFanOffset(CardView card, int index, int totalCount)
    {
        var targetSlot = HandLayoutCalculator.CalculateSlot(
            index,
            totalCount,
            _layoutConfig,
            _handManager.GetHandCenterPosition()
        );
        card.UpdateLayoutTarget(targetSlot);
    }
    
    /// <summary>
    /// Notifica cartas sobre modo de transição (desativa/ativa efeitos visuais como flutuação).
    /// </summary>
    private void SetCardsTransitionMode(IReadOnlyList<CardView> cards, bool isTransitioning)
    {
        foreach (var card in cards)
        {
            if (card == null) continue;
            card.SetTransitionMode(isTransitioning);
        }
    }
    
    // =================================================================================
    // SISTEMA DE CONVERGÊNCIA ROBUSTO (v2.0)
    // =================================================================================
    
    /// <summary>
    /// Sistema híbrido de convergência:
    /// 1. Tempo mínimo garantido (para animação começar)
    /// 2. Polling de posição com threshold adaptativo
    /// 3. Fallback por tempo máximo (evita travamento)
    /// 4. Detecção de velocidade (se cartas pararam de mover)
    /// </summary>
    private async UniTask WaitForCardsConvergenceRobust()
    {
        var stopwatch = Stopwatch.StartNew();
        int iteration = 0;
        
        // Configurações
        float threshold = _convergenceThresholdOverride > 0 
            ? _convergenceThresholdOverride 
            : (_visualConfig?.ConvergenceThreshold ?? 0.15f);
        
        // Threshold mais generoso para evitar problemas com flutuação
        float effectiveThreshold = Mathf.Max(threshold, 0.15f);
        
        float minTime = _minConvergenceTime;
        float maxTime = _maxConvergenceTime;
        
        // Fase 1: Tempo mínimo garantido
        await UniTask.Delay((int)(minTime * 1000));
        
        // Fase 2: Polling com fallback por tempo
        while (stopwatch.Elapsed.TotalSeconds < maxTime)
        {
            iteration++;
            
            var convergenceResult = CheckConvergence(effectiveThreshold);
            
            if (convergenceResult.AllConverged)
            {
                // Sucesso! Todas as cartas chegaram
                _lastConvergenceTime = (float)stopwatch.Elapsed.TotalSeconds;
                _lastConvergenceIterations = iteration;
                return;
            }
            
            // Se a velocidade média é muito baixa, considera "good enough"
            if (convergenceResult.AverageVelocity < 0.5f && convergenceResult.MaxDistance < effectiveThreshold * 2f)
            {
                Debug.Log($"[HandFanController] Convergência por velocidade baixa (v={convergenceResult.AverageVelocity:F2}, d={convergenceResult.MaxDistance:F2})");
                _lastConvergenceTime = (float)stopwatch.Elapsed.TotalSeconds;
                _lastConvergenceIterations = iteration;
                return;
            }
            
            await UniTask.Yield();
        }
        
        // Fase 3: Timeout - não é erro, apenas log informativo
        _lastConvergenceTime = (float)stopwatch.Elapsed.TotalSeconds;
        _lastConvergenceIterations = iteration;
        
        var finalCheck = CheckConvergence(effectiveThreshold);
        Debug.Log($"[HandFanController] Convergência por timeout ({maxTime}s). MaxDist={finalCheck.MaxDistance:F2}, AvgVel={finalCheck.AverageVelocity:F2}");
    }
    
    /// <summary>
    /// Resultado da checagem de convergência com métricas detalhadas.
    /// </summary>
    private struct ConvergenceCheckResult
    {
        public bool AllConverged;
        public float MaxDistance;
        public float AverageVelocity;
        public int CardCount;
    }
    
    /// <summary>
    /// Verifica convergência de todas as cartas e retorna métricas.
    /// </summary>
    private ConvergenceCheckResult CheckConvergence(float threshold)
    {
        var result = new ConvergenceCheckResult
        {
            AllConverged = true,
            MaxDistance = 0f,
            AverageVelocity = 0f,
            CardCount = 0
        };
        
        var cards = _handManager.GetActiveCardsReadOnly();
        if (cards.Count == 0)
        {
            return result;
        }
        
        float totalVelocity = 0f;
        
        foreach (var card in cards)
        {
            if (card == null) continue;
            
            result.CardCount++;
            
            // Distância até o target (usa posição XY apenas, ignora Z que pode ter offset)
            Vector2 currentPos = card.transform.position;
            Vector2 targetPos = card.CurrentLayoutTarget.Position;
            float distance = Vector2.Distance(currentPos, targetPos);
            
            result.MaxDistance = Mathf.Max(result.MaxDistance, distance);
            
            if (distance > threshold)
            {
                result.AllConverged = false;
            }
            
            // Velocidade estimada (se o CardMovementController expor, usar diretamente)
            // Por ora, usamos a distância como proxy
            totalVelocity += distance;
        }
        
        if (result.CardCount > 0)
        {
            result.AverageVelocity = totalVelocity / result.CardCount;
        }
        
        return result;
    }
    
    /// <summary>
    /// Força reset do estado (para recuperação de erros).
    /// </summary>
    public void ForceReset()
    {
        _fanState = HandFanState.Normal;
        _fanVisualOffset = Vector3.zero;
        
        var cards = _handManager?.GetActiveCardsReadOnly();
        if (cards != null)
        {
            SetCardsTransitionMode(cards, false);
        }
        
        Debug.LogWarning("[HandFanController] Estado forçado para Normal.");
    }
}
