/// <summary>
/// Interface para factory de padrões.
/// 
/// FUNÇÃO:
/// - Abstrair criação de IGridPattern implementations
/// - Eliminar reflexão (type-safe)
/// - Permitir testes com mock factory
/// - Seguir Dependency Inversion Principle
/// </summary>
public interface IPatternFactory
{
    /// <summary>
    /// Cria uma instância de IGridPattern baseado na definição.
    /// </summary>
    /// <param name="definition">Configuração do padrão</param>
    /// <returns>Instância do padrão ou null se não encontrado</returns>
    IGridPattern CreatePattern(PatternDefinitionSO definition);
    
    /// <summary>
    /// Verifica se a factory pode criar o padrão especificado.
    /// </summary>
    bool CanCreate(string implementationClassName);
}
