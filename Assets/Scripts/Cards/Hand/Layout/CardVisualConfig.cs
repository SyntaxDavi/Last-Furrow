using UnityEngine;

[CreateAssetMenu(fileName = "CardVisualConfig", menuName = "Last Furrow/Card Visual Config")]
public class CardVisualConfig : ScriptableObject
{
    [Header("1. Fisica & Suavizacao")]
    public float PositionSmoothTime = 0.1f;
    public float ScaleSmoothTime = 0.1f;
    public float RotationSpeed = 15f;

    [Header("2. Profundidade (Z-Depth)")]
    public float IdleZ = 0f;
    public float HoverZ = -1.5f;
    public float DragZ = -5.0f;

    [Header("3. Comportamento Idle")]
    public float IdleFloatSpeed = 2.0f;
    public float IdleFloatAmount = 0.1f;
    public float IdleRotationAmount = 1.0f;

    [Header("4. Comportamento Hover (Mouse Over)")]
    public float PeekScale = 1.1f;
    public float PeekYOffset = 0.2f;

    [Header("5. Comportamento Tilt 3D")]
    public float TiltAngleMax = 20f;
    public float TiltInfluenceRadius = 1.5f;
    public float TiltRotationSpeed = 20f;

    [Header("6. Comportamento Drag")]
    public float DragTiltAmount = 30f;

    [Header("7. Feedback de Clique (Juice)")]
    public float ClickSquashAmount = 0.15f;
    public float ClickRecoverySpeed = 15f;

    [Header("8. Elevacao da Mao (Hover Sequencial)")]
    [Tooltip("Quanto a carta sobe quando a mao esta elevada.")]
    public float HandElevationOffset = 0.5f;
    
    [Tooltip("Velocidade da animacao de elevacao (maior = mais rapido).")]
    public float HandElevationSpeed = 8f;
    
    [Tooltip("Delay entre cada carta na sequencia (segundos).")]
    public float HandElevationSequenceDelay = 0.05f;
}
