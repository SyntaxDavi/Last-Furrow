using UnityEngine;
using System;
using System.IO;

public class SaveManager : MonoBehaviour, ISaveManager
{
    private const string SAVE_FILENAME = "last_furrow_save.json";
    private const string BACKUP_EXTENSION = ".bak";

    private string _savePath;

    // Propriedade pública
    public GameData Data { get; private set; }

    public void Initialize()
    {
        _savePath = Path.Combine(Application.persistentDataPath, SAVE_FILENAME);
        LoadGame();
    }

    public void SaveGame()
    {
        // Se Data for nulo por algum bug catastrófico, não salve (para não apagar o save bom com nada)
        if (Data == null)
        {
            Debug.LogError("[SaveManager] Tentativa de salvar dados nulos! Abortando.");
            return;
        }

        try
        {
            // 1. Serializa para JSON
            string json = JsonUtility.ToJson(Data, true);

            // 2. Escreve em um arquivo temporário primeiro (Atomic Save Pattern)
            // Isso evita corrupção se o jogo fechar ENQUANTO salva.
            string tempPath = _savePath + ".tmp";
            File.WriteAllText(tempPath, json);

            // 3. Se chegou aqui, a escrita funcionou. Agora substituímos o arquivo real.
            if (File.Exists(_savePath))
            {
                File.Delete(_savePath);
            }

            File.Move(tempPath, _savePath);

            Debug.Log("[SaveManager] Jogo salvo com segurança.");
        }
        catch (Exception e)
        {
            // Se der erro (ex: disco cheio), o jogo continua rodando e o save antigo não foi corrompido
            Debug.LogError($"[SaveManager] Falha ao salvar o jogo: {e.Message}");
        }
    }

    public void LoadGame()
    {
        if (!File.Exists(_savePath))
        {
            Debug.Log("[SaveManager] Nenhum save encontrado. Criando novo jogo.");
            CreateNewGameData();
            return;
        }

        try
        {
            string json = File.ReadAllText(_savePath);

            // Validação simples: O arquivo está vazio?
            if (string.IsNullOrEmpty(json))
            {
                throw new Exception("Arquivo de save vazio.");
            }

            GameData loadedData = JsonUtility.FromJson<GameData>(json);

            // Validação de Integridade: O JsonUtility retornou algo?
            if (loadedData == null)
            {
                throw new Exception("JSON malformado ou incompatível.");
            }

            // SUCESSO
            Data = loadedData;
            Debug.Log("[SaveManager] Jogo carregado com sucesso.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Save corrompido ou erro de leitura: {e.Message}");

            // DECISÃO DE DESIGN: O que fazer se o save estiver podre?
            // Opção A: Criar um novo (Reset forçado) -> Mais seguro para evitar travamentos
            // Opção B: Tentar carregar um backup (se você implementar sistema de backup)

            Debug.LogWarning("[SaveManager] Reiniciando dados devido a erro crítico.");
            CreateNewGameData();
        }
    }

    public void DeleteSave()
    {
        try
        {
            if (File.Exists(_savePath))
            {
                File.Delete(_savePath);
                Debug.Log("[SaveManager] Arquivo de save deletado.");
            }

            // Reseta a memória também
            CreateNewGameData();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Erro ao deletar save: {e.Message}");
        }
    }

    // Helper para centralizar a lógica de "Novo Jogo"
    private void CreateNewGameData()
    {
        Data = new GameData();
        // Aqui você pode configurar valores iniciais globais se necessário
        // ex: Data.TotalSouls = 0;
    }
}