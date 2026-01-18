using System;

[Serializable]
public class GridSlotState
{
    public bool IsUnlocked;

    // Construtor padrão para serialização
    public GridSlotState() 
    {
        IsUnlocked = false;
    }

    public GridSlotState(bool isUnlocked)
    {
        IsUnlocked = isUnlocked;
    }
}
