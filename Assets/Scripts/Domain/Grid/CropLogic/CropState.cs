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
    public CropState(IReadOnlyCropState slot) => Clear(); // Construtor padrão já nasce limpo

    public CropState(CropID cropID)
    {
        Clear(); // Garante defaults
        CropID = cropID;
    }

    // --- CONSTRUTOR DE CÓPIA (para snapshots) ---
    public CropState(CropState other)
    {
        if (other == null)
        {
            Clear();
            return;
        }

        CropID = other.CropID;
        CurrentGrowth = other.CurrentGrowth;
        DaysMature = other.DaysMature;
        IsWithered = other.IsWithered;
        IsWatered = other.IsWatered;
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

    /// <summary>
    /// Planta um crop neste slot (reseta estado e define crop).
    /// </summary>
    public void Plant(CropID cropID)
    {
        if (!cropID.IsValid)
        {
            Clear();
            return;
        }

        CropID = cropID;
        CurrentGrowth = 0;
        DaysMature = 0;
        IsWithered = false;
        IsWatered = false;
    }

    // --- IMPLEMENTAÇÃO DA INTERFACE ---
    CropID IReadOnlyCropState.CropID => CropID;
    int IReadOnlyCropState.CurrentGrowth => CurrentGrowth;
    int IReadOnlyCropState.DaysMature => DaysMature;
    bool IReadOnlyCropState.IsWatered => IsWatered;
    bool IReadOnlyCropState.IsWithered => IsWithered;

    public bool IsEmpty => !CropID.IsValid;
}