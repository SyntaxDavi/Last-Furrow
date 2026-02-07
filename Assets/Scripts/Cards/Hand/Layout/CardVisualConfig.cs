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
    
    [Tooltip("Delay global ANTES de iniciar a elevacao (subida) da mao.")]
    public float HandElevationStartDelay = 0f;
    
    [Tooltip("Delay global ANTES de iniciar a descida da mao.")]
    public float HandLoweringStartDelay = 0f;

    [Header("9. Fan Out/In (Analysis Phase)")]
    [Tooltip("Offset para onde as cartas vão durante análise (relativo ao HandCenter)")]
    public Vector3 FanOutOffset = new Vector3(-15f, -10f, 0f);
    
    [Tooltip("Delay antes de começar o Fan-In (respiro visual)")]
    public float FanInPreDelay = 0.5f;
    
    [Tooltip("Delay entre cada carta no Fan-Out sequencial")]
    public float FanOutSequenceDelay = 0.12f;
    
    [Tooltip("Delay entre cada carta no Fan-In sequencial")]
    public float FanInSequenceDelay = 0.08f;
    
    [Tooltip("Distância mínima para considerar que a carta 'chegou' no target")]
    public float ConvergenceThreshold = 0.15f;  // AUMENTADO de 0.1f para 0.15f

    [Tooltip("Tempo máximo de espera por convergência (segundos)")]
    public float MaxConvergenceTime = 1.5f;  // NOVO

    [Tooltip("Tempo mínimo antes de checar convergência (segundos)")]
    public float MinConvergenceTime = 0.3f;  // NOVO

    [Header("10. Audio")]
    public SoundEffect[] CardDrawSounds;
    public SoundEffect[] CardSelectSounds;
    public SoundEffect[] CardHoverSounds;
    public SoundEffect[] HandElevatedSounds;
    public SoundEffect[] HandLoweredSounds;
    public SoundEffect[] CardReorderSounds;
    public SoundEffect[] OneCardReorderSounds;
    public SoundEffect[] CardDragSounds;
    public SoundEffect[] CardOnGridSounds;

    [Header("11. Drag Ghost (Transparency)")]
    [Tooltip("Alpha da carta quando está sobre um drop target válido durante drag.")]
    [Range(0.1f, 0.9f)]
    public float DragGhostAlpha = 0.5f;
    
    [Tooltip("Velocidade da transição de transparência (maior = mais rápido).")]
    public float DragGhostTransitionSpeed = 10f;

    [Header("12. Use Animation (Slam Cake)")]
    public float UseAnticipationY = 0.5f;
    public float UseAnticipationDuration = 0.2f;
    public float UseSlamDuration = 0.1f;
    public float UsePunchAmount = 0.2f;
}

[System.Serializable]
public class SoundEffect
{
    public AudioClip Clip;
    [Range(0, 1)] public float Volume = 1f;
}
