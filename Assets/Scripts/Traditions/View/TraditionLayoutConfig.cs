using UnityEngine;

namespace LastFurrow.Traditions
{
    public enum TraditionAlignment
    {
        Left,
        Center,
        Right
    }

    /// <summary>
    /// Configuração de layout para as tradições.
    /// Define espaçamento, escala e comportamento visual.
    /// </summary>
    [CreateAssetMenu(fileName = "TraditionLayoutConfig", menuName = "Last Furrow/Traditions/Layout Config")]
    public class TraditionLayoutConfig : ScriptableObject
    {
        [Header("Layout")]
        [Tooltip("Como as tradições se alinham em relação ao container")]
        public TraditionAlignment alignment = TraditionAlignment.Center;

        [Tooltip("Espaço entre cada tradição")]
        public float spacing = 1.2f;
        
        [Tooltip("Escala das tradições")]
        public float scale = 0.7f;

        [Header("Z-Index")]
        [Tooltip("Z-Offset para evitar Z-fighting")]
        public float zOffset = -0.1f;
        
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
        
        [Header("Idle Physics (Levitação)")]
        [Tooltip("Velocidade de flutuação idle")]
        public float idleFloatSpeed = 2.0f;
        
        [Tooltip("Amplitude da flutuação idle")]
        public float idleFloatAmount = 0.08f;
        
        [Tooltip("Rotação sutil durante idle")]
        public float idleRotationAmount = 0.5f;
        
        [Header("Hover (Mouse Over)")]
        [Tooltip("Offset Y no hover (negativo = desce, positivo = sobe)")]
        public float hoverElevation = -0.25f;
        
        [Tooltip("Escala da tradição no hover")]
        public float hoverScale = 1.15f;
        
        [Tooltip("Suavização do movimento (menor = mais suave)")]
        public float hoverSmoothTime = 0.12f;
        
        [Tooltip("Intensidade do glow ao passar o mouse")]
        public float hoverGlowIntensity = 0.6f;
        
        /// <summary>
        /// Calcula a posição local X de uma tradição baseado no índice e total.
        /// </summary>
        public float GetLocalPositionX(int index, int total)
        {
            if (total <= 0) return 0f;
            
            float totalWidth = (total - 1) * spacing;
            float startX = 0f;

            switch (alignment)
            {
                case TraditionAlignment.Left:
                    startX = 0f;
                    break;
                case TraditionAlignment.Center:
                    startX = -(totalWidth / 2f);
                    break;
                case TraditionAlignment.Right:
                    startX = -totalWidth;
                    break;
            }
            
            return startX + (index * spacing);
        }
        
        /// <summary>
        /// Retorna a posição local completa para uma tradição.
        /// </summary>
        public Vector3 GetLocalPosition(int index, int total)
        {
            return new Vector3(GetLocalPositionX(index, total), 0f, zOffset * index);
        }
    }
}
