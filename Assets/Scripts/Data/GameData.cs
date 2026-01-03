using System;
using System.Collections.Generic;

[Serializable]
public class GameData
{
    public int TotalSouls;
    public List<string> UnlockedCards = new List<string>();
    public RunData CurrentRun;
}

[Serializable]
public class RunData    
{
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
            CardsDrawPerDay = 5
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