using UnityEngine;
public enum CardVisualState
{
    Idle,
    Selected, // Magnético
    Dragging,
    Consuming,
    // Futuro: Locked, Preview, Disabled...
}
public struct CardVisualTarget
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;

    // Factory method para facilitar criação padrão
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
    public float PositionSmoothTime;
    public float RotationSpeed;
    public float ScaleSmoothTime;

    // Perfis predefinidos podem viver aqui ou no Config
    public static CardMovementProfile Instant => new CardMovementProfile { PositionSmoothTime = 0, RotationSpeed = 9999, ScaleSmoothTime = 0 };
}
public interface ICardVisualModifier
{
    // Recebe o target atual e pode alterá-lo (Position, Rotation, Scale)
    void Apply(ref CardVisualTarget target, CardVisualConfig config, float time);
}