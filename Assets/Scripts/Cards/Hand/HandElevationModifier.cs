using UnityEngine;

/// <summary>
/// Modificador visual que aplica elevação à carta quando a mão está destacada.
/// Implementa ICardVisualModifier para integração com o pipeline de CardView.
/// Segue SRP: apenas gerencia a elevação individual da carta.
/// </summary>
public class HandElevationModifier : ICardVisualModifier
{
    private float _elevationFactor = 0f; // 0.0 = abaixado, 1.0 = totalmente elevado
    private float _currentOffset;
    private float _targetOffset;
    
    // Pequeno threshold para snap (evita cálculo eterno de Lerp)
    private const float SNAP_THRESHOLD = 0.001f;

    /// <summary>
    /// Define o fator de elevação (0.0 a 1.0) para transições graduais ou sequenciais.
    /// </summary>
    public void SetElevationFactor(float factor)
    {
        _elevationFactor = Mathf.Clamp01(factor);
    }

    /// <summary>
    /// Retorna se a carta está atualmente com algum nível de elevação.
    /// </summary>
    public bool IsRaised => _elevationFactor > 0.01f;

    /// <summary>
    /// Aplica o offset de elevação ao target visual.
    /// Chamado automaticamente pelo pipeline de CardView.
    /// </summary>
    public void Apply(ref CardVisualTarget target, CardVisualConfig config, float time)
    {
        // Calcula o offset alvo baseado no FATOR de elevação (permite transição gradual)
        _targetOffset = config.HandElevationOffset * _elevationFactor;

        // Otimização: Se já estivermos no alvo (ou muito perto), snapa e retorna
        if (Mathf.Abs(_currentOffset - _targetOffset) < SNAP_THRESHOLD)
        {
            _currentOffset = _targetOffset;
        }
        else
        {
            // Interpola suavemente para o valor alvo
            _currentOffset = Mathf.Lerp(
                _currentOffset,
                _targetOffset,
                Time.deltaTime * config.HandElevationSpeed
            );
        }

        // Aplica o offset ao target
        if (_currentOffset > 0)
        {
            // Se a carta estiver em hover, podemos reduzir o impacto da elevação da mão
            // para evitar que ela suba "demais". 
            // Nota: CardView já aplica o PeekYOffset separadamente.
            target.Position.y += _currentOffset;
        }
    }

    /// <summary>
    /// Força a elevação instantânea (útil para reset ou inicialização).
    /// </summary>
    public void ForceImmediate(bool raised, float offset)
    {
        _elevationFactor = raised ? 1f : 0f;
        _currentOffset = offset;
        _targetOffset = offset;
    }
}
