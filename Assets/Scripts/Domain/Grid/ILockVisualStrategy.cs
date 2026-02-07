using UnityEngine;

/// <summary>
/// Estratégia para customizar a aparência visual de slots bloqueados.
/// Permite trocar estilos de lock sem modificar GridSlotView.
/// </summary>
public interface ILockVisualStrategy
{
    Sprite GetLockedSprite(GridVisualConfig config);
    Color GetLockedTint(GridVisualConfig config);
    bool ShouldHidePlant { get; }
}