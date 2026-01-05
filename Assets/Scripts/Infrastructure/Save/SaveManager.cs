using UnityEngine;
using System;
using System.IO;

public class SaveManager : MonoBehaviour, ISaveManager
{
    private const string SAVE_FILENAME = "last_furrow_save.json";
    private const string BACKUP_EXTENSION = ".bak";

    private string _savePath;
    private string _backupPath;

    public GameData Data { get; private set; }

    public void Initialize()
    {
        _savePath = Path.Combine(Application.persistentDataPath, SAVE_FILENAME);
        _backupPath = _savePath + BACKUP_EXTENSION;

        LoadGame();
    }

    public void SaveGame()
    {
        if (Data == null)
        {
            Debug.LogError("[SaveManager] CRÍTICO: Tentativa de salvar dados nulos.");
            return;
        }

        // Garante que a versão está atualizada antes de salvar
        Data.SaveVersion = GameData.CURRENT_VERSION;

        try
        {
            string json = JsonUtility.ToJson(Data, true);
            string tempPath = _savePath + ".tmp";

            // 1. Atomic Write
            File.WriteAllText(tempPath, json);

            // 2. Rotação de Backup (Só faz backup se o arquivo principal atual existir e for válido)
            if (File.Exists(_savePath))
            {
                File.Copy(_savePath, _backupPath, true);
            }

            // 3. Finalização
            if (File.Exists(_savePath)) File.Delete(_savePath);
            File.Move(tempPath, _savePath);

            // Debug.Log("[SaveManager] Salvo com sucesso."); // Comentado para evitar spam no console
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] ERRO AO SALVAR: {e.Message}");
        }
    }

    public void LoadGame()
    {
        // 1. Tenta carregar o Principal
        if (TryLoadFromFile(_savePath, out GameData loadedData))
        {
            Data = loadedData;
            Debug.Log($"[SaveManager] Save v{Data.SaveVersion} carregado.");
            CheckMigration(); // Verifica se precisa atualizar dados antigos
            return;
        }

        // 2. Falhou? Tenta Backup
        Debug.LogWarning("[SaveManager] Save principal falhou. Tentando Backup...");

        if (TryLoadFromFile(_backupPath, out GameData backupData))
        {
            Data = backupData;
            Debug.LogWarning("[SaveManager] RECUPERADO VIA BACKUP.");

            // 3. AUTO-REPARO: Se o backup funcionou, conserta o arquivo principal IMEDIATAMENTE.
            // Isso evita que o jogador continue jogando sem um arquivo principal válido.
            SaveGame();

            CheckMigration();
            return;
        }

        // 3. Tudo falhou? Novo Jogo.
        Debug.LogError("[SaveManager] Nenhum save válido. Criando Novo Perfil.");
        CreateNewGameData();
    }

    private bool TryLoadFromFile(string path, out GameData result)
    {
        result = null;
        if (!File.Exists(path)) return false;

        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrEmpty(json)) return false;

            GameData temp = JsonUtility.FromJson<GameData>(json);
            if (temp == null) return false;

            // VALIDAÇÃO DE VERSÃO
            // Se o save for muito antigo (ex: v1) e o jogo está na v5,
            // aqui você decidiria se tenta carregar ou descarta.
            // Por enquanto, aceitamos qualquer versão e migramos na memória.

            result = temp;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] Erro de leitura em {Path.GetFileName(path)}: {e.Message}");
            return false;
        }
    }

    // Lugar reservado para lógica futura de atualização de saves antigos
    private void CheckMigration()
    {
        if (Data.SaveVersion < GameData.CURRENT_VERSION)
        {
            Debug.Log($"[SaveManager] Migrando save da v{Data.SaveVersion} para v{GameData.CURRENT_VERSION}...");

            // Exemplo futuro:
            // if (Data.SaveVersion == 1) { Data.NewField = "Default"; }

            Data.SaveVersion = GameData.CURRENT_VERSION;
            SaveGame(); // Salva a versão migrada
        }
    }

    public void DeleteSave()
    {
        try
        {
            if (File.Exists(_savePath)) File.Delete(_savePath);
            if (File.Exists(_backupPath)) File.Delete(_backupPath);
            CreateNewGameData();
            Debug.Log("[SaveManager] Dados resetados.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Erro ao deletar: {e.Message}");
        }
    }

    private void CreateNewGameData()
    {
        // Usa a Factory do GameData, centralizando a lógica de "Nascimento"
        Data = GameData.CreateNew();
    }
}