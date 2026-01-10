using System;

[Serializable]
public class CropState : IReadOnlyCropState
{
    // --- CAMPOS ---
    public CropID CropID;
    public int CurrentGrowth;
    public int DaysMature;
    public bool IsWithered;
    public bool IsWatered;

    // --- CONSTRUTORES ---
    public CropState() => Clear(); // Construtor padrão já nasce limpo

    public CropState(CropID cropID)
    {
        Clear(); // Garante defaults
        CropID = cropID;
    }

    // --- LÓGICA DE LIMPEZA (SÊNIOR) ---
    // Único ponto de verdade sobre como resetar um slot.
    public void Clear()
    {
        CropID = CropID.Empty;
        CurrentGrowth = 0;
        DaysMature = 0;
        IsWithered = false;
        IsWatered = false;
        // Se amanhã você adicionar "public int PragaAmount", 
        // você só precisa adicionar "PragaAmount = 0" AQUI.
        // Todas as estratégias (Colheita, Pá, Explosão) serão atualizadas automaticamente.
    }

    // --- IMPLEMENTAÇÃO DA INTERFACE ---
    CropID IReadOnlyCropState.CropID => CropID;
    int IReadOnlyCropState.CurrentGrowth => CurrentGrowth;
    int IReadOnlyCropState.DaysMature => DaysMature;
    bool IReadOnlyCropState.IsWatered => IsWatered;
    bool IReadOnlyCropState.IsWithered => IsWithered;

    public bool IsEmpty => !CropID.IsValid;
}