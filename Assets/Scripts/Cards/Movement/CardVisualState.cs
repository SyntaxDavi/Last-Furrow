using UnityEngine;

public enum CardVisualState
{
    Idle,
    Selected,
    Dragging,
    Consuming,
}

public struct CardVisualTarget
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;

    public static CardVisualTarget Create(Vector3 pos, Quaternion rot, float scale)
    {
        return new CardVisualTarget
        {
            Position = pos,
            Rotation = rot,
            Scale = Vector3.one * scale
        };
    }
}

[System.Serializable]
public struct CardMovementProfile
{
    [Header("Tempos de Suavização (Menor = Mais Rápido)")]
    public float PositionSmoothTime;
    public float ScaleSmoothTime;

    // MUDANÇA: Usamos tempo em vez de velocidade para rotação
    public float RotationSmoothTime;

    [Header("Juice")]
    // MUDANÇA: Campo novo para o efeito de esticar
    public float MovementStretchAmount;

    // Perfil helper para teletransporte lógico
    public static CardMovementProfile Instant => new CardMovementProfile
    {
        PositionSmoothTime = 0,
        RotationSmoothTime = 0,
        ScaleSmoothTime = 0,
        MovementStretchAmount = 0
    };
}

public interface ICardVisualModifier
{
    void Apply(ref CardVisualTarget target, CardVisualConfig config, float time);
}