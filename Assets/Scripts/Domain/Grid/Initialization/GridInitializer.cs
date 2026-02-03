using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Responsável pela inicialização física e lógica do grid de jogo.
/// </summary>
public static class GridInitializer
{
    /// <summary>
    /// Versão do algoritmo de posicionamento. 
    /// Incrementar este valor força a regeneração do padrão de desbloqueio em saves existentes.
    /// </summary>
    public const int ALGORITHM_VERSION = 1;

    /// <summary>
    /// Ponto de entrada para novas runs. Garante que o grid comece em um estado íntegro e válido.
    /// </summary>
    public static void InitializeNewRun(RunData runData, GridConfiguration config, PatternWeightConfig patternConfig = null)
    {
        if (runData == null) throw new ArgumentNullException(nameof(runData));
        if (config == null) throw new ArgumentNullException(nameof(config));

        Debug.Log($"[{nameof(GridInitializer)}] Inicializando novo grid {config.Columns}x{config.Rows}.");

        // Garante que os arrays de dados correspondam à configuração atual
        SyncGridDimensions(runData, config);

        // Inicializa o padrão de desbloqueio determinístico
        int initialSlotCount = GetInitialSlotCount(config);
        InitializeUnlockPattern(runData, config, initialSlotCount, patternConfig);
        
        runData.IsGridInitialized = true;
    }

    /// <summary>
    /// API de "Auto-Cura" (Self-Healing). 
    /// Deve ser chamada durante o Load para garantir que o save é compatível com a configuração atual do projeto.
    /// </summary>
    public static void EnsureGridIntegrity(RunData runData, GridConfiguration config)
    {
        if (runData == null || config == null) return;

        bool needsRepair = false;

        // 1. Validação de Dimensões (Arrays de Runtime)
        if (runData.GridSlots.Length != config.TotalSlots || runData.SlotStates.Length != config.TotalSlots)
        {
            Debug.LogWarning($"[{nameof(GridInitializer)}] Inconsistência de dimensões detectada (Save: {runData.GridSlots.Length} vs Config: {config.TotalSlots}). Corrigindo...");
            SyncGridDimensions(runData, config);
            needsRepair = true;
        }

        // 2. Validação de Contrato de Desbloqueio (Versão e Grid)
        if (runData.GenerationContract == null || !runData.GenerationContract.IsCompatibleWith(config.Columns, config.Rows, ALGORITHM_VERSION))
        {
            Debug.LogWarning($"[{nameof(GridInitializer)}] GenerationContract ausente ou incompatível. Regenerando a partir do Seed...");
            ReapplyUnlockPattern(runData, config);
            needsRepair = true;
        }

        if (needsRepair)
        {
            Debug.Log($"[{nameof(GridInitializer)}] ✅ Integridade do grid restaurada com sucesso.");
        }
    }

    /// <summary>
    /// Regenera o padrões de desbloqueio preservando o Seed original da Run.
    /// Útil para migrações de algoritmo ou ajustes de design.
    /// </summary>
    public static void ReapplyUnlockPattern(RunData runData, GridConfiguration config)
    {
        if (runData == null || config == null) return;

        if (runData.UnlockPatternSeed == 0)
        {
            Debug.LogWarning($"[{nameof(GridInitializer)}] UnlockPatternSeed era zero. Gerando novo seed determinístico.");
            runData.UnlockPatternSeed = GenerateSecureSeed();
        }

        int targetSlotCount = runData.GenerationContract?.TargetSlotCount ?? GetInitialSlotCount(config);

        var random = new SeededRandomProvider(runData.UnlockPatternSeed);
        var patternCoords = UnlockPatternGenerator.Generate(
            random,
            config.Columns,
            config.Rows,
            targetSlotCount,
            null
        );

        // Atualiza o cache de contrato
        runData.GenerationContract = GridUnlockState.Create(
            config.Columns,
            config.Rows,
            targetSlotCount,
            patternCoords
        );

        ApplyPatternToStates(runData, config, patternCoords);
    }

    private static void InitializeUnlockPattern(RunData runData, GridConfiguration config, int slotCount, PatternWeightConfig patternConfig)
    {
        if (runData.UnlockPatternSeed == 0)
        {
            runData.UnlockPatternSeed = GenerateSecureSeed();
        }

        var random = new SeededRandomProvider(runData.UnlockPatternSeed);
        var patternCoords = UnlockPatternGenerator.Generate(
            random,
            config.Columns,
            config.Rows,
            slotCount,
            patternConfig
        );

        runData.GenerationContract = GridUnlockState.Create(
            config.Columns,
            config.Rows,
            slotCount,
            patternCoords
        );

        ApplyPatternToStates(runData, config, patternCoords);
    }

    private static void SyncGridDimensions(RunData runData, GridConfiguration config)
    {
        int targetSize = config.TotalSlots;

        // Redimensiona Slots de Cultivo
        runData.GridSlots = ResizeArray(runData.GridSlots, targetSize, _ => new CropState(default(CropID)));
            
        // Redimensiona Estados de Slots
        runData.SlotStates = ResizeArray(runData.SlotStates, targetSize, _ => new GridSlotState { IsUnlocked = false });
    }   

    private static T[] ResizeArray<T>(T[] original, int targetSize, Func<int, T> factory)
    {
        var newArray = new T[targetSize];
        int sourceLength = original?.Length ?? 0;

        for (int i = 0; i < targetSize; i++)
        {
            if (i < sourceLength)
                newArray[i] = original[i];
            else
                newArray[i] = factory(i);
        }
        return newArray;
    }

    private static void ApplyPatternToStates(RunData runData, GridConfiguration config, List<Vector2Int> pattern)
    {
        // Nota: Não resetamos o array inteiro aqui para permitir que desbloqueios manuais (Cheat/Gameplay) 
        // persistam se o padrão for apenas um subconjunto.
        foreach (var coord in pattern)
        {
            int index = coord.y * config.Columns + coord.x;
            if (index >= 0 && index < runData.SlotStates.Length)
            {
                runData.SlotStates[index].IsUnlocked = true;
            }
        }
    }

    private static int GetInitialSlotCount(GridConfiguration config) => 5;

    private static int GenerateSecureSeed() => UnityEngine.Random.Range(int.MinValue, int.MaxValue);
}
