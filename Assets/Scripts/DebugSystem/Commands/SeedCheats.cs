using UnityEngine;
using UnityEngine.SceneManagement;

[Cheat("set_master_seed", "Seed", "Força um MasterSeed específico e recarrega.")]
public class SetMasterSeedCheat : ICheatCommand
{
    public string Id => "set_master_seed";
    public string Category => "Seed";
    public string Description => "Força um MasterSeed específico e recarrega.";
    
    public bool ValidateArgs(string[] args, out string error)
    {
        if (args.Length == 0 || !int.TryParse(args[0], out _))
        {
            error = "Uso: set_master_seed <inteiro>";
            return false;
        }
        error = null;
        return true;
    }

    public bool Execute(string[] args, out string feedback)
    {
        var run = CheatContext.Instance.SaveManager?.Data?.CurrentRun;
        if (run == null) { feedback = "❌ Nenhuma run ativa."; return false; }

        int newSeed = int.Parse(args[0]);
        run.MasterSeed = newSeed;

        CheatContext.Instance.SaveManager.SaveGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        feedback = $"✅ MasterSeed alterado para {newSeed}. Recarregando...";
        return true;
    }
}

[Cheat("set_unlock_seed", "Seed", "Força um UnlockPatternSeed e regenera o padrão do grid.")]
public class SetUnlockSeedCheat : ICheatCommand
{
    public string Id => "set_unlock_seed";
    public string Category => "Seed";
    public string Description => "Força um UnlockPatternSeed e regenera o padrão do grid.";

    public bool ValidateArgs(string[] args, out string error)
    {
        if (args.Length == 0 || !int.TryParse(args[0], out _))
        {
            error = "Uso: set_unlock_seed <inteiro>";
            return false;
        }
        error = null;
        return true;
    }

    public bool Execute(string[] args, out string feedback)
    {
        var ctx = CheatContext.Instance;
        var run = ctx.SaveManager?.Data?.CurrentRun;
        if (run == null) { feedback = "❌ Nenhuma run ativa."; return false; }

        int newSeed = int.Parse(args[0]);
        run.UnlockPatternSeed = newSeed;

        // Regenera o padrão imediatamente
        GridInitializer.ReapplyUnlockPattern(run, ctx.GridService.Config);

        ctx.SaveManager.SaveGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        feedback = $"✅ UnlockSeed alterado para {newSeed} e padrão regenerado. Recarregando...";
        return true;
    }
}

[Cheat("reroll_seeds", "Seed", "Gera novos seeds aleatórios para Master e Unlock e regenera o grid.")]
public class RerollSeedsCheat : ICheatCommand
{
    public string Id => "reroll_seeds";
    public string Category => "Seed";
    public string Description => "Gera novos seeds aleatórios para Master e Unlock e regenera o grid.";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        var ctx = CheatContext.Instance;
        var run = ctx.SaveManager?.Data?.CurrentRun;
        if (run == null) { feedback = "❌ Nenhuma run ativa."; return false; }

        run.MasterSeed = Random.Range(int.MinValue, int.MaxValue);
        run.UnlockPatternSeed = Random.Range(int.MinValue, int.MaxValue);

        GridInitializer.ReapplyUnlockPattern(run, ctx.GridService.Config);

        ctx.SaveManager.SaveGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        feedback = $"✅ Seeds resetados (Master: {run.MasterSeed}, Unlock: {run.UnlockPatternSeed}). Recarregando...";
        return true;
    }
}
