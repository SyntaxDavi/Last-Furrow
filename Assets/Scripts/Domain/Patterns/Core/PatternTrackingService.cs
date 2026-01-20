using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serviço responsável por gerenciar o tracking de padrões ativos e decay.
/// 
/// RESPONSABILIDADES:
/// - Manter lista de padrões ativos (ActivePatterns)
/// - Comparar padrões detectados com padrões anteriores (identidade)
/// - Incrementar DaysActive para padrões mantidos
/// - Resetar decay quando padrão é quebrado e recriado
/// - Aplicar bonus pós-reset (+10%)
/// - Reset semanal automático
/// 
/// FLUXO NO FIM DO DIA:
/// 1. DetectPatternsStep detecta padrões atuais
/// 2. PatternTrackingService.UpdateActivePatterns() compara com anteriores
/// 3. Padrões mantidos: DaysActive++
/// 4. Padrões novos: DaysActive = 1
/// 5. Padrões quebrados: removidos do tracking
/// 6. PatternScoreCalculator usa DaysActive para aplicar decay
/// 
/// PERSISTÊNCIA:
/// - ActivePatterns é salvo no RunData
/// - Carregado automaticamente ao iniciar run
/// </summary>
public class PatternTrackingService
{
    /// <summary>
    /// Dicionário de padrões ativos indexado por InstanceID.
    /// </summary>
    private Dictionary<string, PatternInstanceData> _activePatterns;
    
    /// <summary>
    /// Referência ao RunData para persistência.
    /// </summary>
    private readonly RunData _runData;
    
    /// <summary>
    /// Histórico de padrões que foram quebrados (para detectar recriação).
    /// Limpo no reset semanal.
    /// </summary>
    private HashSet<string> _brokenPatternIDs;
    
    public PatternTrackingService(RunData runData)
    {
        _runData = runData;
        _activePatterns = new Dictionary<string, PatternInstanceData>();
        _brokenPatternIDs = new HashSet<string>();
        
        // Carregar padrões ativos do RunData se existirem
        LoadFromRunData();
    }
    
    /// <summary>
    /// Carrega padrões ativos do RunData (persistência).
    /// </summary>
    private void LoadFromRunData()
    {
        if (_runData.ActivePatterns != null)
        {
            _activePatterns = new Dictionary<string, PatternInstanceData>(_runData.ActivePatterns);
            Debug.Log($"[PatternTracking] Carregados {_activePatterns.Count} padrões ativos do save");
        }
        
        if (_runData.BrokenPatternIDs != null)
        {
            _brokenPatternIDs = new HashSet<string>(_runData.BrokenPatternIDs);
        }
    }
    
    /// <summary>
    /// Salva padrões ativos no RunData (persistência).
    /// </summary>
    private void SaveToRunData()
    {
        _runData.ActivePatterns = new Dictionary<string, PatternInstanceData>(_activePatterns);
        _runData.BrokenPatternIDs = new List<string>(_brokenPatternIDs);
    }
    
