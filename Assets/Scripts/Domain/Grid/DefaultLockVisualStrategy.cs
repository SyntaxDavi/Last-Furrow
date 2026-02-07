using UnityEngine;

/// <summary>
/// Estratégia padrão de visualização de lock.
/// Usa sprite específico se disponível, senão aplica tint cinza.
/// </summary>
public class DefaultLockVisualStrategy : ILockVisualStrategy
{
    public Sprite GetLockedSprite(GridVisualConfig config)
    {
        return config.lockedSoilSprite ?? config.drySoilSprite;
    }

    public Color GetLockedTint(GridVisualConfig config)
    {
        return config.lockedSoilSprite != null ? Color.white : Color.gray;
    }

    public bool ShouldHidePlant => true;
}