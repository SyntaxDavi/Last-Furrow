using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema de fila para eventos visuais com priorização.
/// 
/// RESPONSABILIDADE:
/// - Enfileirar eventos visuais (highlights, pop-ups, etc)
/// - Processar sequencialmente baseado em prioridade
/// - Agrupar eventos similares para evitar explosão visual
/// - Permitir interrupção de eventos de baixa prioridade
/// 
/// FILOSOFIA: Evitar sobrecarga visual quando múltiplos padrões detectados.
/// Critical events (sinergia) podem interromper Low priority (partículas).
/// </summary>
public class VisualQueueSystem : MonoBehaviour
{
    public static VisualQueueSystem Instance { get; private set; }
    
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _config;
    
    // Fila de eventos ordenada por prioridade
    private List<VisualEvent> _eventQueue;
    
    // Coroutine de processamento
    private Coroutine _processingCoroutine;
    
    // Flag de processamento
    private bool _isProcessing;
    
    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        _eventQueue = new List<VisualEvent>();
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    /// <summary>
    /// Enfileira um evento visual.
    /// </summary>
    public void Enqueue(VisualEvent visualEvent)
    {
        if (visualEvent == null)
        {
            Debug.LogWarning("[VisualQueueSystem] Tentativa de enfileirar evento NULL");
            return;
        }
        
        // Adicionar à fila
        _eventQueue.Add(visualEvent);
        
        // Ordenar por prioridade (Critical = 0 vem primeiro)
        _eventQueue.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        
        _config?.DebugLog($"Event enqueued: {visualEvent.EventType} (Priority: {visualEvent.Priority}, Queue: {_eventQueue.Count})");
        
        // Se não está processando, iniciar
        if (!_isProcessing)
        {
            StartProcessing();
        }
    }
    
    /// <summary>
    /// Inicia processamento da fila.
    /// </summary>
    private void StartProcessing()
    {
        if (_processingCoroutine != null)
        {
            StopCoroutine(_processingCoroutine);
        }
        
        _processingCoroutine = StartCoroutine(ProcessQueueRoutine());
    }
    
    /// <summary>
    /// Processa fila sequencialmente.
    /// </summary>
    private IEnumerator ProcessQueueRoutine()
    {
        _isProcessing = true;
        _config?.DebugLog("Queue processing STARTED");
        
        while (_eventQueue.Count > 0)
        {
            // Pegar próximo evento (já ordenado por prioridade)
            VisualEvent currentEvent = _eventQueue[0];
            _eventQueue.RemoveAt(0);
            
            _config?.DebugLog($"Processing: {currentEvent.EventType} (Priority: {currentEvent.Priority})");
            
            // Executar ação do evento
            currentEvent.Action?.Invoke();
            
            // Aguardar delay configurável
            float delay = GetDelayForPriority(currentEvent.Priority);
            
            // Debug: congelar animações pula delays
            if (_config != null && _config.freezeAnimations)
            {
                delay = 0f;
            }
            
            yield return new WaitForSeconds(delay);
        }
        
        _isProcessing = false;
        _config?.DebugLog("Queue processing FINISHED");
        _processingCoroutine = null;
    }
    
    /// <summary>
    /// Retorna delay baseado na prioridade.
    /// </summary>
    private float GetDelayForPriority(VisualEventPriority priority)
    {
        return priority switch
        {
            VisualEventPriority.Critical => 0.05f,   // Quase imediato
            VisualEventPriority.High => 0.1f,        // Rápido
            VisualEventPriority.Normal => 0.15f,     // Normal
            VisualEventPriority.Low => 0.2f,         // Lento
            _ => 0.15f
        };
    }
    
    /// <summary>
    /// Limpa a fila (útil para interrupções).
    /// </summary>
    public void Clear()
    {
        _eventQueue.Clear();
        
        if (_processingCoroutine != null)
        {
            StopCoroutine(_processingCoroutine);
            _processingCoroutine = null;
        }
        
        _isProcessing = false;
        _config?.DebugLog("Queue CLEARED");
    }
    
    /// <summary>
    /// Retorna tamanho atual da fila (para debug/metrics).
    /// </summary>
    public int GetQueueSize()
    {
        return _eventQueue.Count;
    }
}

/// <summary>
/// Prioridade de eventos visuais.
/// </summary>
public enum VisualEventPriority
{
    Critical = 0,    // Sinergia, Recreation Bonus
    High = 1,        // Highlights de padrões Tier 3-4
    Normal = 2,      // Pop-ups, Highlights Tier 1-2
    Low = 3          // Partículas ambientais
}

/// <summary>
/// Tipo de evento visual.
/// </summary>
public enum VisualEventType
{
    Highlight,
    Popup,
    Synergy,
    Particles,
    Combo,
    GridReaction
}

/// <summary>
/// Representa um evento visual na fila.
/// </summary>
public class VisualEvent
{
    public VisualEventType EventType { get; set; }
    public VisualEventPriority Priority { get; set; }
    public Action Action { get; set; }
    public object Data { get; set; } // Dados adicionais se necessário
    
    public VisualEvent(VisualEventType type, VisualEventPriority priority, Action action, object data = null)
    {
        EventType = type;
        Priority = priority;
        Action = action;
        Data = data;
    }
}
