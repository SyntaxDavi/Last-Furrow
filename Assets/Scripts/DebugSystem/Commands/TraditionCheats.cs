using System.Collections.Generic;
using LastFurrow.Traditions;

/// <summary>
/// Cheats para testar o sistema de tradições.
/// </summary>
public static class TraditionCheats
{
    [CheatCommand("tradition.add", "Adiciona uma tradição aleatória")]
    public static bool AddRandomTradition(CheatContext ctx, out string feedback)
    {
        var manager = UnityEngine.Object.FindFirstObjectByType<TraditionManager>();
        if (manager == null)
        {
            feedback = "❌ TraditionManager não encontrado na scene";
            return false;
        }
        
        if (!manager.CanAddTradition)
        {
            feedback = $"❌ Máximo de tradições atingido ({manager.MaxTraditions})";
            return false;
        }
        
        var traditions = ctx.Library.GetRandomTraditions(1);
        if (traditions.Count == 0)
        {
            feedback = "❌ Nenhuma tradição disponível no GameDatabase";
            return false;
        }
        
        if (manager.TryAddTradition(traditions[0]))
        {
            feedback = $"✅ Tradição adicionada: {traditions[0].DisplayName}";
            return true;
        }
        
        feedback = "❌ Falha ao adicionar tradição";
        return false;
    }
    
    [CheatCommand("tradition.add.id", "Adiciona tradição por ID", "id")]
    public static bool AddTraditionByID(CheatContext ctx, string id, out string feedback)
    {
        var manager = UnityEngine.Object.FindFirstObjectByType<TraditionManager>();
        if (manager == null)
        {
            feedback = "❌ TraditionManager não encontrado na scene";
            return false;
        }
        
        if (!ctx.Library.TryGetTradition(id, out var data))
        {
            feedback = $"❌ Tradição não encontrada: {id}";
            return false;
        }
        
        if (manager.TryAddTradition(data))
        {
            feedback = $"✅ Tradição adicionada: {data.DisplayName}";
            return true;
        }
        
        feedback = "❌ Falha ao adicionar tradição (máximo atingido?)";
        return false;
    }
    
    [CheatCommand("tradition.list", "Lista todas as tradições ativas")]
    public static bool ListTraditions(CheatContext ctx, out string feedback)
    {
        var manager = UnityEngine.Object.FindFirstObjectByType<TraditionManager>();
        if (manager == null)
        {
            feedback = "❌ TraditionManager não encontrado";
            return false;
        }
        
        var traditions = manager.ActiveTraditions;
        if (traditions.Count == 0)
        {
            feedback = "📋 Nenhuma tradição ativa";
            return true;
        }
        
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"📋 Tradições Ativas ({traditions.Count}/{manager.MaxTraditions}):");
        
        for (int i = 0; i < traditions.Count; i++)
        {
            var t = traditions[i];
            var name = t.Data?.DisplayName ?? t.TraditionID;
            sb.AppendLine($"  [{i}] {name}");
        }
        
        feedback = sb.ToString();
        return true;
    }
    
    [CheatCommand("tradition.swap", "Troca duas tradições de posição", "indexA", "indexB")]
    public static bool SwapTraditions(CheatContext ctx, int indexA, int indexB, out string feedback)
    {
        var manager = UnityEngine.Object.FindFirstObjectByType<TraditionManager>();
        if (manager == null)
        {
            feedback = "❌ TraditionManager não encontrado";
            return false;
        }
        
        if (indexA < 0 || indexA >= manager.ActiveCount || indexB < 0 || indexB >= manager.ActiveCount)
        {
            feedback = $"❌ Índices inválidos (0-{manager.ActiveCount - 1})";
            return false;
        }
        
        manager.SwapTraditions(indexA, indexB);
        feedback = $"✅ Tradições {indexA} e {indexB} trocadas";
        return true;
    }
    
    [CheatCommand("tradition.remove", "Remove tradição por índice", "index")]
    public static bool RemoveTradition(CheatContext ctx, int index, out string feedback)
    {
        var manager = UnityEngine.Object.FindFirstObjectByType<TraditionManager>();
        if (manager == null)
        {
            feedback = "❌ TraditionManager não encontrado";
            return false;
        }
        
        if (index < 0 || index >= manager.ActiveCount)
        {
            feedback = $"❌ Índice inválido (0-{manager.ActiveCount - 1})";
            return false;
        }
        
        var tradition = manager.ActiveTraditions[index];
        var name = tradition.Data?.DisplayName ?? tradition.TraditionID;
        
        if (manager.TrySellTradition(index, out int sellValue))
        {
            feedback = $"✅ Tradição removida: {name} (valor: ${sellValue})";
            return true;
        }
        
        feedback = "❌ Falha ao remover tradição";
        return false;
    }
    
    [CheatCommand("tradition.slots", "Aumenta slots de tradições", "amount")]
    public static bool AddTraditionSlots(CheatContext ctx, int amount, out string feedback)
    {
        if (ctx.RunData == null)
        {
            feedback = "❌ RunData não disponível";
            return false;
        }
        
        int oldMax = ctx.RunData.MaxTraditionSlots;
        ctx.RunData.MaxTraditionSlots += amount;
        
        feedback = $"✅ Slots de tradições: {oldMax} → {ctx.RunData.MaxTraditionSlots}";
        return true;
    }
    
    [CheatCommand("tradition.available", "Lista tradições disponíveis no database")]
    public static bool ListAvailableTraditions(CheatContext ctx, out string feedback)
    {
        var allTraditions = ctx.Library.GetAllTraditions();
        var list = new List<TraditionData>();
        foreach (var t in allTraditions) list.Add(t);
        
        if (list.Count == 0)
        {
            feedback = "📋 Nenhuma tradição no GameDatabase";
            return true;
        }
        
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"📋 Tradições Disponíveis ({list.Count}):");
        
        foreach (var t in list)
        {
            sb.AppendLine($"  • {t.ID}: {t.DisplayName} ({t.Rarity})");
        }
        
        feedback = sb.ToString();
        return true;
    }
}
