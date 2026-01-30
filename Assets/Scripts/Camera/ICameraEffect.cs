using UnityEngine;

namespace LastFurrow.Visual.Camera
{
    /// <summary>
    /// Interface para qualquer efeito que queira contribuir para o estado final da câmera.
    /// Segue o padrão de Composer/Stack para evitar race conditions.
    /// </summary>
    public interface ICameraEffect
    {
        /// <summary>
        /// Ordem de processamento (maior = processado por último/prioritário).
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// O efeito está ativo e deve contribuir para o frame atual?
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Retorna o deslocamento de posição sugerido por este efeito.
        /// </summary>
        Vector3 GetPositionOffset();

        /// <summary>
        /// Retorna a inclinação 3D (X e Y) sugerida por este efeito.
        /// </summary>
        Vector2 GetTiltOffset();

        /// <summary>
        /// Retorna a rotação Z (inércia/roll) sugerida por este efeito.
        /// </summary>
        float GetRotationOffset();

        /// <summary>
        /// Retorna a alteração no Orthographic Size sugerida por este efeito.
        /// </summary>
        float GetSizeOffset();
    }
}
