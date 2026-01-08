using UnityEngine;

[CreateAssetMenu(fileName = "CardVisualConfig", menuName = "Last Furrow/Card Visual Config")]
public class CardVisualConfig : ScriptableObject
{
    [Header("Suavização")]
    public float PositionSmoothTime = 0.1f;
    public float ScaleSmoothTime = 0.1f;
    public float RotationSpeed = 15f;

    [Header("Feeling: Idle)")]
    public float IdleFloatSpeed = 2.0f;     // Quão rápido ela sobe e desce
    public float IdleFloatAmount = 0.1f;    // Distância que ela percorre
    public float IdleRotationAmount = 1.0f; // Leve rotação enquanto flutua

    [Header("Feeling: Mouse Over (Sutil)")]
    public float PeekScale = 1.1f;          // Aumenta um pouquinho só de passar o mouse
    public float PeekYOffset = 0.2f;        // Sobe um tiquinho

    [Header("Interação: Selecionado (Clique)")]
    public float SelectedYOffset = 1.5f;    // Sobe BASTANTE quando clica
    public float SelectedScale = 1.25f;

    [Header("Interação: Drag")]
    public float DragZDepth = -5f;
    public float DragTiltAmount = 30f;
    public float DragTiltSpeed = 10f;
}