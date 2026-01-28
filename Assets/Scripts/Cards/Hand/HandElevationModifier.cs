using UnityEngine;
using LastFurrow.Infrastructure.Visual;

/// <summary>
/// Modificador visual que aplica elevação à carta quando a mão está destacada.
/// Implementa ICardVisualModifier para integração com o pipeline de CardView.
/// Segue SRP: apenas gerencia a elevação individual da carta.
/// </summary>
public class HandElevationModifier : ICardVisualModifier
{
    private VisualElevationProcessor _elevationProcessor = new();
    
    /// <summary>
    /// Define o fator de elevação (0.0 a 1.0) para transições graduais ou sequenciais.
    /// </summary>
    public void SetElevationFactor(float factor)
    {
        _elevationProcessor.SetElevationFactor(factor);
    }

    /// <summary>
    /// Retorna se a carta está atualmente com algum nível de elevação.
    /// </summary>
    public bool IsRaised => _elevationProcessor.IsRaised;

    /// <summary>
    /// Aplica o offset de elevação ao target visual.
    /// Chamado automaticamente pelo pipeline de CardView.
    /// </summary>
    public void Apply(ref CardVisualTarget target, CardVisualConfig config, float time)
    {
        _elevationProcessor.Update(
            config.HandElevationOffset,
            config.HandElevationSpeed,
            Time.deltaTime
        );

        target.Position.y += _elevationProcessor.CurrentOffset;
    }

    /// <summary>
    /// Força a elevação instantânea (útil para reset ou inicialização).
    /// </summary>
    public void ForceImmediate(bool raised, float offset)
    {
        // Forçar offset imediato no processador exigiria um método novo ou reset + update
        // Para simplicidade técnica neste refactor:
        _elevationProcessor.SetElevationFactor(raised ? 1f : 0f);
    }
}
