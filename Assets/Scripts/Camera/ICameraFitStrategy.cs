using UnityEngine;

namespace LastFurrow.Visual.Camera
{
    /// <summary>
    /// Interface para estratégias de enquadramento de câmera.
    /// </summary>
    public interface ICameraFitStrategy
    {
        /// <summary>
        /// Calcula os bounds necessários para enquadrar o grid com padding.
        /// </summary>
        (float width, float height) CalculateRequiredBounds(
            GridConfiguration gridConfig,
            Vector2 gridSpacing,
            CameraFramingConfig framingConfig
        );
    }

    /// <summary>
    /// Implementação padrão: Grid centralizado com padding uniforme.
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
                Debug.LogError("[PaddedGridFitStrategy] GridConfiguration is NULL! Using fallback 10x10.");
                return (10f, 10f);
            }

            // 1. Tamanho real do grid em world units
            float gridWidth = gridConfig.Columns * gridSpacing.x;
            float gridHeight = gridConfig.Rows * gridSpacing.y;

            // 2. Adiciona padding POR LADO
            float totalWidth = gridWidth + framingConfig.PaddingLeft + framingConfig.PaddingRight;
            float totalHeight = gridHeight + framingConfig.PaddingTop + framingConfig.PaddingBottom;

            return (totalWidth, totalHeight);
        }
    }
}
