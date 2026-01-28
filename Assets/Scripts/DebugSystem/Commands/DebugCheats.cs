using UnityEngine;
using UnityEngine.SceneManagement;

[Cheat("delete_save", "Meta", "Deleta o save atual e recomeça a cena.")]
public class DeleteSaveCheat : ICheatCommand
{
    public string Id => "delete_save";
    public string Category => "Meta";
    public string Description => "Deleta o save atual e recomeça a cena.";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        var ctx = CheatContext.Instance;
        if (ctx.SaveManager != null)
        {
            ctx.SaveManager.DeleteSave();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            feedback = "✅ Save deletado. Reiniciando...";
            return true;
        }
        feedback = "❌ SaveManager não disponível.";
        return false;
    }
}

[Cheat("save_game", "Meta", "Força um autosave imediato.")]
public class SaveGameCheat : ICheatCommand
{
    public string Id => "save_game";
    public string Category => "Meta";
    public string Description => "Força um autosave imediato.";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        CheatContext.Instance.SaveManager?.SaveGame();
        feedback = "✅ Jogo salvo.";
        return true;
    }
}

[Cheat("test_logs", "Debug", "Gera logs de teste no console.")]
public class TestLogsCheat : ICheatCommand
{
    public string Id => "test_logs";
    public string Category => "Debug";
    public string Description => "Gera logs de teste no console.";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        Debug.Log("[Cheat] ✓ Debug.Log funcionando!");
        Debug.LogWarning("[Cheat] ⚠️ Debug.LogWarning funcionando!");
        Debug.LogError("[Cheat] ❌ Debug.LogError funcionando!");
        feedback = "✅ Logs de teste gerados.";
        return true;
    }
}

[Cheat("toggle_logger", "Debug", "Ativa/Desativa o EventLogger.")]
public class ToggleLoggerCheat : ICheatCommand
{
    public string Id => "toggle_logger";
    public string Category => "Debug";
    public string Description => "Ativa/Desativa o EventLogger.";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        var logger = LastFurrow.EventInspector.EventLogger.Instance;
        if (logger == null) { feedback = "❌ EventLogger não disponível."; return false; }
        logger.IsEnabled = !logger.IsEnabled;
        feedback = $"✅ EventLogger {(logger.IsEnabled ? "ATIVADO" : "DESATIVADO")}";
        return true;
    }
}

[Cheat("export_events", "Debug", "Exporta eventos capturados para JSON.")]
public class ExportEventsCheat : ICheatCommand
{
    public string Id => "export_events";
    public string Category => "Debug";
    public string Description => "Exporta eventos capturados para JSON.";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        var logger = LastFurrow.EventInspector.EventLogger.Instance;
        if (logger == null) { feedback = "❌ EventLogger não disponível."; return false; }
        string path = logger.ExportToFile();
        feedback = string.IsNullOrEmpty(path) ? "❌ Falha ao exportar." : $"✅ Exportado para: {path}";
        return true;
    }
}

[Cheat("clear_events", "Debug", "Limpar eventos capturados.")]
public class ClearEventsCheat : ICheatCommand
{
    public string Id => "clear_events";
    public string Category => "Debug";
    public string Description => "Limpar eventos capturados.";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        LastFurrow.EventInspector.EventLogger.Instance?.Clear();
        feedback = "✅ Eventos limpos.";
        return true;
    }
}
