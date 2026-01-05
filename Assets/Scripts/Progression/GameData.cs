using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameData
{
    // CONTROLE DE VERSÃO
    // Sempre que você alterar a estrutura desta classe (adicionar campos), incremente isso.
    public const int CURRENT_VERSION = 1;

    public int SaveVersion;
    public int TotalSouls;
    public List<string> UnlockedCards;
    public RunData CurrentRun;

    // Construtor padrão privado ou protegido (para forçar uso da Factory)
    // O JsonUtility precisa dele, então deixamos público mas vazio.
    public GameData() { }

    // --- FACTORY METHOD (Nascimento Seguro) ---
    public static GameData CreateNew()
    {
        return new GameData
        {
            SaveVersion = CURRENT_VERSION,
            TotalSouls = 0,
            UnlockedCards = new List<string>(), // Inicializa listas para evitar null
            CurrentRun = null // Run começa nula, só é criada no StartRun
        };
    }
}

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

    public List<CardInstance> Hand = new List<CardInstance>();

    public int MaxHandSize = 15;
    public int CardsDrawPerDay = 5;

    public int Money;
    public int TotalMoneyEarned;

    // Construtor padrão (usado pelo JSON Utility ou Serializer)
    // Mantemos ele "burro" apenas alocando listas para evitar NullReference
    public RunData()
    {
        DeckIDs = new List<string>();
        GridSlots = new CropState[9];
    }

    // FACTORY METHOD (A Regra de Negócio mora aqui)
    // É aqui que definimos como uma Run começa de verdade.
    public static RunData CreateNewRun()
    {
        var run = new RunData
        {
            CurrentWeek = 1,
            CurrentDay = 1,
            GridSlots = new CropState[9],
            Hand = new List<CardInstance>(), 

            MaxHandSize = 15,
            CardsDrawPerDay = 5,

            CurrentWeeklyScore = 0,
            WeeklyGoalTarget = 150, // Meta da Semana 1
            CurrentLives = 3,
            MaxLives = 3
        };

        // --- CORREÇÃO AQUI ---
        // Antes você adicionava strings em DeckIDs.
        // Agora criamos instâncias reais na lista Hand.

        AddStartingCard(run, "card_carrot");
        AddStartingCard(run, "card_corn");
        AddStartingCard(run, "card_carrot");
        AddStartingCard(run, "card_corn");
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