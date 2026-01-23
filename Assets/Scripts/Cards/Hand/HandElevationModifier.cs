using UnityEngine;

/// <summary>
/// Modificador visual que aplica elevação à carta quando a mão está destacada.
/// Implementa ICardVisualModifier para integração com o pipeline de CardView.
/// Segue SRP: apenas gerencia a elevação individual da carta.
/// </summary>
public class HandElevationModifier : ICardVisualModifier
{
    private bool _isRaised;
    private float _currentOffset;
    private float _targetOffset;
    
    /// <summary>
    /// Define se a carta deve estar elevada ou não.
    /// A transição é suave, controlada pelo Apply().
    /// </summary>
    public void SetRaised(bool raised)
    {
        _isRaised = raised;
    }
    
    /// <summary>
    /// Retorna o estado atual de elevação.
    /// </summary>
    public bool IsRaised => _isRaised;
    
    /// <summary>
    /// Aplica o offset de elevação ao target visual.
    /// Chamado automaticamente pelo pipeline de CardView.
    /// </summary>
    public void Apply(ref CardVisualTarget target, CardVisualConfig config, float time)
    {
        // Calcula o offset alvo baseado no estado
        _targetOffset = _isRaised ? config.HandElevationOffset : 0f;
        
        // Interpola suavemente para o valor alvo
        _currentOffset = Mathf.Lerp(
            _currentOffset, 
            _targetOffset, 
            Time.deltaTime * config.HandElevationSpeed
        );
        
        // Aplica o offset ao target
        target.Position.y += _currentOffset;
    }
    
    /// <summary>
    /// Força a elevação instantânea (útil para reset ou inicialização).
    /// </summary>
    public void ForceImmediate(bool raised, float offset)
    {
        _isRaised = raised;
        _currentOffset = raised ? offset : 0f;
        _targetOffset = _currentOffset;
    }
}
