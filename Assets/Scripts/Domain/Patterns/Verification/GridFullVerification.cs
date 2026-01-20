using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Verificação INTEIRA do grid (scan completo de uma vez).
/// 
/// RESPONSABILIDADE (SOLID):
/// - Detectar TODOS os padrões no grid de uma vez
/// - Armazenar resultados no PatternDetectionCache
/// - NÃO fazer visual (apenas lógica)
/// 
/// FILOSOFIA:
/// - Single Responsibility: Apenas detecção completa
/// - Desacoplado de UI e animações
/// - Rápido (~10-50ms mesmo com grid 5x5)
/// 
/// FLOW:
/// Sleep ? GridFullVerification.Scan() ? PatternDetectionCache.StorePatterns()
/// </summary>
public class GridFullVerification
{
    private readonly IGridService _gridService;
    private readonly PatternDetector _detector;
    private readonly PatternTrackingService _trackingService;
    
    public GridFullVerification(
        IGridService gridService,
        PatternDetector detector,
        PatternTrackingService trackingService = null)
    {
        _gridService = gridService;
        _detector = detector;
        _trackingService = trackingService;
    }
    
    /// <summary>
    /// Escaneia grid completo e retorna todos os padrões detectados.
    /// Opcionalmente atualiza tracking (decay, identidade).
    /// </summary>
    public List<PatternMatch> Scan()
    {
        if (_gridService == null)
        {
            Debug.LogError("[GridFullVerification] GridService é NULL!");
            return new List<PatternMatch>();
        }
        
        if (_detector == null)
        {
            Debug.LogError("[GridFullVerification] PatternDetector é NULL!");
            return new List<PatternMatch>();
        }
        
        Debug.Log("[GridFullVerification] Iniciando scan completo do grid...");
        
        // Detectar todos os padrões
        List<PatternMatch> matches = _detector.DetectAll(_gridService);
        
        Debug.Log($"[GridFullVerification] {matches.Count} padrões detectados");
        
        // Atualizar tracking (se disponível)
        if (_trackingService != null && matches.Count > 0)
        {
            matches = _trackingService.UpdateActivePatterns(matches);
            Debug.Log($"[GridFullVerification] Tracking atualizado (decay, identidade)");
        }
        
        // Armazenar no cache
        if (PatternDetectionCache.Instance != null)
        {
            PatternDetectionCache.Instance.StorePatterns(matches);
            Debug.Log($"[GridFullVerification] Padrões armazenados no cache");
        }
        else
        {
            Debug.LogWarning("[GridFullVerification] PatternDetectionCache não disponível, padrões não foram cacheados!");
        }
        
        return matches;
    }
    
    /// <summary>
    /// Versão simplificada: apenas detecta sem tracking.
    /// </summary>
    public List<PatternMatch> ScanWithoutTracking()
    {
        if (_gridService == null || _detector == null)
        {
            return new List<PatternMatch>();
        }
        
        var matches = _detector.DetectAll(_gridService);
        
        // Armazenar no cache
        PatternDetectionCache.Instance?.StorePatterns(matches);
        
        return matches;
    }
}
