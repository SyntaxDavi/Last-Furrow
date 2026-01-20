# ?? PATTERN VISUAL JUICE - BUGS & FIXES TRACKING

**Data:** 2026-01-20  
**Branch:** feature/pattern-visual-juice  
**Status:** ?? Em correção

---

## ?? BUGS IDENTIFICADOS

### ? **BUG #1: Grid Inteiro Levita (Incorreto)**
**Problema:** Quando detecta padrões, o grid INTEIRO sobe junto (todos os slots)  
**Esperado:** Cada SLOT individual deve levitar ao ser analisado  
**Causa:** `LevitateGridRoutine()` em `PatternHighlightController` anima o Transform do GridManager  
**Solução:** Remover levitação do grid, implementar `AnalyzingPhaseController` para animar slots individuais

---

### ? **BUG #2: Analyzing Pulse Muito Rápido**
**Problema:** Passa por todos os 25 slots muito rápido  
**Esperado:** 0.2s por slot, apenas slots COM PLANTA  
**Causa:** Não existe analyzing phase atualmente  
**Solução:** Criar `AnalyzingPhaseController` que:
- Passa apenas por slots com planta
- 0.2s por slot (variável ajustável)
- Levita cada slot individualmente (altura 0.1)

---

### ? **BUG #3: Highlight Pulse Muito Rápido**
**Problema:** Pulse dos padrões muito rápido, difícil de ver  
**Esperado:** Pulse mais lento, duração ajustável  
**Causa:** `pulseSpeed` muito alto, sem controle fino  
**Solução:** Adicionar variáveis:
- `highlightDuration` (tempo total)
- `highlightPulseSpeed` (velocidade do pulse)
- `highlightDelayBetween` (delay entre padrões)

---

### ? **BUG #4: Error Flash Não Aparece**
**Problema:** Ao dropar carta inválida ou expansion em slot bloqueado, não fica vermelho  
**Esperado:** Flash vermelho aparece  
**Causa:** Possível problema de sorting order ou lógica de validação  
**Investigar:**
- `GridSlotView.FlashError()`
- `GridVisualConfig.errorFlash` (cor e alpha)
- Sorting order de `_highlightRenderer`

---

### ? **BUG #5: State Overlays Não Aparecem**
**Problema:** Mature/Withered overlays não aparecem nas plantas  
**Esperado:** Slots com plantas maduras/murchas mostram overlay colorido  
**Causa:** Possível problema de:
- Sorting order
- Alpha muito baixo
- Lógica de `UpdateVisuals()` não chamada
**Investigar:**
- `GridSlotView.UpdateVisuals()`
- `GridVisualConfig` mature/withered colors
- Event subscriptions

---

## ? REQUISITOS CLARIFICADOS

### **FLOW DESEJADO:**
```
Sleep Button
    ?
1. ANALYZING PHASE (0.2s/slot)
   - Passa apenas por slots COM PLANTA
   - Cada slot levita 0.1 altura individualmente
   - Rosa pulse atual (sem branco adicional)
   - Total: ~2s para 10 plantas
    ?
2. SYNERGY VISUAL (se 2+ padrões)
   - Explosion + Aura (em paralelo com analyzing)
    ?
3. HIGHLIGHTING PHASE
   - Slots dos padrões pulsam com cor de Tier
   - Duração ajustável
   - Delay entre padrões ajustável
    ?
4. FARMER DIARY (futuro)
```

### **VARIÁVEIS REQUERIDAS:**
```csharp
[Header("Analyzing Phase")]
float analyzingDurationPerSlot = 0.2f;      // Tempo por slot
float analyzingLevitationHeight = 0.1f;     // Altura levitação
bool analyzingOnlyPlants = true;            // Só slots com planta

[Header("Highlight Phase")]
float highlightDuration = 1.0f;             // Duração total do pulse
float highlightPulseSpeed = 1.0f;           // Velocidade do pulse
float highlightDelayBetween = 0.3f;         // Delay entre padrões
float highlightFadeOut = 0.3f;              // Fade out final

[Header("Synergy Timing")]
bool synergyDuringAnalyzing = true;         // Sinergia em paralelo?
```

---

## ?? PLANO DE CORREÇÃO

### **FASE 1: Remover Bugs Críticos**
1. ? Remover `LevitateGridRoutine()` de `PatternHighlightController`
2. ? Criar `AnalyzingPhaseController.cs`
3. ? Integrar analyzing phase no flow

### **FASE 2: Ajustar Timing**
4. ? Adicionar variáveis de controle no `PatternVisualConfig`
5. ? Atualizar `PatternHighlightController` para usar novas variáveis

### **FASE 3: Investigar Overlays**
6. ?? Debugar `FlashError()` (error flash)
7. ?? Debugar state overlays (mature/withered)
8. ?? Verificar sorting orders

---

## ?? NOTAS

- Todas as variáveis devem ser **bem documentadas** com tooltips
- Valores iniciais **baixos** (usuário ajusta no Inspector)
- Código **flexível** para mudar flow no futuro (portas abertas)
- **Debug logs** habilitáveis via `PatternVisualConfig.debugMode`

---

**Última Atualização:** 2026-01-20 23:45  
**Próximo:** Implementar AnalyzingPhaseController
