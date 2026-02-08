using System;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;
using LastFurrow.Traditions;

[Serializable]
public class RunData
{
    [Header("Versionamento")]
    [Tooltip("Hash da GridConfiguration usada ao criar esta run. Usado para validar compatibilidade ao carregar saves.")]
    public int GridConfigVersion;

    [Header("Grid Unlock State")]
    [Tooltip("AUTORIDADE: Seed principal da run. Usado para gerar o IRandomProvider determinístico.")]
    public int MasterSeed;

    [Tooltip("AUTORIDADE: Seed usado para gerar padrão inicial de desbloqueamento.")]
    public int UnlockPatternSeed;

    [Tooltip("CONTRATO EXPLÍCITO: Flag que indica se grid foi inicializado. Previne reinicializações acidentais.")]    
    public bool IsGridInitialized;

    [Tooltip("CONTRATO DE GERAÇÃO: Cache do padrão gerado. Serve como referência para validação e auto-cura.")]
    [FormerlySerializedAs("UnlockState")]
    public GridUnlockState GenerationContract;

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

    // Valores padrão - serão sobrescritos no CreateNewRun com GameSettingsSO
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

    // ===== TRADITIONS =====
    
    [Header("Traditions")]
    [Tooltip("IDs das tradições ativas nesta run. Ordem importa para efeitos.")]
    public List<TraditionID> ActiveTraditionIDs = new List<TraditionID>();
    
    [Tooltip("Máximo de tradições que podem ser equipadas. Pode ser aumentado por upgrades.")]
    public int MaxTraditionSlots = 5;

    // ===== RUN DECK (ECONOMIA DETERMINÍSTICA) =====
    
    [Header("Run Deck")]
    [Tooltip("IDs das cartas restantes no deck da run.")]
    public List<string> RunDeckCardIDs = new List<string>();
    
    // ===== DRAW VALIDATION TRACKING =====
    
    [Header("Draw Validation")]
    [Tooltip("Tracking de última aparição de cartas (dia absoluto). Usado por GuaranteedCardsRule.")]
    public Dictionary<string, int> CardLastSeenDays = new Dictionary<string, int>();
    
    [Tooltip("Cartas do último draw. Usado por NoRepeatDrawRule para evitar draws idênticos.")]
    public List<string> LastDrawCardIDs = new List<string>();
    
    [Tooltip("Índice de quantas cartas já foram sacadas do deck.")]
    public int RunDeckDrawIndex = 0;
    
    [Tooltip("Flag que indica se o deck da run foi inicializado.")]
    public bool IsRunDeckInitialized = false;

    [Header("Daily State Tracking")]
    [Tooltip("CRÍTICO: Garante que o draw diário aconteça apenas uma vez por dia lógico.")]
    public int LastDrawnDay = -1;
    public int LastDrawnWeek = -1;

    // Helper para facilitar a lógica sem quebrar outros sistemas
    public bool HasDrawnDailyHand => LastDrawnDay == CurrentDay && LastDrawnWeek == CurrentWeek;

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
        
        // Draw Validation Tracking
        CardLastSeenDays = new Dictionary<string, int>();
    }


    // FACTORY METHOD (A Regra de Negócio mora aqui)
    // É aqui que definimos como uma Run começa de verdade.
    public void MarkDailyHandDrawn()
    {
        LastDrawnDay = CurrentDay;
        LastDrawnWeek = CurrentWeek;
    }
    
    public static RunData CreateNewRun(GridConfiguration config, GameSettingsSO settings = null)
    {
        if (config == null)
        {
            throw new System.ArgumentNullException(nameof(config),
                "[RunData] GridConfiguration não pode ser null ao criar nova run!");
        }

        // Fallback para valores padrão se settings não fornecido
        int initialMaxHandSize = settings?.DefaultMaxHandSize ?? 10;
        int initialCardsDrawPerDay = settings?.DefaultCardsDrawPerDay ?? 3;
        int initialMaxLives = settings?.InitialMaxLives ?? 3;
        int initialGoal = settings?.InitialWeeklyGoal ?? 150;
        int masterSeedOverride = settings?.MasterSeedOverride ?? 0;
        int unlockPatternSeedOverride = settings?.UnlockPatternSeedOverride ?? 0;
        
        int slotCount = config.TotalSlots;

        var run = new RunData
        {
            // VERSIONAMENTO: Armazena hash da config
            GridConfigVersion = config.GetVersionHash(),

            // DETERMINISMO: Gera seeds baseados no tempo/random do sistema ou usa override
            MasterSeed = masterSeedOverride != 0 
                ? masterSeedOverride 
                : UnityEngine.Random.Range(int.MinValue, int.MaxValue),

            UnlockPatternSeed = unlockPatternSeedOverride != 0 
                ? unlockPatternSeedOverride 
                : UnityEngine.Random.Range(int.MinValue, int.MaxValue),

            CurrentWeek = 1,
            CurrentDay = 1,
            GridSlots = new CropState[slotCount],
            SlotStates = new GridSlotState[slotCount],
            Hand = new List<CardInstance>(),

            MaxHandSize = initialMaxHandSize,
            CardsDrawPerDay = initialCardsDrawPerDay,

            CurrentWeeklyScore = 0,
            WeeklyGoalTarget = initialGoal,
            CurrentLives = initialMaxLives,
            MaxLives = initialMaxLives
        };

        // --- DECK INICIAL CENTRALIZADO ---
        var startingDeckIDs = settings?.StartingDeckIDs ?? new List<string> 
        { 
            "card_carrot", 
            "card_corn", 
            "card_harvest", 
            "card_shovel", 
            "card_water" 
        };
        
        foreach (var cardID in startingDeckIDs)
        {
            AddStartingCard(run, cardID);
        }
        return run;
    }

    /// <summary>
    /// Helper para adicionar carta inicial na run.
    ///
    /// VALIDAÇÃO: Garante que o CardID não é vazio.
    /// Se o ID for inválido, loga erro mas não quebra a criação da run.
    ///
    /// ARQUITETURA: Idealmente, isso deveria vir de uma config (StartingDeckSO),
    /// mas por simplicidade inicial, está hardcoded no GameSettings.
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

    // ===== RUN DECK HELPERS =====

    /// <summary>
    /// Salva o estado atual do RunDeck para serialização.
    /// </summary>
    public void SaveRunDeck(RunDeck deck)
    {
        if (deck == null)
        {
            Debug.LogWarning("[RunData] Tentativa de salvar RunDeck null.");
            return;
        }

        var (cardIDs, drawIndex) = deck.GetSerializationData();
        RunDeckCardIDs = cardIDs;
        RunDeckDrawIndex = drawIndex;
        IsRunDeckInitialized = true;
    }

    /// <summary>
    /// Restaura o RunDeck a partir do estado salvo.
    /// </summary>
    public RunDeck LoadRunDeck()
    {
        if (!IsRunDeckInitialized || RunDeckCardIDs == null)
        {
            Debug.LogWarning("[RunData] RunDeck não inicializado ou dados corrompidos.");
            return null;
        }

        return new RunDeck(RunDeckCardIDs, RunDeckDrawIndex);
    }
}
