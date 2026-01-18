using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Responsável por inicialização ONE-SHOT do grid.
/// 
/// FASE DO CICLO DE VIDA: Inicialização / Run Start (uma vez)
/// 
/// RESPONSABILIDADES:
/// - Validar tamanho do grid
/// - Migrar dados de saves antigos
/// - Gerar ou validar GridUnlockState
/// - Aplicar padrão inicial de desbloqueamento
/// 
/// NÃO FAZ:
/// - Decidir regras de unlock durante gameplay (isso é GridService)
/// - Processar biologia de plantas (isso é GridService)
/// - Emitir eventos (isso é GridService)
/// 
/// SOLID - Single Responsibility:
/// GridService delega inicialização para cá e recebe grid pronto.
/// </summary>
public static class GridInitializer
{
    private const int ALGORITHM_VERSION = 1;

    /// <summary>
    /// Inicializa ou valida grid para uma run.
    /// 
    /// FLUXO:
    /// 1. Valida/migra tamanho do grid
    /// 2. Valida/migra SlotStates
    /// 3. Gera padrão inicial SE necessário (flag IsGridInitialized)
    /// 4. Aplica desbloqueios
    /// </summary>
    public static void Initialize(
        RunData runData,
        GridConfiguration config,
        int initialSlotCount = 5,
        PatternWeightConfig patternConfig = null)
    {
        if (config == null)
        {
            Debug.LogError("[GridInitializer] ? GridConfiguration é null! Não é possível inicializar.");
            return;
        }

        int targetSize = config.TotalSlots;

        // 1. Migração de Tamanho
        EnsureGridSize(runData, targetSize);

        // 2. Migração de SlotStates
        EnsureSlotStates(runData, targetSize);

        // 3. Inicialização de Gameplay (Padrão de Desbloqueio)
        if (!runData.IsGridInitialized)
        {
            InitializeUnlockPattern(runData, config, initialSlotCount, patternConfig);
            runData.IsGridInitialized = true; // ? CONTRATO EXPLÍCITO
        }
        else
        {
            // Grid já foi inicializado, valida se padrão está compatível
            ValidateUnlockState(runData, config);
        }
    }

    /// <summary>
    /// Garante que GridSlots tem tamanho correto.
    /// </summary>
    private static void EnsureGridSize(RunData runData, int targetSize)
    {
        if (runData.GridSlots == null || runData.GridSlots.Length != targetSize)
        {
            Debug.Log($"[GridInitializer] Ajustando Grid para {targetSize} slots...");
            var oldSlots = runData.GridSlots ?? new CropState[0];
            runData.GridSlots = new CropState[targetSize];

            for (int i = 0; i < runData.GridSlots.Length; i++)
            {
                if (i < oldSlots.Length)
                    runData.GridSlots[i] = oldSlots[i];
                else
                    runData.GridSlots[i] = new CropState();
            }
        }
    }

    /// <summary>
    /// Garante que SlotStates tem tamanho correto.
    /// </summary>
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
                    runData.SlotStates[i] = new GridSlotState(false);
            }
        }
    }

    /// <summary>
    /// Inicializa padrão de desbloqueamento (run nova).
    /// 
    /// ? AUTORIDADE: Seed é salvo no RunData.
    /// Pattern é cache que pode ser regenerado.
    /// </summary>
    private static void InitializeUnlockPattern(
        RunData runData,
        GridConfiguration config,
        int slotCount,
        PatternWeightConfig patternConfig)
    {
        // 1. Gera seed se ainda não existe
        if (runData.UnlockPatternSeed == 0)
        {
            runData.UnlockPatternSeed = GenerateSeed();
        }

        // 2. Gera padrão usando seed
        var pattern = UnlockPatternGenerator.Generate(
            runData.UnlockPatternSeed,
            slotCount,
            config.Columns,
            config.Rows,
            patternConfig
        );

        // 3. Salva estado para validação futura
        runData.UnlockState = GridUnlockState.Create(
            config.Columns,
            config.Rows,
            slotCount,
            pattern
        );

        // 4. Aplica desbloqueios
        ApplyPattern(runData, config, pattern);

        Debug.Log(
            $"[GridInitializer] ? Grid inicializado | " +
            $"Seed: {runData.UnlockPatternSeed} | " +
            $"Slots: {pattern.Count}/{slotCount}"
        );
    }

    /// <summary>
    /// Valida se UnlockState está compatível com grid atual.
    /// Se não estiver, regenera a partir do seed.
    /// </summary>
    private static void ValidateUnlockState(RunData runData, GridConfiguration config)
    {
        if (runData.UnlockState == null)
        {
            Debug.LogWarning("[GridInitializer] ?? UnlockState null em grid inicializado! Regenerando...");
            RegenerateFromSeed(runData, config);
            return;
        }

        bool isCompatible = runData.UnlockState.IsCompatibleWith(
            config.Columns,
            config.Rows,
            ALGORITHM_VERSION
        );

        if (!isCompatible)
        {
            Debug.LogWarning(
                $"[GridInitializer] ?? UnlockState incompatível!\n" +
                $"Esperado: {config.Columns}×{config.Rows} v{ALGORITHM_VERSION}\n" +
                $"Atual: {runData.UnlockState.GridWidth}×{runData.UnlockState.GridHeight} v{runData.UnlockState.AlgorithmVersion}\n" +
                $"? Regenerando a partir do Seed: {runData.UnlockPatternSeed}"
            );
            RegenerateFromSeed(runData, config);
        }
    }

    /// <summary>
    /// Regenera padrão a partir do seed (cache invalidado).
    /// </summary>
    private static void RegenerateFromSeed(RunData runData, GridConfiguration config)
    {
        if (runData.UnlockPatternSeed == 0)
        {
            Debug.LogError("[GridInitializer] ? Seed é 0! Não é possível regenerar. Criando novo seed.");
            runData.UnlockPatternSeed = GenerateSeed();
        }

        int slotCount = runData.UnlockState?.TargetSlotCount ?? 5;

        var pattern = UnlockPatternGenerator.Generate(
            runData.UnlockPatternSeed,
            slotCount,
            config.Columns,
            config.Rows,
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

    /// <summary>
    /// Aplica padrão ao grid (desbloqueia slots).
    /// </summary>
    private static void ApplyPattern(RunData runData, GridConfiguration config, List<Vector2Int> pattern)
    {
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

    /// <summary>
    /// Gera seed único baseado em timestamp.
    /// </summary>
    private static int GenerateSeed()
    {
        return System.DateTime.Now.Ticks.GetHashCode();
    }
}
