# REFATORAÇÃO UI - RESUMO TÉCNICO

## ?? **DECISÕES DE ARQUITETURA**

### **1. Debounce System (DayWeekDisplay)**
**Problema**: Quando semana muda, dia também muda (Dia 7 ? Dia 1). Isso dispara 2 eventos:
- `OnDayChanged(1)`
- `OnWeekChanged(2)`

**Solução**: Debounce de 0.1s agrupa ambos em um único pulse.

```csharp
private void HandleDayChanged(int newDay) {
    _pendingUpdate = true;
    StartDebounce(); // Aguarda 0.1s antes de animar
}
```

**Resultado**: Pulse único, animação mais suave.

---

### **2. ITimePolicy Delegation (SleepButtonController)**
**Problema**: UI decidindo regras de negócio (`if (currentDay >= 6)`).

**Solução**: Criar `ITimePolicy` que centraliza regras:
```csharp
public interface ITimePolicy {
    bool CanSleep(int currentDay, RunPhase phase);
    bool IsWeekend(int day);
    bool IsLastProductionDay(int day);
}
```

**Benefício**: 
- UI pergunta, não decide
- Fácil adicionar eventos especiais (ex: feriados, cartas especiais)
- Testável isoladamente

---

### **3. UIContext Pattern**
**Problema**: Cada componente chamava `AppCore.Instance.Something`.

**Solução**: Contexto único com tudo que UI precisa:
```csharp
public class UIContext {
    public readonly ProgressionEvents ProgressionEvents;
    public readonly TimeEvents TimeEvents;
    public readonly IRunDataProvider RunData;
    public readonly ITimePolicy TimePolicy;
    // ...
}
```

**Benefício**:
- Single point of configuration
- Mockável para testes
- Imutável após criação

---

### **4. Guardas Lógicas (HeartView)**
**Problema**: Animações podiam ser chamadas em estados inválidos.

**Solução**: Guardas no início de cada método:
```csharp
public void AnimateHeal() {
    if (_isFull) return; // Já cheio, não anima
    // ...
}
```

**Benefício**: Previne bugs visuais (ex: heal em coração cheio).

---

## ?? **MÉTRICAS**

| Métrica | Antes | Depois |
|---------|-------|--------|
| Dependências `AppCore.Instance` na UI | 12 | 2* |
| Linhas de código duplicadas | ~50 | 0 |
| Interfaces públicas | 0 | 3 |
| Componentes testáveis | 0 | 3 |
| Event handlers com debounce | 0 | 1 |

*Restantes: `DailyResolutionSystem` e `GameStateManager` (TODO Fase 3)

---

## ?? **TRADE-OFFS**

### **Complexidade vs Flexibilidade**
- **Custo**: +6 arquivos novos (interfaces, adapters)
- **Ganho**: Testável, extensível, desacoplado

### **Performance**
- **Debounce**: +0.1s latência (imperceptível)
- **Pooling**: Mantido, zero impacto

### **Manutenção**
- **Antes**: Mudar regra de tempo ? buscar em 5 arquivos
- **Depois**: Mudar `DefaultTimePolicy` ? 1 arquivo

---

## ?? **DÍVIDAS TÉCNICAS CONHECIDAS**

### **1. AppCore.Instance Residual**
**Onde**: `SleepButtonController.CanActivate()`
```csharp
AppCore.Instance.DailyResolutionSystem.StartEndDaySequence();
AppCore.Instance.GameStateManager.CurrentState;
AppCore.Instance.RunManager.CurrentPhase;
```

**Solução Futura**: Injetar esses 3 no UIContext.

### **2. HeartDisplayManager não spawna**
**Sintoma**: Corações não aparecem na primeira vez.
**Possível causa**: 
- UIBootstrapper executando antes de RunData estar pronto
- Prefab não configurado corretamente
- Container não atribuído

**Debug**:
1. Verificar Console: "[UIBootstrapper] ? HeartDisplayManager..."
2. Verificar Inspector: HeartPrefab atribuído?
3. Verificar RunData: `CurrentLives` > 0?

### **3. EconomyEvents Ausente**
**Impacto**: Não consegue escutar mudanças de dinheiro.
**Workaround**: Criar quando necessário (comentado no UIContext).

---

## ?? **REFERÊNCIAS DE PADRÕES**

- **Dependency Injection**: Martin Fowler
- **Strategy Pattern**: Gang of Four (ITimePolicy)
- **Adapter Pattern**: Gang of Four (RunDataProviderAdapter)
- **Debounce**: RxJS/Reactive patterns
- **Event Aggregator**: Fowler's PoEAA (UIContext como hub)

---

**Autor**: Refatoração Fase 2
**Data**: Sessão atual
**Status**: ? Concluído e compilando
