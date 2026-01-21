using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Factory para criação de detectores de padrões.
/// SOLID: Open/Closed Principle - fácil adicionar novos detectores.
/// Carrega definições de PatternDefinitionSO existentes.
/// </summary>
public class PatternDetectorFactory
{
    private static List<IPatternDetector> _detectors;
    private static bool _initialized;
    
    /// <summary>
    /// Inicializa a factory carregando definitions do caminho correto.
    /// </summary>
    private static void Initialize()
    {
        if (_initialized) return;
        
        _detectors = new List<IPatternDetector>();
        
        // Carregar PatternLibrary (contém todas as definitions)
        var library = Resources.Load<PatternLibrary>("PatternLibrary");
        
        if (library == null)
        {
            Debug.LogError("[PatternDetectorFactory] PatternLibrary not found in Resources!");
            _initialized = true;
            return;
        }
        
        // Buscar definitions específicas que temos detectores implementados
        // Ordem de prioridade: mais complexo/valioso primeiro (evitar que padrões simples "roubem" slots)
        
        // TIER 4 - Master (detectar primeiro para máxima pontuação)
        var perfectGridDef = library.GetPatternByID("PERFECT_GRID");
        
        // TIER 3 - Dedicado
        var rainbowDef = library.GetPatternByID("RAINBOW_LINE");
        var frameDef = library.GetPatternByID("FRAME");
        var diagonalDef = library.GetPatternByID("DIAGONAL");
        var crossDef = library.GetPatternByID("CROSS");
        
        // TIER 2 - Casual
        var lineDef = library.GetPatternByID("FULL_LINE");
        var checkerDef = library.GetPatternByID("CHECKER");
        
        // TIER 1 - Iniciante
        var trioDef = library.GetPatternByID("TRIO_LINE");
        var cornerDef = library.GetPatternByID("CORNER");
        var adjacentDef = library.GetPatternByID("ADJACENT_PAIR");
        
        // ====== TIER 4: Master ======
        if (perfectGridDef != null)
        {
            _detectors.Add(new PerfectGridDetector(perfectGridDef));
            Debug.Log($"[PatternDetectorFactory] Registered: {perfectGridDef.DisplayName} ({perfectGridDef.BaseScore}pts)");
        }
        
        // ====== TIER 3: Dedicado ======
        if (rainbowDef != null)
        {
            _detectors.Add(new RainbowLineDetector(rainbowDef));
            Debug.Log($"[PatternDetectorFactory] Registered: {rainbowDef.DisplayName} ({rainbowDef.BaseScore}pts)");
        }
        
        if (frameDef != null)
        {
            _detectors.Add(new FrameDetector(frameDef));
            Debug.Log($"[PatternDetectorFactory] Registered: {frameDef.DisplayName} ({frameDef.BaseScore}pts)");
        }
        
        if (diagonalDef != null)
        {
            _detectors.Add(new DiagonalDetector(diagonalDef));
            Debug.Log($"[PatternDetectorFactory] Registered: {diagonalDef.DisplayName} ({diagonalDef.BaseScore}pts)");
        }
        
        if (crossDef != null)
        {
            _detectors.Add(new CrossPatternDetector(crossDef));
            Debug.Log($"[PatternDetectorFactory] Registered: {crossDef.DisplayName} ({crossDef.BaseScore}pts)");
        }
        
        // ====== TIER 2: Casual ======
        if (lineDef != null)
        {
            _detectors.Add(new HorizontalLineDetector(lineDef));
            _detectors.Add(new VerticalLineDetector(lineDef));
            Debug.Log($"[PatternDetectorFactory] Registered: {lineDef.DisplayName} H+V ({lineDef.BaseScore}pts)");
        }
        
        if (checkerDef != null)
        {
            _detectors.Add(new CheckerDetector(checkerDef));
            Debug.Log($"[PatternDetectorFactory] Registered: {checkerDef.DisplayName} ({checkerDef.BaseScore}pts)");
        }
        
        // ====== TIER 1: Iniciante ======
        if (trioDef != null)
        {
            _detectors.Add(new TrioLineDetector(trioDef));
            Debug.Log($"[PatternDetectorFactory] Registered: {trioDef.DisplayName} ({trioDef.BaseScore}pts)");
        }
        
        if (cornerDef != null)
        {
            _detectors.Add(new CornerDetector(cornerDef));
            Debug.Log($"[PatternDetectorFactory] Registered: {cornerDef.DisplayName} ({cornerDef.BaseScore}pts)");
        }
        
        if (adjacentDef != null)
        {
            _detectors.Add(new AdjacentPairDetector(adjacentDef));
            Debug.Log($"[PatternDetectorFactory] Registered: {adjacentDef.DisplayName} ({adjacentDef.BaseScore}pts)");
        }
        
        Debug.Log($"[PatternDetectorFactory] {_detectors.Count} detectores registrados");
        _initialized = true;
    }
    
    /// <summary>
    /// Retorna todos os detectores disponíveis, ordenados por prioridade.
    /// </summary>
    public static List<IPatternDetector> GetAllDetectors()
    {
        Initialize();
        return _detectors;
    }
    
    /// <summary>
    /// Retorna detector específico por ID.
    /// </summary>
    public static IPatternDetector GetDetector(string patternID)
    {
        Initialize();
        return _detectors.Find(d => d.Definition.PatternID == patternID);
    }
    
    /// <summary>
    /// Adiciona detector customizado (para extensibilidade futura).
    /// </summary>
    public static void RegisterDetector(IPatternDetector detector)
    {
        Initialize();
        
        // Evitar duplicatas
        if (_detectors.Exists(d => d.Definition.PatternID == detector.Definition.PatternID))
        {
            Debug.LogWarning($"[PatternDetectorFactory] Detector '{detector.Definition.PatternID}' já registrado");
            return;
        }
        
        _detectors.Add(detector);
        Debug.Log($"[PatternDetectorFactory] Detector '{detector.Definition.PatternID}' registrado");
    }
}


