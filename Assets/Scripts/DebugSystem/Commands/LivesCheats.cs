using UnityEngine;

[Cheat("add_life", "Progression", "Adiciona 1 vida ao jogador.")]
public class AddLifeCheat : ICheatCommand
{
    public string Id => "add_life";
    public string Category => "Progression";
    public string Description => "Adiciona 1 vida ao jogador.";

    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        var ctx = CheatContext.Instance;
        var run = ctx.SaveManager?.Data?.CurrentRun;
        if (run == null) { feedback = "❌ Nenhuma run ativa."; return false; }

        if (run.CurrentLives < run.MaxLives)
        {
            run.CurrentLives++;
            ctx.Events.Progression.TriggerLivesChanged(run.CurrentLives);
            feedback = $"✅ Vida adicionada: {run.CurrentLives}";
            return true;
        }
        feedback = "❌ Vidas já estão no máximo.";
        return false;
    }
}

[Cheat("remove_life", "Progression", "Remove 1 vida do jogador.")]
public class RemoveLifeCheat : ICheatCommand
{
    public string Id => "remove_life";
    public string Category => "Progression";
    public string Description => "Remove 1 vida do jogador.";

    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        var ctx = CheatContext.Instance;
        var run = ctx.SaveManager?.Data?.CurrentRun;
        if (run == null) { feedback = "❌ Nenhuma run ativa."; return false; }

        if (run.CurrentLives > 0)
        {
            run.CurrentLives--;
            ctx.Events.Progression.TriggerLivesChanged(run.CurrentLives);
            feedback = $"✅ Vida removida: {run.CurrentLives}";
            return true;
        }
        feedback = "❌ Vidas já estão em zero.";
        return false;
    }
}
