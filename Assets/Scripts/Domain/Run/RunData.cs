using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RunData
{
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

    // Construtor padrão (usado pelo JSON Utility ou Serializer)
    // Mantemos ele "burro" apenas alocando listas para evitar NullReference
    public RunData()
    {
        DeckIDs = new List<string>();
        // Inicializa zerado ou com mínimo para evitar nulls imediatos, 
        // mas o tamanho real será corrigido pelo GridService.
        GridSlots = new CropState[0]; 
        SlotStates = new GridSlotState[0];
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
        int initialGoal = 150; // Valor padrão se não houver settings
        
        int slotCount = config != null ? config.TotalSlots : 25;

        var run = new RunData
        {
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

    // Helper para facilitar a criação (pode ficar dentro do RunData mesmo)
    private static void AddStartingCard(RunData run, string cardIDString)
    {
        // Converte string -> CardID -> CardInstance
        CardID id = (CardID)cardIDString;
        CardInstance instance = new CardInstance(id);
        run.Hand.Add(instance);
    }
}