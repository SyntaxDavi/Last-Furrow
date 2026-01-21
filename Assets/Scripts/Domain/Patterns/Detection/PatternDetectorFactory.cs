using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Factory para criação de detectores de padrões.
/// SOLID: Open/Closed Principle - fácil adicionar novos detectores.
/// </summary>
public class PatternDetectorFactory
{
    private static List<IPatternDetector> _detectors;
    
    /// <summary>
    /// Retorna todos os detectores disponíveis, ordenados por prioridade.
    /// Detectores mais complexos/valiosos primeiro para evitar overlaps.
    /// </summary>
    public static List<IPatternDetector> GetAllDetectors()
    {
        if (_detectors == null)
        {
            _detectors = new List<IPatternDetector>
            {
                // Ordem de prioridade: mais complexo primeiro
                new CrossPatternDetector(),        // Tier 3: 35 pts
                new HorizontalLineDetector(),      // Tier 2: 15 pts
                new AdjacentPairDetector()         // Tier 1: 5 pts
            };
            
            Debug.Log($"[PatternDetectorFactory] {_detectors.Count} detectores registrados");
        }
        
        return _detectors;
    }
    
    /// <summary>
    /// Retorna detector específico por ID.
    /// </summary>
    public static IPatternDetector GetDetector(string patternID)
    {
        var detectors = GetAllDetectors();
        return detectors.Find(d => d.PatternID == patternID);
    }
    
    /// <summary>
    /// Adiciona detector customizado (para extensibilidade futura).
    /// </summary>
    public static void RegisterDetector(IPatternDetector detector)
    {
        var detectors = GetAllDetectors();
        
        // Evitar duplicatas
        if (detectors.Exists(d => d.PatternID == detector.PatternID))
        {
            Debug.LogWarning($"[PatternDetectorFactory] Detector '{detector.PatternID}' já registrado");
            return;
        }
        
        detectors.Add(detector);
        Debug.Log($"[PatternDetectorFactory] Detector '{detector.PatternID}' registrado");
    }
}
