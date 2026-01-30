using UnityEngine;

namespace LastFurrow.Visual.Camera
{
    /// <summary>
    /// Define a intenção de movimento/input para a câmera.
    /// Desacopla a câmera de mouse/teclado/resolução física.
    /// </summary>
    public interface ICameraInputProvider
    {
        /// <summary>
        /// Direção normalizada do "olhar" ou cursor relativo ao centro (-1 a 1).
        /// Usado para Edge Scroll e Perspective Lean.
        /// </summary>
        Vector2 LookIntent { get; }

        /// <summary>
        /// Define se o input sistemático está bloqueado (ex: Shop aberto).
        /// </summary>
        bool IsInputLocked { get; }
    }
}
