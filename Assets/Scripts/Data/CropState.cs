[System.Serializable]
public class CropState
{
    public string CropID;
    public int CurrentGrowth;
    public bool IsWithered;
    public bool IsWatered;

    // --- ADICIONE ESTE CONSTRUTOR VAZIO ---
    // Necessário para criar slots vazios e para serialização do JSON/Save
    public CropState()
    {
        CropID = ""; // ou null
        CurrentGrowth = 0;
        IsWithered = false;
        IsWatered = false;
    }

    // Construtor específico (Mantém este também)
    public CropState(string cropID)
    {
        CropID = cropID;
        CurrentGrowth = 0;
        IsWithered = false;
        IsWatered = false;
    }

    public void Grow()
    {
        CurrentGrowth++;
    }
}