    /// <summary>
    /// Atualiza o tracking de padrões com os padrões detectados no dia atual.
    /// 
    /// ALGORITMO:
    /// 1. Para cada padrão detectado, verificar se já existe no tracking
    /// 2. Se existe: incrementar DaysActive
    /// 3. Se não existe: criar novo com DaysActive = 1
    /// 4. Padrões no tracking que não foram detectados: marcar como quebrados
    /// </summary>
    /// <param name="detectedMatches">Padrões detectados no dia atual</param>
    /// <returns>Lista de PatternMatch com DaysActive preenchido</returns>
    public List<PatternMatch> UpdateActivePatterns(List<PatternMatch> detectedMatches)
    {
        int currentDay = _runData.CurrentDay;
        int currentWeek = _runData.CurrentWeek;
        
        var updatedMatches = new List<PatternMatch>();
        var detectedInstanceIDs = new HashSet<string>();
        
        // 1. Processar padrões detectados
        foreach (var match in detectedMatches)
        {
            string instanceID = PatternInstanceData.GenerateInstanceID(
                match.PatternID, 
                match.SlotIndices, 
                ConvertCropIDsToStrings(match.CropIDs)
            );
            
            detectedInstanceIDs.Add(instanceID);
            
            PatternInstanceData trackingData;
            
            if (_activePatterns.TryGetValue(instanceID, out var existingData))
            {
                // Padrão já existia - incrementar dias
                existingData.IncrementDaysActive();
                trackingData = existingData;
                
                Debug.Log($"[PatternTracking] Padrão mantido: {match.PatternID} (Dia {existingData.DaysActive})");
            }
            else
            {
                // Novo padrão - verificar se é recriação
                bool isRecreated = _brokenPatternIDs.Contains(match.PatternID);
                trackingData = PatternInstanceData.CreateFromMatch(match, currentDay, currentWeek, isRecreated);
                _activePatterns[instanceID] = trackingData;
                
                if (isRecreated)
                {
                    Debug.Log($"[PatternTracking] Padrão RECRIADO: {match.PatternID} (+10% bonus)");
                    _brokenPatternIDs.Remove(match.PatternID);
                }
                else
                {
                    Debug.Log($"[PatternTracking] Novo padrão: {match.PatternID}");
                }
            }
            
            // Criar PatternMatch atualizado com dados de tracking
            var updatedMatch = PatternMatch.Create(
                match.PatternID,
                match.DisplayName,
                match.SlotIndices,
                match.BaseScore,
                match.CropIDs,
                match.DebugDescription
            );
            updatedMatch.SetTrackingData(trackingData.DaysActive, trackingData.IsRecreated && trackingData.DaysActive == 1);
            
            updatedMatches.Add(updatedMatch);
        }
        
        // 2. Identificar padrões quebrados (estavam no tracking mas não foram detectados)
        var brokenPatterns = new List<string>();
        foreach (var kvp in _activePatterns)
        {
            if (!detectedInstanceIDs.Contains(kvp.Key))
            {
                brokenPatterns.Add(kvp.Key);
                _brokenPatternIDs.Add(kvp.Value.PatternID);
                
                Debug.Log($"[PatternTracking] Padrão QUEBRADO: {kvp.Value.PatternID}");
            }
        }
        
        // 3. Remover padrões quebrados do tracking
        foreach (var instanceID in brokenPatterns)
        {
            _activePatterns.Remove(instanceID);
        }
        
        // 4. Atualizar estatísticas no RunData
        UpdateStatistics(updatedMatches);
        
        // 5. Salvar no RunData
        SaveToRunData();
        
        Debug.Log($"[PatternTracking] Fim do dia: {_activePatterns.Count} padrões ativos, {brokenPatterns.Count} quebrados");
        
        return updatedMatches;
    }
    
    /// <summary>
    /// Reset semanal - limpa o histórico de padrões quebrados.
    /// Chamado no início de cada nova semana.
    /// </summary>
    public void OnWeeklyReset()
    {
        _brokenPatternIDs.Clear();
        
        // Opcional: Resetar decay de todos os padrões ativos
        // Isso é configurável - atualmente NÃO resetamos decay semanal
        // O documento diz que decay reseta semanalmente OU quando padrão é quebrado e recriado
        // Por segurança, vamos manter o decay mas limpar o histórico de quebrados
        
        Debug.Log("[PatternTracking] Reset semanal - histórico de quebrados limpo");
        
        SaveToRunData();
    }
    
    /// <summary>
    /// Atualiza estatísticas de padrões no RunData.
    /// </summary>
    private void UpdateStatistics(List<PatternMatch> matches)
    {
        // Contar total de padrões completados
        _runData.TotalPatternsDetected += matches.Count;
        
        // Atualizar contador por tipo
        foreach (var match in matches)
        {
            if (_runData.PatternCompletionCount == null)
                _runData.PatternCompletionCount = new Dictionary<string, int>();
            
            if (_runData.PatternCompletionCount.ContainsKey(match.PatternID))
                _runData.PatternCompletionCount[match.PatternID]++;
            else
                _runData.PatternCompletionCount[match.PatternID] = 1;
        }
    }
    
    /// <summary>
    /// Obtém o número de padrões atualmente ativos.
    /// </summary>
    public int GetActivePatternsCount()
    {
        return _activePatterns.Count;
    }
    
    /// <summary>
    /// Obtém dados de tracking para um padrão específico.
    /// </summary>
    public PatternInstanceData GetTrackingData(string instanceID)
    {
        _activePatterns.TryGetValue(instanceID, out var data);
        return data;
    }
    
    /// <summary>
    /// Helper para converter lista de CropID para lista de strings.
    /// </summary>
    private List<string> ConvertCropIDsToStrings(List<CropID> cropIDs)
    {
        var strings = new List<string>();
        if (cropIDs != null)
        {
            foreach (var cropID in cropIDs)
            {
                strings.Add(cropID.ToString());
            }
        }
        return strings;
    }
}
