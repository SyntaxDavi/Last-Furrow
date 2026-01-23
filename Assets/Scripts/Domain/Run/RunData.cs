using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RunData
{
    [Header("Versionamento")]
    [Tooltip("Hash da GridConfiguration usada ao criar esta run. Usado para validar compatibilidade ao carregar saves.")]
    public int GridConfigVersion;

    [Header("Grid Unlock State")]
    [Tooltip("? AUTORIDADE: Seed usado para gerar padrão inicial de desbloqueamento.")]
    public int UnlockPatternSeed;

    [Tooltip("? CONTRATO EXPLÍCITO: Flag que indica se grid foi inicializado. Previne reinicializações acidentais.")]
    public bool IsGridInitialized;

    [Tooltip("Cache do padrão gerado (pode ser regenerado a partir do Seed se necessário).")]
    public GridUnlockState UnlockState;

    [Header("Progressão Semanal")]
    public int CurrentWeeklyScore;
    public int WeeklyGoalTarget;
    public int CurrentLives;
    public int MaxLives;
    public int CurrentWeek;
    public int CurrentDay;
    public List<string> DeckIDs;
    public CropState[] GridSlots;
    public GridSlotState[] SlotStates;

    public List<CardInstance> Hand = new List<CardInstance>();


    public int MaxHandSize = 10;
    public int CardsDrawPerDay = 3;

    public int Money;
    public int TotalMoneyEarned;
    
    // ===== ONDA 4: Pattern Tracking =====
    
    [Header("Pattern System - Tracking")]
    [Tooltip("Total de padrões detectados durante toda a run.")]
    public int TotalPatternsDetected;
    
    [Tooltip("Maior pontuação de padrões em um único dia.")]
    public int HighestDailyPatternScore;
    
    [Tooltip("Contador de quantas vezes cada tipo de padrão foi completado. Usa PatternID (ex: 'FULL_LINE').")]
    public Dictionary<string, int> PatternCompletionCount;
    
    [Tooltip("Padrões atualmente ativos com seus dados de decay. Indexado por InstanceID.")]
    public Dictionary<string, PatternInstanceData> ActivePatterns;
    
    [Tooltip("Lista de PatternIDs que foram quebrados (para detectar recriação e dar bonus).")]
    public List<string> BrokenPatternIDs;

    [Header("Daily State Tracking")]
    [Tooltip("CRÍTICO: Garante que o draw diário aconteça apenas uma vez por dia lógico.")]
    public bool HasDrawnDailyHand = false;

    // Construtor padrão (usado pelo JSON Utility ou Serializer)
    // Mantemos ele "burro" apenas alocando listas para evitar NullReference
    public RunData()
    {
        DeckIDs = new List<string>();
        // Inicializa zerado ou com mínimo para evitar nulls imediatos, 
        // mas o tamanho real será corrigido pelo GridService.
        GridSlots = new CropState[0]; 
        SlotStates = new GridSlotState[0];
        
        // Pattern Tracking (Onda 4)
        PatternCompletionCount = new Dictionary<string, int>();
        ActivePatterns = new Dictionary<string, PatternInstanceData>();
        BrokenPatternIDs = new List<string>();
    }


    // FACTORY METHOD (A Regra de Negócio mora aqui)
    // É aqui que definimos como uma Run começa de verdade.

    public bool IsHealthFull()
    {
        return CurrentLives >= MaxLives;
    }
    public void Heal(int amount)
    {
        if (amount <= 0) return;

        CurrentLives += amount;
        if (CurrentLives > MaxLives)
        {
            CurrentLives = MaxLives;
        }
        // Nota: Não disparamos evento aqui porque RunData é apenas DADOS.
        // Quem chama (Service ou Item) dispara o evento.
    }
    public static RunData CreateNewRun(GridConfiguration config)
    {
        if (config == null)
        {
            throw new System.ArgumentNullException(nameof(config), 
                "[RunData] GridConfiguration não pode ser null ao criar nova run!");
        }

        int initialGoal = 150; // Valor padrão se não houver settings
        int slotCount = config.TotalSlots;

        var run = new RunData
        {
            // ? VERSIONAMENTO: Armazena hash da config
            GridConfigVersion = config.GetVersionHash(),

            CurrentWeek = 1,
            CurrentDay = 1,
            GridSlots = new CropState[slotCount],
            SlotStates = new GridSlotState[slotCount],
            Hand = new List<CardInstance>(),

            MaxHandSize = 10,
            CardsDrawPerDay = 3,

            CurrentWeeklyScore = 0,
            WeeklyGoalTarget = initialGoal, 
            CurrentLives = 3,
            MaxLives = 3
        };

        // --- CORREÇÃO AQUI ---
        // Antes você adicionava strings em DeckIDs.
        // Agora criamos instâncias reais na lista Hand.

        AddStartingCard(run, "card_carrot");
        AddStartingCard(run, "card_corn");
        AddStartingCard(run, "card_harvest");
        AddStartingCard(run, "card_shovel");
        AddStartingCard(run, "card_water");

        return run;
    }

    /// <summary>
    /// Helper para adicionar carta inicial na run.
    /// 
    /// ? VALIDAÇÃO: Garante que o CardID não é vazio.
    /// Se o ID for inválido, loga erro mas não quebra a criação da run.
    /// 
    /// ARQUITETURA: Idealmente, isso deveria vir de uma config (StartingDeckSO),
    /// mas por simplicidade inicial, está hardcoded aqui.
    /// </summary>
    private static void AddStartingCard(RunData run, string cardIDString)
    {
        // Validação básica
        if (string.IsNullOrEmpty(cardIDString))
        {
            Debug.LogError("[RunData] Tentativa de adicionar carta inicial com ID vazio!");
            return;
        }

        // Conversão explícita (segura para Value Objects)
        CardID id = (CardID)cardIDString;

        // Validação extra do Value Object
        if (!id.IsValid)
        {
            Debug.LogError($"[RunData] CardID inválido ao criar run: '{cardIDString}'");
            return;
        }

        // Cria instância e adiciona na mão
        CardInstance instance = new CardInstance(id);
        run.Hand.Add(instance);
    }

    /// <summary>
    /// Valida se esta RunData é compatível com uma GridConfiguration.
    /// 
    /// POLÍTICA: Se os hashes não baterem, o save é INCOMPATÍVEL.
    /// Não há migração - o jogador deve iniciar uma nova run.
    /// 
    /// Isso previne corrupção de dados quando o mundo muda estruturalmente.
    /// </summary>
    public bool IsCompatibleWith(GridConfiguration config)
    {
        if (config == null)
        {
            Debug.LogError("[RunData] GridConfiguration null ao validar compatibilidade!");
            return false;
        }

        int currentConfigHash = config.GetVersionHash();
        bool isCompatible = GridConfigVersion == currentConfigHash;

        if (!isCompatible)
        {
            Debug.LogWarning(
                $"[RunData] Save incompatível detectado!\n" +
                $"Save Version: {GridConfigVersion}\n" +
                $"Config Atual: {currentConfigHash}\n" +
                $"Diferença: Grid foi alterado desde a criação deste save."
            );
        }

        return isCompatible;
    }
}