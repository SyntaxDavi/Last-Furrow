using UnityEngine;

/// <summary>
/// Interface para estratégias de enquadramento de câmera.
/// 
/// SOLID: Strategy Pattern
/// - Permite diferentes formas de calcular bounds necessários
/// - Testável isoladamente
/// - Extensível sem modificar código existente
/// 
/// RESPONSABILIDADE:
/// - Recebe dados do mundo (grid, spacing, padding)
/// - Retorna bounds necessários (width, height)
/// - NÃO aplica na câmera (isso é responsabilidade do Controller)
/// </summary>
public interface ICameraFitStrategy
{
    /// <summary>
    /// Calcula os bounds necessários para enquadrar o grid com padding.
    /// </summary>
    /// <param name="gridConfig">Configuração do grid (dimensões)</param>
    /// <param name="gridSpacing">Espaçamento entre slots em world units</param>
    /// <param name="framingConfig">Configuração de padding e pixel perfect</param>
    /// <returns>(width, height) em world units</returns>
    (float width, float height) CalculateRequiredBounds(
        GridConfiguration gridConfig,
        Vector2 gridSpacing,
        CameraFramingConfig framingConfig
    );
}

/// <summary>
/// Implementação padrão: Grid centralizado com padding uniforme.
/// 
/// LÓGICA:
/// 1. Calcula tamanho real do grid (cols × spacing.x, rows × spacing.y)
/// 2. Adiciona padding em todos os lados
/// 3. Retorna bounds totais necessários
/// 
/// FILOSOFIA:
/// - "Mundo é protagonista" ? Padding generoso para ambientação
/// - "Grid é estrutura" ? Centralizado, protegido
/// </summary>
public class PaddedGridFitStrategy : ICameraFitStrategy
{
    public (float width, float height) CalculateRequiredBounds(
        GridConfiguration gridConfig,
        Vector2 gridSpacing,
        CameraFramingConfig framingConfig)
    {
        if (gridConfig == null)
        {
            Debug.LogError("[PaddedGridFitStrategy] ?? GridConfiguration é NULL! Usando fallback 10×10.");
            Debug.LogError("[PaddedGridFitStrategy] ? CAUSA PROVÁVEL: GridConfiguration não atribuída no AppCore ou Bootstrap falhou.");
            return (10f, 10f);
        }

        // 1. Tamanho real do grid em world units
        float gridWidth = gridConfig.Columns * gridSpacing.x;
        float gridHeight = gridConfig.Rows * gridSpacing.y;

        // 2. ? Adiciona padding POR LADO (permite composição assimétrica)
        float totalWidth = gridWidth + framingConfig.PaddingLeft + framingConfig.PaddingRight;
        float totalHeight = gridHeight + framingConfig.PaddingTop + framingConfig.PaddingBottom;

        return (totalWidth, totalHeight);
    }
}
