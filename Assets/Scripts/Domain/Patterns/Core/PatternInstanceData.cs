using System;
using System.Collections.Generic;

/// <summary>
/// DTO serializável que representa uma instância de padrão ativo para tracking de decay.
/// 
/// FUNÇÃO: Persistir estado de padrões entre dias para calcular decay.
/// 
/// IDENTIDADE DE PADRÃO (CRÍTICO):
/// Um padrão é considerado o "mesmo padrão" para efeitos de decay SOMENTE se:
/// 1. PatternID (tipo) for idêntico
/// 2. Slots exatos (índices) forem os mesmos
/// 3. CropIDs de todas as crops envolvidas forem os mesmos
/// 
/// PatternInstanceID = Hash(PatternID + SlotIndices + CropIDs)
/// 
/// IMPLICAÇÕES:
/// - Mudou 1 slot ? NOVO PADRÃO (reseta decay)
/// - Trocou crop ? NOVO PADRÃO (reseta decay)
/// - Apenas cresceu (young ? mature) ? MESMO PADRÃO (decay continua)
/// </summary>
[Serializable]
public class PatternInstanceData
{
    /// <summary>
    /// Hash único que identifica esta instância específica do padrão.
    /// Gerado a partir de: PatternID + SlotIndices (sorted) + CropIDs (sorted)
    /// </summary>
    public string InstanceID;
    
    /// <summary>
    /// ID do tipo de padrão (ex: "FULL_LINE", "FRAME").
    /// </summary>
    public string PatternID;
    
    /// <summary>
    /// Número de dias consecutivos que este padrão está ativo.
    /// Usado para calcular decay: -10% por dia após o primeiro.
    /// </summary>
    public int DaysActive;
    
    /// <summary>
    /// Dia da semana em que o padrão foi criado (1-7).
    /// Usado para bonus pós-reset semanal.
    /// </summary>
    public int CreatedOnDay;
    
    /// <summary>
    /// Semana em que o padrão foi criado.
    /// Se CurrentWeek > CreatedOnWeek, pode ter reset semanal.
    /// </summary>
    public int CreatedOnWeek;
    
    /// <summary>
    /// Flag que indica se este padrão foi recriado após ser quebrado.
    /// Se true, ganha +10% bonus no primeiro dia.
    /// </summary>
    public bool IsRecreated;
    
    /// <summary>
    /// Índices dos slots que formam o padrão (para comparação de identidade).
    /// </summary>
    public List<int> SlotIndices;
    
    /// <summary>
    /// Strings dos CropIDs nos slots (para comparação de identidade).
    /// Usamos string ao invés de CropID para serialização JSON.
    /// </summary>
    public List<string> CropIDStrings;
    
    // Construtor padrão para serialização
    public PatternInstanceData()
    {
        SlotIndices = new List<int>();
        CropIDStrings = new List<string>();
    }
    
    /// <summary>
    /// Cria uma nova instância de tracking a partir de um PatternMatch.
    /// </summary>
    public static PatternInstanceData CreateFromMatch(PatternMatch match, int currentDay, int currentWeek, bool isRecreated = false)
    {
        var data = new PatternInstanceData
        {
            PatternID = match.PatternID,
            DaysActive = 1,
            CreatedOnDay = currentDay,
            CreatedOnWeek = currentWeek,
            IsRecreated = isRecreated,
            SlotIndices = new List<int>(match.SlotIndices),
            CropIDStrings = new List<string>()
        };
        
        // Converter CropIDs para strings
        if (match.CropIDs != null)
        {
            foreach (var cropID in match.CropIDs)
            {
                data.CropIDStrings.Add(cropID.ToString());
            }
        }
        
        // Gerar InstanceID
        data.InstanceID = GenerateInstanceID(match.PatternID, data.SlotIndices, data.CropIDStrings);
        
        return data;
    }
    
    /// <summary>
    /// Gera um ID único e determinístico para esta instância de padrão.
    /// 
    /// FÓRMULA: Hash(PatternID + SortedSlots + SortedCropIDs)
    /// 
    /// Isso garante que:
    /// - Mesmo padrão nos mesmos slots com mesmas crops = mesmo InstanceID
    /// - Qualquer mudança = InstanceID diferente
    /// </summary>
    public static string GenerateInstanceID(string patternID, List<int> slots, List<string> cropIDs)
    {
        // Ordenar para garantir determinismo
        var sortedSlots = new List<int>(slots);
        sortedSlots.Sort();
        
        var sortedCrops = new List<string>(cropIDs);
        sortedCrops.Sort();
        
        // Construir string para hash
        string slotsStr = string.Join(",", sortedSlots);
        string cropsStr = string.Join(",", sortedCrops);
        string combined = $"{patternID}|{slotsStr}|{cropsStr}";
        
        // Usar hash simples (GetHashCode é determinístico para strings)
        // Para produção, considerar usar SHA256 ou similar
        return $"{patternID}_{combined.GetHashCode():X8}";
    }
    
    /// <summary>
    /// Verifica se este PatternInstanceData corresponde a um PatternMatch.
    /// </summary>
    public bool MatchesPattern(PatternMatch match)
    {
        string matchInstanceID = GenerateInstanceID(
            match.PatternID, 
            match.SlotIndices, 
            ConvertCropIDsToStrings(match.CropIDs)
        );
        
        return InstanceID == matchInstanceID;
    }
    
    /// <summary>
    /// Helper para converter lista de CropID para lista de strings.
    /// </summary>
    private static List<string> ConvertCropIDsToStrings(List<CropID> cropIDs)
    {
        var strings = new List<string>();
        if (cropIDs != null)
        {
            foreach (var cropID in cropIDs)
            {
                strings.Add(cropID.ToString());
            }
        }
        return strings;
    }
    
    /// <summary>
    /// Incrementa o contador de dias ativos.
    /// Chamado no fim de cada dia se o padrão ainda existe.
    /// </summary>
    public void IncrementDaysActive()
    {
        DaysActive++;
    }
    
    /// <summary>
    /// Calcula o multiplicador de decay baseado nos dias ativos.
    /// 
    /// FÓRMULA: 0.9^(DaysActive - 1)
    /// - Dia 1: 1.0x (100%)
    /// - Dia 2: 0.9x (90%)
    /// - Dia 3: 0.81x (81%)
    /// - Dia 4: 0.729x (72.9%)
    /// </summary>
    public float GetDecayMultiplier()
    {
        if (DaysActive <= 1)
            return 1f;
        
        return UnityEngine.Mathf.Pow(0.9f, DaysActive - 1);
    }
    
    /// <summary>
    /// Calcula o multiplicador de bonus pós-reset.
    /// Se o padrão foi recriado após ser quebrado, ganha +10% no primeiro dia.
    /// </summary>
    public float GetRecreationBonus()
    {
        if (IsRecreated && DaysActive == 1)
            return 1.1f;
        
        return 1f;
    }
    
    public override string ToString()
    {
        return $"[{PatternID}] Days: {DaysActive}, Decay: {GetDecayMultiplier():F2}x, Recreated: {IsRecreated}";
    }
}
