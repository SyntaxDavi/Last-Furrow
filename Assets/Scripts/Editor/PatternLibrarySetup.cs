using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Menu helper para criar PatternLibrary e PatternDefinitions facilmente.
/// 
/// USO:
/// - Assets → Create → Patterns → Quick Setup All Patterns
/// - Cria automaticamente os 10 padrões base + PatternLibrary
/// </summary>
public class PatternLibrarySetup
{
    private const string PATTERN_DEFINITIONS_PATH = "Assets/Data/Patterns/Definitions";
    private const string PATTERN_LIBRARY_PATH = "Assets/Data/Patterns";
    
    /// <summary>
    /// Cria PatternLibrary + 10 PatternDefinitions de uma vez.
    /// </summary>
    [MenuItem("Assets/Create/Patterns/Quick Setup All Patterns", priority = 50)]
    public static void CreateAllPatterns()
    {
        // Criar diretórios se não existirem
        if (!Directory.Exists(PATTERN_DEFINITIONS_PATH))
        {
            Directory.CreateDirectory(PATTERN_DEFINITIONS_PATH);
        }
        
        if (!Directory.Exists(PATTERN_LIBRARY_PATH))
        {
            Directory.CreateDirectory(PATTERN_LIBRARY_PATH);
        }
        
        // Dados dos 10 padrões (ID, Nome, Pontos, Tier, Estrelas, Classe)
        var patternData = new[]
        {
            ("ADJACENT_PAIR", "Par Adjacente", 5, 1, 1, "AdjacentPairPattern"),
            ("TRIO_LINE", "Trio em Linha", 10, 1, 1, "TrioLinePattern"),
            ("CORNER", "Cantinho", 8, 1, 1, "GridCornerPattern"),
            ("FULL_LINE", "Linha Completa", 25, 2, 2, "FullLinePattern"),
            ("CHECKER", "Xadrez 2x2", 20, 2, 2, "CheckerPattern"),
            ("CROSS", "Cruz Simples", 30, 2, 2, "GridCrossPattern"),
            ("DIAGONAL", "Diagonal", 40, 3, 3, "DiagonalPattern"),
            ("FRAME", "Moldura", 50, 3, 3, "FramePattern"),
            ("RAINBOW_LINE", "Arco-íris", 55, 3, 4, "RainbowLinePattern"),
            ("PERFECT_GRID", "Grid Perfeito", 150, 4, 5, "PerfectGridPattern")
        };
        
        var createdPatterns = new System.Collections.Generic.List<PatternDefinitionSO>();
        
        // Criar cada PatternDefinition
        foreach (var data in patternData)
        {
            string assetPath = $"{PATTERN_DEFINITIONS_PATH}/{data.Item1}.asset";
            
            // Se já existe, usar o existente
            var existing = AssetDatabase.LoadAssetAtPath<PatternDefinitionSO>(assetPath);
            if (existing != null)
            {
                Debug.Log($"[PatternSetup] ✅ Padrão já existe: {data.Item1}");
                createdPatterns.Add(existing);
                continue;
            }
            
            // Criar novo
            var pattern = ScriptableObject.CreateInstance<PatternDefinitionSO>();
            pattern.PatternID = data.Item1;
            pattern.DisplayName = data.Item2;
            pattern.BaseScore = data.Item3;
            pattern.Tier = data.Item4;
            pattern.DifficultyStars = data.Item5;
            pattern.ImplementationClassName = data.Item6;
            pattern.Description = GenerateDescription(data.Item1);
            pattern.ThemeColor = GetTierColor(data.Item4);
            
            AssetDatabase.CreateAsset(pattern, assetPath);
            createdPatterns.Add(pattern);
            
            Debug.Log($"[PatternSetup] ✨ Criado: {data.Item1}");
        }
        
        // Criar PatternLibrary
        string libraryPath = $"{PATTERN_LIBRARY_PATH}/PatternLibrary.asset";
        var library = AssetDatabase.LoadAssetAtPath<PatternLibrary>(libraryPath);
        
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<PatternLibrary>();
            AssetDatabase.CreateAsset(library, libraryPath);
            Debug.Log($"[PatternSetup] ✨ Criado: PatternLibrary");
        }
        
        // Adicionar padrões à biblioteca via SerializedObject (suporta undo)
        SerializedObject so = new SerializedObject(library);
        SerializedProperty patternsProperty = so.FindProperty("_patterns");
        
        // Limpar lista antiga
        patternsProperty.ClearArray();
        
        // Adicionar todos os padrões
        for (int i = 0; i < createdPatterns.Count; i++)
        {
            patternsProperty.InsertArrayElementAtIndex(i);
            patternsProperty.GetArrayElementAtIndex(i).objectReferenceValue = createdPatterns[i];
        }
        
        so.ApplyModifiedProperties();
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // Selecionar PatternLibrary no Inspector
        Selection.activeObject = library;
        
        Debug.Log($"[PatternSetup] Setup completo! {createdPatterns.Count} padrões criados.");
        Debug.Log($"[PatternSetup] PatternLibrary em: {libraryPath}");
    }
    
    private static string GenerateDescription(string patternID)
    {
        switch (patternID)
        {
            case "ADJACENT_PAIR":
                return "Duas crops iguais lado a lado (horizontal ou vertical).";
            case "TRIO_LINE":
                return "Três crops iguais em linha reta.";
            case "CORNER":
                return "Três crops iguais formando um L nos cantos do grid.";
            case "FULL_LINE":
                return "Linha completa (5 crops) todas iguais.";
            case "CHECKER":
                return "Quatro crops alternadas em padrão ABAB (2x2).";
            case "CROSS":
                return "Cinco crops formando uma cruz (+): centro + 4 adjacentes.";
            case "DIAGONAL":
                return "Cinco crops iguais em diagonal completa (\\ ou /).";
            case "FRAME":
                return "Todas as 16 bordas do grid com a mesma crop.";
            case "RAINBOW_LINE":
                return "Linha com 3-5 tipos DIFERENTES de crops.";
            case "PERFECT_GRID":
                return "Todos os 25 slots plantados com mínimo 4 tipos diferentes.";
            default:
                return "Pattern description.";
        }
    }
    
    private static Color GetTierColor(int tier)
    {
        switch (tier)
        {
            case 1: return new Color(0.8f, 0.8f, 0.8f); // Cinza claro
            case 2: return new Color(0.2f, 0.8f, 0.2f); // Verde
            case 3: return new Color(1f, 0.84f, 0f);    // Dourado
            case 4: return new Color(0.8f, 0.2f, 1f);   // Roxo
            default: return Color.white;
        }
    }
}
