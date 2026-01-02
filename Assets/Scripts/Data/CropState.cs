[System.Serializable]
public class CropState
{
    public CropID CropID;

    public int CurrentGrowth;
    public int DaysMature;
    public bool IsWithered;
    public bool IsWatered;


    // --- ADICIONE ESTE CONSTRUTOR VAZIO ---
    // Necessário para criar slots vazios e para serialização do JSON/Save
    public CropState()
    {
        CropID = CropID.Empty;
    }

    // Construtor específico (Mantém este também)
    public CropState(CropID cropID)
    {
        CropID = cropID;
        CurrentGrowth = 0;
        DaysMature = 0;
        IsWithered = false;
        IsWatered = false;
    }
    public bool IsEmpty => !CropID.IsValid;
}