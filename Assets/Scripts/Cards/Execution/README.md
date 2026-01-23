# Sistema de Execução de Cartas Robusto

## Arquitetura à Prova de Bugs

Este sistema implementa uma arquitetura robusta seguindo padrões enterprise para garantir:

### ✅ GARANTIAS

1. **Validação Prévia Obrigatória** - Toda carta é validada ANTES de executar
2. **Execução Atômica** - Tudo ou nada (não deixa estado inconsistente)
3. **Rollback Automático** - Reverte automaticamente em caso de falha
4. **Auditoria Completa** - Todas as ações são registradas para debug
5. **Type Safety** - Cada tipo de carta tem seu comando específico
6. **Idempotência** - Pode ser executado múltiplas vezes sem duplicar efeitos

### 📐 PADRÕES IMPLEMENTADOS

- **Command Pattern**: Cada execução é um comando imutável
- **Transaction Pattern**: Execução atômica com rollback
- **Factory Pattern**: Criação type-safe de comandos
- **Audit Pattern**: Registro completo de todas as ações
- **Null Object Pattern**: Tratamento seguro de tipos desconhecidos

### 🔧 COMO USAR

```csharp
// 1. Criar executor
var executor = new CardCommandExecutor(gridService, runData, audit);

// 2. Criar comando
var command = CardCommandFactory.CreateCommand(
    cardInstance,
    cardData,
    slotIndex,
    identityContext,
    runtimeContext
);

// 3. Executar (valida, executa, faz rollback se falhar)
var result = executor.ExecuteCommand(command);

if (result.IsSuccess)
{
    // Sucesso! Carta foi aplicada
    if (result.ShouldConsumeCard)
    {
        // Remover carta da mão
    }
}
else
{
    // Falha! Mostrar mensagem de erro
    Debug.LogError(result.Message);
}
```

### 🎯 BENEFÍCIOS

1. **Escalável**: Fácil adicionar novos tipos de cartas sem quebrar código existente
2. **Testável**: Cada comando pode ser testado isoladamente
3. **Debugável**: Auditoria completa de todas as ações
4. **Robusto**: Rollback automático previne estados inconsistentes
5. **Type-Safe**: Compilador detecta erros antes de executar

### 📝 ADICIONANDO NOVOS TIPOS DE CARTAS

1. Criar novo `XxxCardCommand : CardCommand`
2. Implementar `Validate()`, `Execute()`, `Rollback()`, `CreateSnapshot()`
3. Adicionar case no `CardCommandFactory.CreateCommand()`
4. Pronto! Sistema automaticamente usa o novo comando

### 🔍 DEBUG

Use `CardExecutionAudit.GetRecentEntries()` para ver últimas ações:

```csharp
var recentEntries = audit.GetRecentEntries(20);
foreach (var entry in recentEntries)
{
    Debug.Log($"{entry.Timestamp}: {entry.EventType} - {entry.Message}");
}
```
