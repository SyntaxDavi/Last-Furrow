using UnityEngine;

public static class HandLayoutCalculator
{
    public struct CardTransformTarget
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public int SortingOrder;
    }

    public static CardTransformTarget CalculateSlot(
        int index,
        int totalCards,
        HandLayoutConfig config,
        Vector3 centerPos)
    {
        if (totalCards == 0) return new CardTransformTarget();

        // 1. Largura e Espaçamento
        float desiredWidth = (totalCards - 1) * config.CardSpacing;
        float actualWidth = Mathf.Min(desiredWidth, config.MaxHandWidth);
        float currentSpacing = (totalCards > 1) ? actualWidth / (totalCards - 1) : 0;

        // 2. Posição Base (X)
        float startX = -actualWidth / 2f;
        float xOffset = startX + (index * currentSpacing);

        // 3. Arco e Rotação
        float normalizedPos = (totalCards > 1) ? (float)index / (totalCards - 1) : 0.5f;
        float arcX = (normalizedPos - 0.5f) * 2f; // -1 a 1

        float yOffset = -Mathf.Abs(arcX * arcX) * config.ArcHeight;
        float zRotation = -arcX * config.RotationIntensity;

        return new CardTransformTarget
        {
            Position = centerPos + new Vector3(xOffset, yOffset, 0),
            Rotation = Quaternion.Euler(0, 0, zRotation),
            SortingOrder = index * CardSortingConstants.BASE_GAP
        };
    }
}