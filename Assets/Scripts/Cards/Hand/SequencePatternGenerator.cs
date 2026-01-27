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
    /// Gera a ordem de índices baseada no padrão e quantidade de itens.
    /// </summary>
    /// <param name="count">Número total de itens</param>
    /// <param name="pattern">Padrão de propagação desejado</param>
    /// <returns>Lista de índices na ordem de execução</returns>
    public static List<int> GetOrder(int count, SweepPattern pattern)
    {
        if (count <= 0) return new List<int>();
        
        return pattern switch
        {
            SweepPattern.LeftToRight => GenerateLeftToRight(count),
            SweepPattern.RightToLeft => GenerateRightToLeft(count),
            SweepPattern.CenterOut => GenerateCenterOut(count),
            SweepPattern.OutsideIn => GenerateOutsideIn(count),
            _ => GenerateLeftToRight(count)
        };
    }
    
    /// <summary>
    /// Esquerda → Direita: 0 1 2 3 4 5
    /// </summary>
    private static List<int> GenerateLeftToRight(int count)
    {
        var order = new List<int>(count);
        for (int i = 0; i < count; i++)
        {
            order.Add(i);
        }
        return order;
    }
    
    /// <summary>
    /// Direita → Esquerda: 5 4 3 2 1 0
    /// </summary>
    private static List<int> GenerateRightToLeft(int count)
    {
        var order = new List<int>(count);
        for (int i = count - 1; i >= 0; i--)
        {
            order.Add(i);
        }
        return order;
    }
    
    /// <summary>
    /// Centro → Bordas: Para 6 cartas = 2 3 1 4 0 5
    /// Começa no centro e expande alternadamente para os lados.
    /// </summary>
    private static List<int> GenerateCenterOut(int count)
    {
        var order = new List<int>(count);
        
        int center = count / 2;
        
        // Se ímpar, começa exatamente no centro
        // Se par, começa no elemento à esquerda do centro
        if (count % 2 == 1)
        {
            order.Add(center);
        }
        
        int offset = count % 2 == 1 ? 1 : 0;
        int left = center - (count % 2 == 1 ? 1 : 1);
        int right = center + (count % 2 == 1 ? 1 : 0);
        
        while (order.Count < count)
        {
            if (right < count)
            {
                order.Add(right);
                right++;
            }
            if (left >= 0 && order.Count < count)
            {
                order.Add(left);
                left--;
            }
        }
        
        return order;
    }
    
    /// <summary>
    /// Bordas → Centro: Para 6 cartas = 0 5 1 4 2 3
    /// Começa nas bordas e fecha alternadamente para o centro.
    /// </summary>
    private static List<int> GenerateOutsideIn(int count)
    {
        var order = new List<int>(count);
        
        int left = 0;
        int right = count - 1;
        
        while (left <= right)
        {
            order.Add(left);
            if (left != right) // Evita adicionar o mesmo índice duas vezes (quando ímpar)
            {
                order.Add(right);
            }
            left++;
            right--;
        }
        
        return order;
    }
}
