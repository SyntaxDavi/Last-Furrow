using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controller responsável por REPRODUZIR VISUALMENTE os padrões já detectados.
/// 
/// RESPONSABILIDADE:
/// - Orquestrar animações de highlights, pop-ups e score
/// - Processar padrões de forma SEQUENCIAL (um de cada vez)
/// - Dentro de cada padrão, executar animações em PARALELO (highlight + text + score)
/// 
/// ARQUITETURA (SOLID):
/// - Recebe dados já calculados (PatternMatch + PatternScoreTotalResult)
/// - Não contém lógica de negócio, apenas animação
/// - Desacoplado via eventos e configuração centralizada
/// 
/// FLOW:
/// DetectPatternsStep (lógica) ? PatternVisualReplayController (visual)
/// 
/// PADRÃO DE USO:
/// yield return replayController.PlayReplay(matches, scoreResult);
/// </summary>
public class PatternVisualReplayController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private PatternVisualConfig _config;
    
    [Header("Dependencies")]
    [SerializeField] private PatternHighlightController _highlightController;
    [SerializeField] private PatternTextPopupController _textPopupController;
    
    // Estado
    private bool _isPlaying;
    
    private void Awake()
    {
        // Validações
        if (_config == null)
        {
            Debug.LogError("[PatternVisualReplay] PatternVisualConfig não atribuído!");
        }
        
        if (_highlightController == null)
        {
            _highlightController = FindFirstObjectByType<PatternHighlightController>();
            if (_highlightController == null)
            {
                Debug.LogWarning("[PatternVisualReplay] PatternHighlightController não encontrado na cena!");
            }
        }
        
        if (_textPopupController == null)
        {
            _textPopupController = FindFirstObjectByType<PatternTextPopupController>();
            if (_textPopupController == null)
            {
                Debug.LogWarning("[PatternVisualReplay] PatternTextPopupController não encontrado na cena!");
            }
        }
    }
    
    /// <summary>
    /// Reproduz visualmente todos os padrões detectados.
    /// 
    /// COMPORTAMENTO:
    /// - Padrões são mostrados SEQUENCIALMENTE (um após o outro)
    /// - Dentro de cada padrão: Highlight + Text + Score rodam em PARALELO
    /// - Delay configurável entre padrões
    /// </summary>
    public IEnumerator PlayReplay(List<PatternMatch> matches, PatternScoreTotalResult scoreResult)
    {
        if (_isPlaying)
        {
            _config?.DebugLog("[PatternVisualReplay] Já está reproduzindo, ignorando...");
            yield break;
        }
        
        if (matches == null || matches.Count == 0)
        {
            _config?.DebugLog("[PatternVisualReplay] Nenhum padrão para reproduzir");
            yield break;
        }
        
        _isPlaying = true;
        _config?.DebugLog($"[PatternVisualReplay] Iniciando replay de {matches.Count} padrões");
        
        // Processar cada padrão sequencialmente
        foreach (var match in matches)
        {
            yield return PlaySinglePattern(match, scoreResult);
            
            // Delay entre padrões (configurável)
            if (_config != null)
            {
                yield return new WaitForSeconds(_config.highlightDelayBetween);
            }
        }
        
        _isPlaying = false;
        _config?.DebugLog("[PatternVisualReplay] Replay concluído!");
    }
    
    /// <summary>
    /// Reproduz um único padrão com animações em paralelo.
    /// 
    /// ANIMAÇÕES SIMULTÂNEAS:
    /// - Highlight dos slots (piscar todos ao mesmo tempo)
    /// - Pop-up de texto (nome do padrão)
    /// - Pop-up de score (pontos ganhos)
    /// </summary>
    private IEnumerator PlaySinglePattern(PatternMatch match, PatternScoreTotalResult scoreResult)
    {
        _config?.DebugLog($"[PatternVisualReplay] Reproduzindo: {match.DisplayName} ({match.SlotIndices.Count} slots)");
        
        // Iniciar animações em paralelo
        Coroutine highlightCoroutine = null;
        Coroutine textCoroutine = null;
        Coroutine scoreCoroutine = null;
        
        // 1. Highlight dos slots
        if (_highlightController != null)
        {
            highlightCoroutine = StartCoroutine(_highlightController.HighlightPatternSlots(match));
        }
        
        // 2. Pop-up de texto (nome do padrão)
        if (_textPopupController != null)
        {
            textCoroutine = StartCoroutine(_textPopupController.ShowPatternName(match));
        }
        
        // 3. Pop-up de score (pontos)
        if (_textPopupController != null)
        {
            // Encontrar o resultado individual deste padrão
            var individualResult = FindIndividualResult(match, scoreResult);
            if (individualResult != null)
            {
                scoreCoroutine = StartCoroutine(_textPopupController.ShowPatternScore(match, individualResult));
            }
        }
        
        // Aguardar todas as animações terminarem
        if (highlightCoroutine != null) yield return highlightCoroutine;
        if (textCoroutine != null) yield return textCoroutine;
        if (scoreCoroutine != null) yield return scoreCoroutine;
        
        _config?.DebugLog($"[PatternVisualReplay] Padrão {match.DisplayName} concluído");
    }
    
    /// <summary>
    /// Encontra o resultado individual de um padrão específico.
    /// </summary>
    private PatternScoreResult FindIndividualResult(PatternMatch match, PatternScoreTotalResult scoreResult)
    {
        if (scoreResult == null || scoreResult.IndividualResults == null) return null;
        
        // Buscar por matching pattern ID
        foreach (var result in scoreResult.IndividualResults)
        {
            if (result.Match.PatternID == match.PatternID)
            {
                return result;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Cancela replay em andamento (se necessário).
    /// </summary>
    public void StopReplay()
    {
        if (_isPlaying)
        {
            StopAllCoroutines();
            _isPlaying = false;
            _config?.DebugLog("[PatternVisualReplay] Replay cancelado");
        }
    }
    
    private void OnDestroy()
    {
        StopReplay();
    }
}
