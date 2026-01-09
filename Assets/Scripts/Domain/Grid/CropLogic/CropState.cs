using System;

[Serializable]
public class CropState : IReadOnlyCropState
{
    // --- CAMPOS MÚTAVEIS (Dados para JSON) ---
    public CropID CropID;
    public int CurrentGrowth;
    public int DaysMature;
    public bool IsWithered;
    public bool IsWatered;

    // --- CONSTRUTORES ---
    public CropState()
    {
        CropID = CropID.Empty;
    }

    public CropState(CropID cropID)
    {
        CropID = cropID;
        CurrentGrowth = 0;
        DaysMature = 0;
        IsWithered = false;
        IsWatered = false;
    }

    // --- IMPLEMENTAÇÃO DA INTERFACE (READ-ONLY) ---
    // Estas setas '=>' apontam para os campos acima.
    // Quem ver como 'IReadOnlyCropState' só consegue ler.

    CropID IReadOnlyCropState.CropID => CropID;
    int IReadOnlyCropState.CurrentGrowth => CurrentGrowth;
    int IReadOnlyCropState.DaysMature => DaysMature;
    bool IReadOnlyCropState.IsWatered => IsWatered;
    bool IReadOnlyCropState.IsWithered => IsWithered;

    // Lógica encapsulada de leitura
    public bool IsEmpty => !CropID.IsValid;
}