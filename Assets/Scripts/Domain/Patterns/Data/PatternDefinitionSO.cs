using UnityEngine;

/// <summary>
/// ScriptableObject que define a configuração de um padrão específico.
/// 
/// FUNÇÃO:
/// - Armazenar metadados do padrão (ID, nome, pontos, tier)
/// - Configurável via Inspector (balanceamento fácil)
/// - Desacoplado da implementação (IGridPattern)
/// 
/// FILOSOFIA:
/// - ID estável (usado em SaveData) NUNCA muda
/// - DisplayName pode ser traduzido/modificado
/// - BaseScore é tunable sem tocar em código
/// 
/// USO:
/// Criar via: Assets ? Create ? Patterns ? Pattern Definition
/// </summary>
[CreateAssetMenu(fileName = "NewPatternDefinition", menuName = "Patterns/Pattern Definition", order = 1)]
public class PatternDefinitionSO : ScriptableObject
{
    [Header("Identificação")]
    [Tooltip("ID estável usado em SaveData. NUNCA mudar após release!")]
    public string PatternID = "PATTERN_ID";
    
    [Tooltip("Nome exibido na UI (pode ser traduzido)")]
    public string DisplayName = "Pattern Name";
    
    [Tooltip("Descrição curta do padrão")]
    [TextArea(2, 4)]
    public string Description = "Pattern description here.";
    
    [Header("Pontuação")]
    [Tooltip("Pontos base do padrão (antes de multiplicadores)")]
    [Range(5, 200)]
    public int BaseScore = 10;
    
    [Header("Classificação")]
    [Tooltip("Tier do padrão (1=Iniciante, 2=Casual, 3=Dedicado, 4=Master)")]
    [Range(1, 4)]
    public int Tier = 1;
    
    [Tooltip("Dificuldade visual (1-5 estrelas)")]
    [Range(1, 5)]
    public int DifficultyStars = 1;
    
    [Header("Visual (Futuro)")]
    [Tooltip("Ícone do padrão para UI")]
    public Sprite Icon;
    
    [Tooltip("Cor temática do padrão")]
    public Color ThemeColor = Color.white;
    
    [Header("Tipo de Implementação")]
    [Tooltip("Nome da classe que implementa este padrão (ex: 'AdjacentPairPattern')")]
    public string ImplementationClassName = "";
    
    /// <summary>
    /// Validação automática no Inspector.
    /// </summary>
    private void OnValidate()
    {
        // Garantir que PatternID está em UPPER_SNAKE_CASE
        if (!string.IsNullOrEmpty(PatternID))
        {
            string upper = PatternID.ToUpper().Replace(" ", "_");
            if (PatternID != upper)
            {
                Debug.LogWarning($"[PatternDefinition] PatternID deve estar em UPPER_SNAKE_CASE. Sugestão: {upper}");
            }
        }
        
        // Validar BaseScore baseado no Tier
        int minScore = Tier * 5;
        int maxScore = Tier * 50;
        if (BaseScore < minScore || BaseScore > maxScore)
        {
            Debug.LogWarning($"[PatternDefinition] BaseScore {BaseScore} fora do range esperado para Tier {Tier} ({minScore}-{maxScore})");
        }
    }
}
