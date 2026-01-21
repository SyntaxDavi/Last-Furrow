using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Factory para criação de detectores de padrões.
/// SOLID: Open/Closed Principle - fácil adicionar novos detectores.
/// Carrega definições de ScriptableObjects (Single Source of Truth).
/// </summary>
public class PatternDetectorFactory
{
    private static List<IPatternDetector> _detectors;
    private static bool _initialized;
    
    /// <summary>
    /// Inicializa a factory carregando definitions dos Resources.
    /// </summary>
    private static void Initialize()
    {
        if (_initialized) return;
        
        _detectors = new List<IPatternDetector>();
        
        // Carregar definitions dos Resources
        var adjacentDef = Resources.Load<PatternDefinitionSO>("Patterns/AdjacentPair");
        var lineDef = Resources.Load<PatternDefinitionSO>("Patterns/HorizontalLine");
        var crossDef = Resources.Load<PatternDefinitionSO>("Patterns/CrossPattern");
        
        // Criar detectores com definitions (ordem de prioridade: mais complexo primeiro)
        if (crossDef != null)
        {
            _detectors.Add(new CrossPatternDetector(crossDef));
        }
        else
        {
            Debug.LogWarning("[PatternDetectorFactory] CrossPattern definition not found! Creating with defaults.");
            _detectors.Add(new CrossPatternDetector(null));
        }
        
        if (lineDef != null)
        {
            _detectors.Add(new HorizontalLineDetector(lineDef));
        }
        else
        {
            Debug.LogWarning("[PatternDetectorFactory] HorizontalLine definition not found! Creating with defaults.");
            _detectors.Add(new HorizontalLineDetector(null));
        }
        
        if (adjacentDef != null)
        {
            _detectors.Add(new AdjacentPairDetector(adjacentDef));
        }
        else
        {
            Debug.LogWarning("[PatternDetectorFactory] AdjacentPair definition not found! Creating with defaults.");
            _detectors.Add(new AdjacentPairDetector(null));
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

