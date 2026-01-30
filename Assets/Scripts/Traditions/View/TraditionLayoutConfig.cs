using UnityEngine;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// Configuração de layout para as tradições no topo da tela.
    /// Similar ao HandLayoutConfig mas para a barra de tradições.
    /// </summary>
    [CreateAssetMenu(fileName = "TraditionLayoutConfig", menuName = "Last Furrow/Traditions/Layout Config")]
    public class TraditionLayoutConfig : ScriptableObject
    {
        [Header("Position")]
        [Tooltip("Posição Y do centro da barra de tradições (em world space)")]
        public float verticalPosition = 4.5f;
        
        [Tooltip("Posição X central da barra")]
        public float horizontalCenter = 0f;
        
        [Header("Spacing")]
        [Tooltip("Espaço entre cada tradição")]
        public float spacing = 1.2f;
        
        [Tooltip("Escala das tradições (menor que cartas normais)")]
        public float scale = 0.7f;
        
        [Header("Limits")]
        [Tooltip("Máximo de tradições que podem ser exibidas")]
        public int maxTraditions = 5;
        
        [Header("Animation")]
        [Tooltip("Duração da animação de spawn")]
        public float spawnDuration = 0.3f;
        
        [Tooltip("Duração da animação de rearranjo")]
        public float rearrangeDuration = 0.2f;
        
        [Tooltip("Curva de animação do spawn")]
        public AnimationCurve spawnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Visual Effects")]
        [Tooltip("Hover eleva a tradição")]
        public float hoverElevation = 0.2f;
        
        [Tooltip("Intensidade do glow ao passar o mouse")]
        public float hoverGlowIntensity = 1.5f;
        
        /// <summary>
        /// Calcula a posição X de uma tradição baseado no índice e total.
        /// </summary>
        public float GetPositionX(int index, int total)
        {
            if (total <= 0) return horizontalCenter;
            
            // Centraliza as tradições
            float totalWidth = (total - 1) * spacing;
            float startX = horizontalCenter - (totalWidth / 2f);
            
            return startX + (index * spacing);
        }
        
        /// <summary>
        /// Retorna a posição world completa para uma tradição.
        /// </summary>
        public Vector3 GetWorldPosition(int index, int total)
        {
            return new Vector3(GetPositionX(index, total), verticalPosition, 0f);
        }
    }
}
