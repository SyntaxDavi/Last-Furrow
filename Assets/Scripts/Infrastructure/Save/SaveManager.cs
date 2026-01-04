using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour, ISaveManager
{
    private string savePath;
    public GameData Data { get; private set; }

    public void Initialize()
    {
        savePath = Path.Combine(Application.persistentDataPath, "last_furrow_save.json");
        LoadGame();
    }

    public void SaveGame()
    {
        string json = JsonUtility.ToJson(Data, true);
        File.WriteAllText(savePath, json);
        Debug.Log("Jogo Salvo.");
    }

    public void LoadGame()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            Data = JsonUtility.FromJson<GameData>(json);
        }
        else
        {
            Data = new GameData(); // Novo jogo se não existir save
        }
    }

    public void DeleteSave()
    {
        Data = new GameData();
        if (File.Exists(savePath)) File.Delete(savePath);
    }
}