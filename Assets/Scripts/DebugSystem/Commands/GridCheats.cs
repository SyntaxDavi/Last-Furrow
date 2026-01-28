using UnityEngine;
using UnityEngine.SceneManagement;

[Cheat("unlock_grid", "Grid", "Desbloqueia todos os slots do grid.")]
public class UnlockGridCheat : ICheatCommand
{
    public string Id => "unlock_grid";
    public string Category => "Grid";
    public string Description => "Desbloqueia todos os slots do grid.";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        var run = CheatContext.Instance.SaveManager?.Data?.CurrentRun;
        if (run == null) { feedback = "❌ Nenhuma run ativa."; return false; }

        int count = 0;
        foreach (var slot in run.SlotStates)
        {
            if (!slot.IsUnlocked) { slot.IsUnlocked = true; count++; }
        }

        CheatContext.Instance.SaveManager.SaveGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        feedback = $"✅ {count} slots desbloqueados. Recarregando...";
        return true;
    }
}

[Cheat("clear_grid", "Grid", "Remove todas as plantas do grid.")]
public class ClearGridCheat : ICheatCommand
{
    public string Id => "clear_grid";
    public string Category => "Grid";
    public string Description => "Remove todas as plantas do grid.";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        var run = CheatContext.Instance.SaveManager?.Data?.CurrentRun;
        if (run == null) { feedback = "❌ Nenhuma run ativa."; return false; }

        for (int i = 0; i < run.GridSlots.Length; i++)
        {
            run.GridSlots[i] = new CropState(default(CropID));
        }

        CheatContext.Instance.SaveManager.SaveGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        feedback = "✅ Grid limpo. Recarregando...";
        return true;
    }
}

[Cheat("water_all", "Grid", "Rega todas as plantas no grid.")]
public class WaterAllCheat : ICheatCommand
{
    public string Id => "water_all";
    public string Category => "Grid";
    public string Description => "Rega todas as plantas no grid.";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        var run = CheatContext.Instance.SaveManager?.Data?.CurrentRun;
        if (run == null) { feedback = "❌ Nenhuma run ativa."; return false; }

        int count = 0;
        foreach (var slot in run.GridSlots)
        {
            if (!slot.IsEmpty && !slot.IsWatered) { slot.IsWatered = true; count++; }
        }

        CheatContext.Instance.SaveManager.SaveGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        feedback = $"✅ {count} plantas regadas. Recarregando...";
        return true;
    }
}

[Cheat("mature_all", "Grid", "Faz todas as plantas amadurecerem instantaneamente.")]
public class MatureAllCheat : ICheatCommand
{
    public string Id => "mature_all";
    public string Category => "Grid";
    public string Description => "Faz todas as plantas amadurecerem instantaneamente.";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        var run = CheatContext.Instance.SaveManager?.Data?.CurrentRun;
        if (run == null) { feedback = "❌ Nenhuma run ativa."; return false; }

        int count = 0;
        foreach (var slot in run.GridSlots)
        {
            if (!slot.IsEmpty && slot.DaysMature == 0)
            {
                slot.CurrentGrowth = 100;
                slot.DaysMature = 1;
                count++;
            }
        }

        CheatContext.Instance.SaveManager.SaveGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        feedback = $"✅ {count} plantas maduras. Recarregando...";
        return true;
    }
}

[Cheat("auto_plant", "Grid", "Planta crops aleatórios em todos os slots vazios e desbloqueados.")]
public class AutoPlantCheat : ICheatCommand
{
    public string Id => "auto_plant";
    public string Category => "Grid";
    public string Description => "Planta crops aleatórios em todos os slots vazios e desbloqueados.";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        var ctx = CheatContext.Instance;
        var run = ctx.SaveManager?.Data?.CurrentRun;
        if (run == null) { feedback = "❌ Nenhuma run ativa."; return false; }

        string[] cropIDs = { "crop_corn", "crop_carrot" };
        var validCrops = new System.Collections.Generic.List<string>();
        foreach (var id in cropIDs)
        {
            if (ctx.Library.TryGetCrop(new CropID(id), out _)) validCrops.Add(id);
        }

        if (validCrops.Count == 0) { feedback = "❌ Nenhum crop válido no GameLibrary."; return false; }

        int count = 0;
        for (int i = 0; i < run.GridSlots.Length; i++)
        {
            if (run.SlotStates[i].IsUnlocked && run.GridSlots[i].IsEmpty)
            {
                string randomId = validCrops[Random.Range(0, validCrops.Count)];
                run.GridSlots[i] = new CropState(new CropID(randomId)) { CurrentGrowth = 0, IsWatered = false, DaysMature = 0 };
                count++;
            }
        }

        ctx.SaveManager.SaveGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        feedback = $"✅ {count} crops plantados. Recarregando...";
        return true;
    }
}

[Cheat("elevate_row", "Grid", "Levanta todos os slots de uma linha (0-4).")]
public class ElevateRowCheat : ICheatCommand
{
    public string Id => "elevate_row";
    public string Category => "Grid";
    public string Description => "Levanta todos os slots de uma linha (0-4).";
    public bool ValidateArgs(string[] args, out string error)
    {
        if (args.Length == 0 || !int.TryParse(args[0], out int row) || row < 0 || row > 4)
        {
            error = "Uso: elevate_row <0-4>";
            return false;
        }
        error = null;
        return true;
    }

    public bool Execute(string[] args, out string feedback)
    {
        int targetRow = int.Parse(args[0]);
        var slots = Object.FindObjectsByType<GridSlotView>(FindObjectsSortMode.None);
        int count = 0;

        foreach (var slot in slots)
        {
            int row = slot.SlotIndex / 5; // Assumindo grid 5x5
            if (row == targetRow)
            {
                slot.SetElevationFactor(1f);
                count++;
            }
            else
            {
                slot.SetElevationFactor(0f);
            }
        }

        feedback = $"✅ {count} slots levantados na linha {targetRow}.";
        return true;
    }
}

[Cheat("elevate_clear", "Grid", "Reseta a elevação de todos os slots.")]
public class ElevateClearCheat : ICheatCommand
{
    public string Id => "elevate_clear";
    public string Category => "Grid";
    public string Description => "Reseta a elevação de todos os slots.";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        var slots = Object.FindObjectsByType<GridSlotView>(FindObjectsSortMode.None);
        foreach (var slot in slots) slot.SetElevationFactor(0f);
        feedback = "✅ Elevação resetada.";
        return true;
    }
}
