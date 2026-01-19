using System;

/// <summary>
/// Interface para validação de drop de cartas em slots do grid.
/// 
/// RESPONSABILIDADE:
/// - Determinar se uma carta pode ser usada em um slot específico
/// - Fornecer mensagens de erro descritivas para feedback ao usuário
/// - Encapsular todas as regras de validação (GameState, slot locked, etc)
/// 
/// SOLID:
/// - Interface Segregation: Apenas métodos de validação
/// - Dependency Inversion: GridSlotView depende da abstração, não da implementação
/// 
/// BENEFÍCIOS:
/// - Testável: Mock para unit tests
/// - Extensível: Trocar implementação sem quebrar código existente
/// - Centralizado: Todas as regras em um só lugar
/// 
/// USO:
/// <code>
/// if (!validator.CanDrop(slotIndex, cardData)) {
///     string error = validator.GetErrorMessage();
///     ShowError(error);
/// }
/// </code>
/// </summary>
public interface IDropValidator
{
    /// <summary>
    /// Valida se uma carta pode ser usada no slot especificado.
    /// </summary>
    /// <param name="slotIndex">Índice do slot no grid</param>
    /// <param name="cardData">Dados da carta sendo arrastada</param>
    /// <returns>true se a ação é válida, false caso contrário</returns>
    bool CanDrop(int slotIndex, CardData cardData);

    /// <summary>
    /// Obtém a mensagem de erro da última validação que falhou.
    /// Deve ser chamado imediatamente após CanDrop() retornar false.
    /// </summary>
    /// <returns>Mensagem descritiva do motivo da falha</returns>
    string GetErrorMessage();
}
