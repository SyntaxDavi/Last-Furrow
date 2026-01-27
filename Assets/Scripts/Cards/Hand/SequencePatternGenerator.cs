using System.Collections.Generic;

/// <summary>
/// Gera ordens de sequência para propagação de efeitos visuais em listas.
/// Usado para animações de cartas, menus, inventários, etc.
/// Padrão Strategy: permite trocar algoritmos de sequência sem modificar o consumidor.
/// </summary>
public static class SequencePatternGenerator
{
    /// <summary>
    /// Padrões de propagação disponíveis.
    /// </summary>
    public enum SweepPattern
    {
        LeftToRight,    // 0 1 2 3 4 5
        RightToLeft,    // 5 4 3 2 1 0
        CenterOut,      // 2 3 1 4 0 5 (centro expande para fora)
        OutsideIn       // 0 5 1 4 2 3 (bordas fecham para centro)
    }
    
    /// <summary>
    /// Preenche a lista com a ordem de índices baseada no padrão e quantidade de itens.
    /// Padrão Zero-Allocation: utiliza uma lista existente para evitar GC.
    /// </summary>
    /// <param name="order">Lista a ser preenchida (será limpa no início)</param>
    /// <param name="count">Número total de itens</param>
    /// <param name="pattern">Padrão de propagação desejado</param>
    public static void FillOrder(List<int> order, int count, SweepPattern pattern)
    {
        if (order == null) return;
        order.Clear();
        if (count <= 0) return;
        
        switch (pattern)
        {
            case SweepPattern.LeftToRight: 
                GenerateLeftToRight(order, count); break;
            case SweepPattern.RightToLeft: 
                GenerateRightToLeft(order, count); break;
            case SweepPattern.CenterOut: 
                GenerateCenterOut(order, count); break;
            case SweepPattern.OutsideIn: 
                GenerateOutsideIn(order, count); break;
            default:
                GenerateLeftToRight(order, count); break;
        }
    }
    
    private static void GenerateLeftToRight(List<int> order, int count)
    {
        for (int i = 0; i < count; i++) order.Add(i);
    }
    
    private static void GenerateRightToLeft(List<int> order, int count)
    {
        for (int i = count - 1; i >= 0; i--) order.Add(i);
    }
    
    private static void GenerateCenterOut(List<int> order, int count)
    {
        int center = count / 2;
        if (count % 2 == 1) order.Add(center);
        
        int left = center - (count % 2 == 1 ? 1 : 1);
        int right = center + (count % 2 == 1 ? 1 : 0);
        
        while (order.Count < count)
        {
            if (right < count) { order.Add(right); right++; }
            if (left >= 0 && order.Count < count) { order.Add(left); left--; }
        }
    }
    
    private static void GenerateOutsideIn(List<int> order, int count)
    {
        int left = 0;
        int right = count - 1;
        while (left <= right)
        {
            order.Add(left);
            if (left != right) order.Add(right);
            left++;
            right--;
        }
    }
}
