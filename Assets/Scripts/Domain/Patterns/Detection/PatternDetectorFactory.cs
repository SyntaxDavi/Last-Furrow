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
        var adjacentDef = library.GetPatternByID("ADJACENT_PAIR");
        var lineDef = library.GetPatternByID("FULL_LINE");
        var crossDef = library.GetPatternByID("CROSS");
        
        // Criar detectores com definitions (ordem de prioridade: mais complexo primeiro)
        if (crossDef != null)
        {
            _detectors.Add(new CrossPatternDetector(crossDef));
            Debug.Log($"[PatternDetectorFactory] Registered: {crossDef.DisplayName} ({crossDef.BaseScore}pts)");
        }
        
        if (lineDef != null)
        {
            _detectors.Add(new HorizontalLineDetector(lineDef));
            Debug.Log($"[PatternDetectorFactory] Registered: {lineDef.DisplayName} ({lineDef.BaseScore}pts)");
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


