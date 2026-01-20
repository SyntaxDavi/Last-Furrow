using System.Collections;
using UnityEngine;

/// <summary>
/// Controller responsável pela animação de "breathing" do grid + reações a eventos.
/// 
/// RESPONSABILIDADE:
/// - Idle breathing: Grid "respira" suavemente (escala oscila)
/// - Reações: Responder a Plant, Harvest, Pattern Detection
/// - Priority system: Pattern > Harvest > Plant
/// 
/// FILOSOFIA: Grid é uma "terra viva" que reage ao jogador.
/// Não usar Transform.DOTween - apenas coroutines + AnimationCurve.
/// </summary>
public class GridBreathingController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _config;
    
    [Header("Target")]
    [Tooltip("Transform do grid que será animado (geralmente parent de todos os slots)")]
    [SerializeField] private Transform _gridTransform;
    
    [Header("Breathing Curve")]
    [Tooltip("Curva de breathing (ease in/out para movimento orgânico)")]
    [SerializeField] private AnimationCurve _breathingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    // Estado
    private Vector3 _originalScale;
    private Coroutine _breathingCoroutine;
    private Coroutine _reactionCoroutine;
    
    // Reação ativa
    private ReactionType? _activeReaction;
    
    private void Awake()
    {
        if (_gridTransform == null)
        {
            _gridTransform = transform;
            Debug.LogWarning("[GridBreathingController] GridTransform não atribuído, usando transform próprio");
        }
        
        _originalScale = _gridTransform.localScale;
    }
    
    private void Start()
    {
        // Iniciar breathing idle
        StartBreathing();
        
        // Subscribe to events
        SubscribeToEvents();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    private void SubscribeToEvents()
    {
        if (AppCore.Instance?.Events?.Pattern != null)
        {
            AppCore.Instance.Events.Pattern.OnPatternsDetected += OnPatternsDetected;
        }
        
        // TODO: Subscribe to Plant/Harvest events quando implementados
        // AppCore.Instance.Events.Grid.OnPlantPlaced += OnPlantPlaced;
        // AppCore.Instance.Events.Grid.OnCropHarvested += OnCropHarvested;
    }
    
    private void UnsubscribeFromEvents()
    {
        if (AppCore.Instance?.Events?.Pattern != null)
        {
            AppCore.Instance.Events.Pattern.OnPatternsDetected -= OnPatternsDetected;
        }
    }
    
    /// <summary>
    /// Inicia a animação de breathing idle.
    /// </summary>
    private void StartBreathing()
    {
        if (_breathingCoroutine != null)
        {
            StopCoroutine(_breathingCoroutine);
        }
        
        _breathingCoroutine = StartCoroutine(BreathingRoutine());
    }
    
    /// <summary>
    /// Coroutine de breathing contínuo.
    /// </summary>
    private IEnumerator BreathingRoutine()
    {
        if (_config == null || _gridTransform == null)
        {
            yield break;
        }
        
        float time = 0f;
        
        while (true)
        {
            // Debug: freeze animations para ajuste
            if (_config.freezeAnimations)
            {
                yield return null;
                continue;
            }
            
            // Incrementar tempo
            time += Time.deltaTime * _config.breathingSpeed;
            
            // Avaliar curva (0-1 oscilando)
            float curveValue = _breathingCurve.Evaluate(Mathf.PingPong(time, 1f));
            
            // Aplicar breathing
            float scaleOffset = Mathf.Lerp(-_config.breathingAmount, _config.breathingAmount, curveValue);
            Vector3 newScale = _originalScale * (1f + scaleOffset);
            
            _gridTransform.localScale = newScale;
            
            yield return null;
        }
    }
    
    /// <summary>
    /// Trigger de reação (Plant, Harvest, Pattern).
    /// </summary>
    public void TriggerReaction(ReactionType reactionType)
    {
        // Priority system: só sobrescreve se prioridade maior
        if (_activeReaction != null && reactionType.GetPriority() > _activeReaction.Value.GetPriority())
        {
            _config?.DebugLog($"Reaction ignored: {reactionType} (priority lower than active {_activeReaction})");
            return;
        }
        
        // Cancelar reação anterior
        if (_reactionCoroutine != null)
        {
            StopCoroutine(_reactionCoroutine);
        }
        
        // Pausar breathing temporariamente
        if (_breathingCoroutine != null)
        {
            StopCoroutine(_breathingCoroutine);
        }
        
        // Executar nova reação
        _activeReaction = reactionType;
        _reactionCoroutine = StartCoroutine(ReactionRoutine(reactionType));
        
        _config?.DebugLog($"Reaction triggered: {reactionType}");
    }
    
    /// <summary>
    /// Coroutine de reação.
    /// AJUSTADO: Pulse mais suave e natural.
    /// </summary>
    private IEnumerator ReactionRoutine(ReactionType reactionType)
    {
        if (_config == null || _gridTransform == null)
        {
            yield break;
        }
        
        float strength = GetReactionStrength(reactionType);
        float duration = _config.reactionDuration;
        
        // Fase 1: Comprimir ou expandir baseado no tipo
        float elapsed = 0f;
        Vector3 startScale = _gridTransform.localScale;
        Vector3 targetScale = reactionType == ReactionType.Plant
            ? _originalScale * (1f - strength)  // Comprimir (thump)
            : _originalScale * (1f + strength); // Expandir (bounce)
        
        // Usar curva suave ao invés de linear
        while (elapsed < duration * 0.4f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration * 0.4f);
            
            // EaseOut para movimento mais natural
            float easedT = 1f - Mathf.Pow(1f - t, 2f);
            
            _gridTransform.localScale = Vector3.Lerp(startScale, targetScale, easedT);
            yield return null;
        }
        
        // Fase 2: Retornar ao normal com bounce suave
        elapsed = 0f;
        startScale = _gridTransform.localScale;
        
        while (elapsed < duration * 0.6f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration * 0.6f);
            
            // EaseOutBack para bounce sutil
            float easedT = t * t * ((1.70158f + 1f) * t - 1.70158f);
            easedT = Mathf.Clamp01(easedT);
            
            _gridTransform.localScale = Vector3.Lerp(startScale, _originalScale, easedT);
            yield return null;
        }
        
        // Garantir escala final
        _gridTransform.localScale = _originalScale;
        
        // Limpar estado
        _activeReaction = null;
        _reactionCoroutine = null;
        
        // Retomar breathing
        StartBreathing();
    }
    
    /// <summary>
    /// Retorna strength da reação baseado no tipo.
    /// </summary>
    private float GetReactionStrength(ReactionType reactionType)
    {
        return reactionType switch
        {
            ReactionType.Plant => _config.plantReactionStrength,
            ReactionType.Harvest => _config.harvestReactionStrength,
            ReactionType.Pattern => _config.patternReactionStrength,
            _ => 0.02f
        };
    }
    
    // === EVENT HANDLERS ===
    
    private void OnPatternsDetected(System.Collections.Generic.List<PatternMatch> matches, int totalPoints)
    {
        if (matches == null || matches.Count == 0) return;
        
        // Trigger reação de padrão (prioridade máxima)
        TriggerReaction(ReactionType.Pattern);
    }
    
    // TODO: Implementar quando eventos de Plant/Harvest existirem
    // private void OnPlantPlaced() => TriggerReaction(ReactionType.Plant);
    // private void OnCropHarvested() => TriggerReaction(ReactionType.Harvest);
}

/// <summary>
/// Tipo de reação do grid.
/// </summary>
public enum ReactionType
{
    Plant,
    Harvest,
    Pattern
}

/// <summary>
/// Extension methods para ReactionType.
/// </summary>
public static class ReactionTypeExtensions
{
    /// <summary>
    /// Retorna prioridade numérica (menor = maior prioridade).
    /// </summary>
    public static int GetPriority(this ReactionType reactionType)
    {
        return reactionType switch
        {
            ReactionType.Pattern => 0,  // Prioridade máxima
            ReactionType.Harvest => 1,  // Média
            ReactionType.Plant => 2,    // Baixa
            _ => 3
        };
    }
}
