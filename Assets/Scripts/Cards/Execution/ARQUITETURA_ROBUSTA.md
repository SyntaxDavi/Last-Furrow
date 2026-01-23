# Arquitetura Robusta para Sistema de Cartas

## 🎯 Objetivo

Criar um sistema **à prova de bugs** que seja:
- ✅ **Escalável**: Fácil adicionar novas cartas sem quebrar código existente
- ✅ **Robusto**: Validação prévia, execução atômica, rollback automático
- ✅ **Testável**: Cada comando pode ser testado isoladamente
- ✅ **Debugável**: Auditoria completa de todas as ações
- ✅ **Type-Safe**: Compilador detecta erros antes de executar

## 📐 Padrões Implementados

### 1. Command Pattern
Cada execução de carta é um **comando imutável** que encapsula:
- Dados da carta
- Slot alvo
- Timestamp
- ID único

**Benefício**: Pode ser validado, executado, revertido e auditado de forma isolada.

### 2. Transaction Pattern
Execução **atômica** (tudo ou nada):
- Valida ANTES de executar
- Cria snapshot do estado
- Executa
- Se falhar, faz rollback automático

**Benefício**: Nunca deixa estado inconsistente.

### 3. Factory Pattern
Criação **type-safe** de comandos:
- Cada tipo de carta tem seu comando específico
- Compilador detecta erros em compile-time
- Fácil adicionar novos tipos

**Benefício**: Impossível criar comando errado para tipo de carta.

### 4. Audit Pattern
Registro completo de todas as ações:
- Validações
- Execuções
- Falhas
- Rollbacks
- Exceções

**Benefício**: Debug completo quando algo der errado.

## 🔧 Estrutura de Arquivos

```
Cards/Execution/
├── CardCommand.cs              # Classe base abstrata
├── CardCommandExecutor.cs      # Executor com validação e rollback
├── CardCommandFactory.cs       # Factory type-safe
├── CardCommandAdapter.cs       # Adaptador para sistema antigo
├── CardExecutionAudit.cs       # Sistema de auditoria
├── README.md                   # Documentação de uso
├── ARQUITETURA_ROBUSTA.md      # Este arquivo
└── Commands/
    ├── HarvestCardCommand.cs
    ├── PlantCardCommand.cs
    ├── WaterCardCommand.cs
    ├── ClearCardCommand.cs
    └── ExpansionCardCommand.cs
```

## 🚀 Como Usar

### Opção 1: Sistema Novo (Recomendado)

```csharp
// 1. Criar executor
var executor = new CardCommandExecutor(gridService, runData, audit);

// 2. Criar comando via factory
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
```

### Opção 2: Adaptador (Compatibilidade)

```csharp
// Usa novo sistema internamente, mas retorna InteractionResult
var result = CardCommandAdapter.ExecuteCardWithCommand(
    cardInstance,
    cardData,
    slotIndex,
    gridService,
    runData,
    identityContext,
    runtimeContext
);
```

## ✅ Garantias do Sistema

### 1. Validação Prévia Obrigatória
Toda carta é validada ANTES de executar:
- Slot válido?
- Slot desbloqueado?
- Estado correto?
- Dados disponíveis?

**Resultado**: Nunca tenta executar ação inválida.

### 2. Execução Atômica
Tudo ou nada:
- Se qualquer parte falhar, tudo é revertido
- Estado nunca fica inconsistente

**Resultado**: Impossível corromper dados.

### 3. Rollback Automático
Em caso de falha:
- Reverte mudanças no grid
- Reverte dinheiro gasto
- Restaura estado anterior

**Resultado**: Sistema sempre volta a estado válido.

### 4. Auditoria Completa
Todas as ações são registradas:
- Quando validou
- Quando executou
- Quando falhou
- Quando reverteu

**Resultado**: Debug completo de qualquer problema.

## 📝 Adicionando Novos Tipos de Cartas

### Passo 1: Criar Comando

```csharp
public class NovaCartaCommand : CardCommand
{
    private readonly RunIdentityContext _context;

    public NovaCartaCommand(CardInstance instance, CardData data, int slotIndex, RunIdentityContext context)
        : base(instance, data, slotIndex)
    {
        _context = context;
    }

    public override ValidationResult Validate(IGridService gridService, RunData runData)
    {
        // Validações específicas desta carta
        if (!gridService.IsValidIndex(TargetSlotIndex))
            return ValidationResult.Fail("Slot inválido.");
        
        // Mais validações...
        return ValidationResult.Success();
    }

    public override CommandExecutionResult Execute(IGridService gridService, RunData runData)
    {
        // Lógica de execução
        var snapshot = CreateSnapshot(gridService, runData);
        // Fazer mudanças...
        return CommandExecutionResult.Success("Sucesso!", snapshot, consumeCard: true);
    }

    public override void Rollback(IGridService gridService, RunData runData, StateSnapshot snapshot)
    {
        // Reverter mudanças usando snapshot
    }

    protected override StateSnapshot CreateSnapshot(IGridService gridService, RunData runData)
    {
        // Criar snapshot do estado atual
    }
}
```

### Passo 2: Registrar na Factory

```csharp
// Em CardCommandFactory.CreateCommand()
case CardType.NovaCarta:
    return new NovaCartaCommand(instance, data, slotIndex, identityContext);
```

### Pronto! 🎉

O sistema automaticamente:
- Valida antes de executar
- Executa atomicamente
- Faz rollback se falhar
- Audita tudo

## 🔍 Debug

### Ver Últimas Ações

```csharp
var recentEntries = audit.GetRecentEntries(20);
foreach (var entry in recentEntries)
{
    Debug.Log($"{entry.Timestamp}: {entry.EventType} - {entry.Message}");
}
```

### Verificar Validações

Todas as validações falhadas são registradas automaticamente no audit.

## 🎯 Benefícios para Escalabilidade

1. **Adicionar 100 cartas novas**: Apenas criar 100 comandos, sem modificar código existente
2. **Mudar lógica de uma carta**: Apenas modificar um comando, sem afetar outros
3. **Testar isoladamente**: Cada comando pode ser testado sem dependências
4. **Debug rápido**: Auditoria mostra exatamente o que aconteceu
5. **Rollback seguro**: Nunca deixa estado inconsistente

## 🛡️ Proteções Implementadas

- ✅ Null checks em todos os pontos críticos
- ✅ Validação de índices antes de acessar arrays
- ✅ Validação de estado antes de executar
- ✅ Snapshot antes de modificar estado
- ✅ Rollback automático em caso de falha
- ✅ Auditoria completa para debug
- ✅ Type safety em compile-time
- ✅ Idempotência (pode executar múltiplas vezes)

## 📊 Comparação: Antes vs Depois

### Antes (Sistema Antigo)
- ❌ Validação e execução misturadas
- ❌ Sem rollback
- ❌ Difícil debug
- ❌ Estado pode ficar inconsistente
- ❌ Difícil adicionar novas cartas

### Depois (Sistema Novo)
- ✅ Validação separada e obrigatória
- ✅ Rollback automático
- ✅ Auditoria completa
- ✅ Estado sempre consistente
- ✅ Fácil adicionar novas cartas

## 🚦 Migração Gradual

O sistema antigo continua funcionando. Você pode:
1. Usar novo sistema para cartas novas
2. Migrar cartas antigas gradualmente
3. Usar adaptador para compatibilidade

**Sem breaking changes!** 🎉
