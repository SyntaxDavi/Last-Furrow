using UnityEngine;
using System.Linq;

[Cheat("detect_patterns", "Patterns", "Detecta e lista todos os padrões no grid.")]
public class DetectPatternsCheat : ICheatCommand
{
    public string Id => "detect_patterns";
    public string Category => "Patterns";
    public string Description => "Detecta e lista todos os padrões no grid.";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        var ctx = CheatContext.Instance;
        if (ctx.GridService == null || ctx.PatternDetector == null || ctx.PatternCalculator == null)
        {
            feedback = "❌ Sistema de padrões não disponível.";
            return false;
        }

        var matches = ctx.PatternDetector.DetectAll(ctx.GridService);
        string detail = string.Join("\n", matches.Select(m => $"• {m.DisplayName}: {m.BaseScore} pts"));
        int total = ctx.PatternCalculator.CalculateTotal(matches, ctx.GridService);

        feedback = $"✅ {matches.Count} padrões detectados. Total: {total} pts\n{detail}";
        return true;
    }
}

[Cheat("log_pattern_status", "Patterns", "Loga o status detalhado do tracking de padrões.")]
public class LogPatternStatusCheat : ICheatCommand
{
    public string Id => "log_pattern_status";
    public string Category => "Patterns";
    public string Description => "Loga o status detalhado do tracking de padrões.";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        var ctx = CheatContext.Instance;
        var run = ctx.SaveManager?.Data?.CurrentRun;
        if (run == null || ctx.PatternTracking == null)
        {
            feedback = "❌ Tracking não disponível.";
            return false;
        }

        Debug.Log($"--- PATTERN STATUS ---");
        Debug.Log($"Ativos: {ctx.PatternTracking.GetActivePatternsCount()}");
        foreach(var kvp in run.ActivePatterns)
        {
            Debug.Log($"• {kvp.Key}: Dia {kvp.Value.DaysActive}");
        }
        feedback = "✅ Status logado no console.";
        return true;
    }
}

[Cheat("reset_weekly", "Patterns", "Reseta o tracking semanal.")]
public class ResetWeeklyCheat : ICheatCommand
{
    public string Id => "reset_weekly";
    public string Category => "Patterns";
    public string Description => "Reseta o tracking semanal.";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        AppCore.Instance?.OnWeeklyReset();
        feedback = "✅ Tracking semanal resetado.";
        return true;
    }
}

[Cheat("simulate_end_day", "Patterns", "Simula a sequência de fim de dia.")]
public class SimulateEndDayCheat : ICheatCommand
{
    public string Id => "simulate_end_day";
    public string Category => "Patterns";
    public string Description => "Simula a sequência de fim de dia.";
    public bool ValidateArgs(string[] args, out string error) { error = null; return true; }

    public bool Execute(string[] args, out string feedback)
    {
        if (CheatContext.Instance.ResolutionSystem == null)
        {
            feedback = "❌ ResolutionSystem não disponível.";
            return false;
        }
        CheatContext.Instance.ResolutionSystem.StartEndDaySequence();
        feedback = "✅ Sequência de fim de dia iniciada.";
        return true;
    }
}
