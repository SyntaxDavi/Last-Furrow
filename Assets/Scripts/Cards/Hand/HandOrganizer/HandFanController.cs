using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// Controlador responsável pela animação de Fan Out/Fan In das cartas durante a fase de análise.
/// Desacoplado do HandManager para melhor organização e separação de responsabilidades.
/// </summary>
public class HandFanController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private CardVisualConfig _visualConfig;
    [SerializeField] private HandLayoutConfig _layoutConfig;
    
    // Referência ao HandManager (injetada via Initialize)
    private HandManager _handManager;
    
    // Estado da transição
    private HandFanState _fanState = HandFanState.Normal;
    private Vector3 _fanVisualOffset = Vector3.zero;
    
    // Propriedades públicas
    public HandFanState CurrentFanState => _fanState;
    public bool IsFannedOut => _fanState == HandFanState.FannedOut;
    
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
            int sequenceIndex = count - 1 - i; // primeira carta a sair = 0
            _handManager.TriggerCardVisuallySpawned(sequenceIndex);
            
            // Delay entre cada carta
            if (sequenceDelay > 0)
                await UniTask.Delay((int)(sequenceDelay * 1000));
        }
        
        // Aguarda convergência REAL (polling de posição)
        await WaitForCardsConvergence();
        
        _fanState = HandFanState.FannedOut;
        Debug.Log("[HandFanController] FanOut complete - todas as cartas convergiram.");
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
            
            // Som com pitch variável (reutiliza o evento de draw)
            _handManager.TriggerCardVisuallySpawned(i);
            
            // Delay entre cada carta (efeito fan)
            if (sequenceDelay > 0)
                await UniTask.Delay((int)(sequenceDelay * 1000));
        }
        
        // Aguarda a última carta convergir
        await WaitForCardsConvergence();
        
        _fanVisualOffset = Vector3.zero;
        _fanState = HandFanState.Normal;
        Debug.Log("[HandFanController] FanIn complete - todas as cartas convergiram.");
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
        // Recalcula a posição lógica correta desta carta
        var targetSlot = HandLayoutCalculator.CalculateSlot(
            index,
            totalCount,
            _layoutConfig,
            _handManager.GetHandCenterPosition()
        );
        card.UpdateLayoutTarget(targetSlot);
    }
    
    /// <summary>
    /// Aguarda até que TODAS as cartas tenham convergido para seus targets.
    /// Polling real de posição, não delay baseado em tempo.
    /// </summary>
    private async UniTask WaitForCardsConvergence()
    {
        float threshold = _visualConfig?.ConvergenceThreshold ?? 0.1f;
        int maxIterations = 300; // Safety: ~5 segundos a 60fps
        int iteration = 0;
        
        while (iteration < maxIterations)
        {
            bool allConverged = true;
            var cards = _handManager.GetActiveCardsReadOnly();
            
            foreach (var card in cards)
            {
                if (card == null) continue;
                
                // Verifica distância entre posição atual e target
                float distance = Vector3.Distance(
                    card.transform.position, 
                    card.CurrentLayoutTarget.Position
                );
                
                if (distance > threshold)
                {
                    allConverged = false;
                    break;
                }
            }
            
            if (allConverged) return;
            
            await UniTask.Yield();
            iteration++;
        }
        
        Debug.LogWarning("[HandFanController] WaitForCardsConvergence timeout - algumas cartas não convergiram.");
    }
}
