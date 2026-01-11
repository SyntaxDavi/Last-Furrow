using UnityEngine;

// Centraliza a lógica de camadas para evitar "Magic Numbers"
public static class CardSortingConstants
{
    public const int BASE_GAP = 10;       // Distância entre cartas normais
    public const int HOVER_LAYER = 500;  // BEM ALTO: Garante que ganha de qualquer vizinha
    public const int DRAG_LAYER = 600;  // MÁXIMO: Ninguém cobre quem está sendo arrastado
}
[CreateAssetMenu(fileName = "HandLayoutConfig", menuName = "Last Furrow/Hand Layout Config")]
public class HandLayoutConfig : ScriptableObject
{
    [Header("Matemática do Leque")]
    public float CardSpacing = 2.0f;
    public float MaxHandWidth = 12.0f;
    public float ArcHeight = 0.5f;
    public float RotationIntensity = 15.0f;

    [Header("Animação de Entrada (Fan Out)")]
    [Tooltip("Tempo de espera entre uma carta e outra")]
    public float SpawnDelay = 0.08f;

    [Tooltip("De onde a carta sai? (Relativo ao HandCenter)")]
    // Ex: X=-15, Y=-10 faz sair do canto inferior esquerdo (Deck imaginário)
    public Vector2 SpawnOffset = new Vector2(-15f, -10f);
}