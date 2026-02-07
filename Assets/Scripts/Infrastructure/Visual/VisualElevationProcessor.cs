using UnityEngine;

namespace LastFurrow.Infrastructure.Visual
{
    /// <summary>
    /// Processador genérico para cálculos de elevação visual (Y-Offset).
    /// Centraliza a lógica de suavização (Lerp) para evitar duplicação em Cartas e Grids.
    /// </summary>
    public class VisualElevationProcessor
    {
        private float _currentOffset;
        private float _targetOffset;
        private float _elevationFactor; // 0.0 a 1.0

        private const float SNAP_THRESHOLD = 0.001f;

        public float CurrentOffset => _currentOffset;
        public float TargetOffset => _targetOffset;
        public bool IsRaised => _currentOffset > SNAP_THRESHOLD;

        /// <summary>
        /// Indica se a elevação está estável E em repouso (não precisa de   Update).
        /// True apenas quando offset atual E target são ambos ~zero.
        /// </summary>
        public bool IsStable => _elevationFactor < SNAP_THRESHOLD 
                                && Mathf.Abs(_currentOffset) < SNAP_THRESHOLD 
                                && Mathf.Abs(_targetOffset) < SNAP_THRESHOLD;

        /// <summary>
        /// Indica se a animação de elevação está em andamento.
        /// </summary>
        public bool IsAnimating => Mathf.Abs(_currentOffset - _targetOffset) > SNAP_THRESHOLD;

        /// <summary>
        /// Atualiza o fator de elevação (0.0 a 1.0).
        /// </summary>
        public void SetElevationFactor(float factor)
        {
            _elevationFactor = Mathf.Clamp01(factor);
        }

        /// <summary>
        /// Processa a interpolação suave do offset.
        /// </summary>
        public void Update(float maxOffset, float speed, float deltaTime)
        {
            _targetOffset = maxOffset * _elevationFactor;

            if (Mathf.Abs(_currentOffset - _targetOffset) < SNAP_THRESHOLD)
            {
                _currentOffset = _targetOffset;
            }
            else
            {
                _currentOffset = Mathf.Lerp(_currentOffset, _targetOffset, deltaTime * speed);
            }
        }

        /// <summary>
        /// Aplica o offset calculado a um Vector3.
        /// </summary>
        public Vector3 Apply(Vector3 basePosition)
        {
            return new Vector3(basePosition.x, basePosition.y + _currentOffset, basePosition.z);
        }

        /// <summary>
        /// Reseta instantaneamente a elevação.
        /// </summary>
        public void Reset()
        {
            _currentOffset = 0f;
            _targetOffset = 0f;
            _elevationFactor = 0f;
        }
    }
}
