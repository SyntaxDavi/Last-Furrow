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
            GridSlots = new CropState[9] // Garante array limpo
        };

        // Regra de Design: Deck Inicial
        run.DeckIDs.Add("card_carrot");
        run.DeckIDs.Add("card_corn");
        run.DeckIDs.Add("card_carrot");
        run.DeckIDs.Add("card_corn");
        run.DeckIDs.Add("card_water"); 
        return run;
    }
}