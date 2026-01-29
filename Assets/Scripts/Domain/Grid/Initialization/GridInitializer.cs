using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Responsável pela inicialização física e lógica do grid de jogo.
/// </summary>
public static class GridInitializer
{
    /// <summary>
    /// Inicializa o grid para um novo RunData.
    /// </summary>
    public static void InitializeNewRun(RunData runData, GridConfiguration config, PatternWeightConfig patternConfig = null)
    {
        if (runData == null || config == null) return;

        int totalSlots = config.Columns * config.Rows;

        // 1. Garante que as listas internas existem
        EnsureGridSlots(runData, totalSlots);
        EnsureSlotStates(runData, totalSlots);

        // 2. Determina quantos slots devem ser desbloqueados
        int slotCount = GetInitialSlotCount(config);

        // 3. Seleciona e Aplica Padrão
        InitializeUnlockPattern(runData, config, slotCount, patternConfig);
        
        Debug.Log($"[GridInitializer] Grid inicializado: {totalSlots} slots, {slotCount} desbloqueados.");
    }

    /// <summary>
    /// Regenera o padrões de desbloqueio (usado para compatibilidade ou debug).
    /// </summary>
    public static void ReapplyUnlockPattern(RunData runData, GridConfiguration config)
    {
        if (runData == null || config == null) return;

        if (runData.UnlockPatternSeed == 0)
        {
            Debug.LogError("[GridInitializer] Seed é 0! Não é possível regenerar deterministicamente. Criando novo seed.");
            runData.UnlockPatternSeed = GenerateSeed();
        }

        int slotCount = runData.UnlockState?.TargetSlotCount ?? 5;

        // USA O NOVO PROVIDER E SIGNATURA
        var random = new SeededRandomProvider(runData.UnlockPatternSeed);
        var pattern = UnlockPatternGenerator.Generate(
            random,
            config.Columns,
            config.Rows,
            slotCount,
            null
        );

        runData.UnlockState = GridUnlockState.Create(
            config.Columns,
            config.Rows,
            slotCount,
            pattern
        );

        ApplyPattern(runData, config, pattern);
    }

    private static void InitializeUnlockPattern(RunData runData, GridConfiguration config, int slotCount, PatternWeightConfig patternConfig)
    {
        // 1. Gera seed se ainda não existe (Normalmente gerado no RunData.CreateNewRun)
        if (runData.UnlockPatternSeed == 0)
        {
            runData.UnlockPatternSeed = GenerateSeed();
        }

        // 2. Gera padrão usando seed e o NOVO PROVIDER
        var random = new SeededRandomProvider(runData.UnlockPatternSeed);
        var pattern = UnlockPatternGenerator.Generate(
            random,
            config.Columns,
            config.Rows,
            slotCount,
            patternConfig
        );

        // 3. Salva estado para validação futura
        runData.UnlockState = GridUnlockState.Create(
            config.Columns,
            config.Rows,
            slotCount,
            pattern
        );

        ApplyPattern(runData, config, pattern);
    }

    private static int GetInitialSlotCount(GridConfiguration config)
    {
        // Exemplo: 20% do grid ou valor fixo
        return 5; 
    }

    private static void EnsureGridSlots(RunData runData, int targetSize)
    {
        if (runData.GridSlots == null || runData.GridSlots.Length != targetSize)
        {
            var oldSlots = runData.GridSlots ?? new CropState[0];
            runData.GridSlots = new CropState[targetSize];

            for (int i = 0; i < runData.GridSlots.Length; i++)
            {
                if (i < oldSlots.Length)
                    runData.GridSlots[i] = oldSlots[i];
                else
                    runData.GridSlots[i] = new CropState(default(CropID));
            }
        }
    }

    private static void EnsureSlotStates(RunData runData, int targetSize)
    {
        if (runData.SlotStates == null || runData.SlotStates.Length != targetSize)
        {
            var oldStates = runData.SlotStates ?? new GridSlotState[0];
            runData.SlotStates = new GridSlotState[targetSize];

            for (int i = 0; i < runData.SlotStates.Length; i++)
            {
                if (i < oldStates.Length)
                    runData.SlotStates[i] = oldStates[i];
                else
                    runData.SlotStates[i] = new GridSlotState { IsUnlocked = false };
            }
        }
    }

    private static void ApplyPattern(RunData runData, GridConfiguration config, List<Vector2Int> pattern)
    {
        // Reset primeiro? Depende da regra. Aqui estamos inicializando.
        foreach (var coord in pattern)
        {
            int c = coord.x;
            int r = coord.y;

            if (c >= 0 && c < config.Columns && r >= 0 && r < config.Rows)
            {
                int index = r * config.Columns + c;
                if (index >= 0 && index < runData.SlotStates.Length)
                {
                    runData.SlotStates[index].IsUnlocked = true;
                }
            }
        }
    }

    private static int GenerateSeed()
    {
        return UnityEngine.Random.Range(int.MinValue, int.MaxValue);
    }
}
