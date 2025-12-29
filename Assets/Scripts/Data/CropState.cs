[System.Serializable]
public class CropState
{
    public string CropID;     
    public int CurrentGrowth; 
    public bool IsWithered;   
    public bool IsWatered;     

    // Construtor para facilitar
    public CropState(string cropID)
    {
        CropID = cropID;
        CurrentGrowth = 0;
        IsWithered = false;
    }

    // Métodos de Lógica Pura (Testáveis!)
    public void Grow()
    {
        CurrentGrowth++;
    }
}