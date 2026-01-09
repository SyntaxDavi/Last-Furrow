using System;
using System.Collections.Generic;

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