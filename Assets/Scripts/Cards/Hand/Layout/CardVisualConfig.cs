using UnityEngine;

[CreateAssetMenu(fileName = "CardVisualConfig", menuName = "Last Furrow/Card Visual Config")]
public class CardVisualConfig : ScriptableObject
{
    [Header("Suavização (SmoothDamp)")]
    public float PositionSmoothTime = 0.1f;
    public float ScaleSmoothTime = 0.1f;
    public float RotationSpeed = 15f;

    [Header("Z-Depth (Camadas de Profundidade)")]
    public float IdleZ = 0f;
    public float HoverZ = -1.5f;
    public float SelectedZ = -3.0f; // Carta inspecionada fica bem na frente
    public float DragZ = -5.0f;     // Drag ganha de tudo

    [Header("Feeling: Idle")]
    public float IdleFloatSpeed = 2.0f;
    public float IdleFloatAmount = 0.1f;
    public float IdleRotationAmount = 1.0f;

    [Header("Feeling: Mouse Over")]
    public float PeekScale = 1.1f;
    public float PeekYOffset = 0.2f;

    [Header("Interação: Selecionado (Magnético)")]
    public float SelectedYOffset = 1.5f;       // Quanto sobe
    public float SelectedScale = 1.25f;
    [Tooltip("Quanto a carta gira olhando para o mouse")]
    public float LookRotationStrength = 20.0f;
    [Tooltip("Quanto a carta se move levemente na direção do mouse")]
    public float MagneticPullStrength = 0.2f;
    [Tooltip("Raio máximo de influência do mouse")]
    public float MaxInteractionDistance = 4.0f;

    [Header("Interação: Drag")]
    public float DragTiltAmount = 30f;
    public float DragTiltSpeed = 10f;
}