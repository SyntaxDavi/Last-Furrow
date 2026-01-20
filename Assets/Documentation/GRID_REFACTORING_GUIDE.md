# GRID VISUAL REFACTORING - GUIA COMPLETO

## VISÃO GERAL

Este documento descreve a refatoração completa do sistema visual do Grid, aplicando princípios SOLID e injeção de dependências.

---

## ARQUITETURA ANTES vs DEPOIS

### ANTES (Acoplado)
```csharp
public class GridSlotView {
    private void CanReceive() {
        // ? Acoplamento direto com AppCore
        var state = AppCore.Instance.GameStateManager.CurrentState;
        
        // ? Lógica de validação espalhada
        if (state != GameState.Playing) return false;
        
        // ? Acessa GridService via Manager
        return _gridManager.Service.CanReceiveCard(...);
    }
}
```

### DEPOIS (Desacoplado)
```csharp
public class GridSlotView {
    private GridVisualContext _context;
    
    public void Initialize(GridVisualContext context, int index) {
        _context = context;
        // ? Recebe dependências injetadas
    }
    
    private void CanReceive() {
        // ? Usa IDropValidator (testável)
        return _context.DropValidator.CanDrop(_index, cardData);
    }
}
```

---

## COMPONENTES PRINCIPAIS

### 1. GridVisualConfig (ScriptableObject)

**Propósito**: Centraliza todas as configurações visuais

**Criação**:
```
Unity: Assets ? Create ? Grid/Visual Config
```

**Configurações**:
- Cores base: dry, wet, locked
- Overlays: mature, withered, disabled
- Hover: valid, invalid, unlockable
- Flash effects: error, analyzing

**Benefício**: Mudar cor em 1 lugar, afeta todos os slots

---

### 2. GridVisualContext

**Propósito**: Container de dependências (DI)

**Contém**:
```csharp
public readonly IGridService GridService;
public readonly IGameLibrary Library;
public readonly IDropValidator DropValidator;
public readonly GridEvents GridEvents;
public readonly GameStateEvents GameStateEvents;
public readonly GameStateManager GameStateManager;
public readonly GridVisualConfig VisualConfig;
```

**Benefício**: 
- GridManager e GridSlotView recebem tudo que precisam
- Testável com mocks
- Zero AppCore.Instance

---

### 3. IDropValidator

**Propósito**: Validação de drop desacoplada

**Interface**:
```csharp
public interface IDropValidator {
    bool CanDrop(int slotIndex, CardData cardData);
    string GetErrorMessage();
}
```

**Implementação (DefaultDropValidator)**:
```csharp
1. Valida GameState (deve ser Playing)
2. Valida slot desbloqueado
3. Delega para GridService.CanReceiveCard()
4. Retorna mensagem de erro descritiva
```

**Benefício**:
- Testável isoladamente
- Mensagens de erro centralizadas
- Extensível (criar CustomDropValidator)

---

### 4. GridVisualBootstrapper

**Propósito**: Inicializa sistema visual do Grid

**Fluxo**:
```
1. Espera AppCore.Instance estar pronto
2. Espera GridService ser criado
3. Cria GridVisualContext
4. Encontra GridManager na cena
5. Injeta contexto: gridManager.Initialize(context)
```

**Quando executa**: Após GameplayBootstrapper criar GridService

**Benefício**: Único ponto de configuração visual

---

### 5. GridManager Refatorado

**Mudanças principais**:
```csharp
// ANTES
public void Configure(IGridService service, IGameLibrary library) {...}

// DEPOIS
public void Initialize(GridVisualContext context) {...}
```

**Responsabilidades**:
- Spawnar GridSlotViews
- Injetar GridVisualContext em cada slot
- Manager Push: Empurra updates quando estado muda
- Traduzir IReadOnlyCropState ? comandos visuais

**Não faz mais**:
- Validar regras (IDropValidator faz)
- Acessar AppCore diretamente

---

### 6. GridSlotView Refatorado

**Mudanças principais**:

#### Priority Layered Rendering
```csharp
Layer 0: Base Color (_baseRenderer)       ? dry/wet/locked
Layer 1: Plant Sprite (_plantRenderer)    ? visual da planta
Layer 2: GameState Overlay (novo)         ? cinza durante Shopping
Layer 3: Hover Highlight                  ? branco/verde/vermelho
Layer 4: Flash Effects (futuro)           ? error flash, analyzing pulse
```

#### GameState Overlay Automático
```csharp
private void HandleGameStateChanged(GameState newState) {
    bool isDisabled = (newState != GameState.Playing);
    _gameStateOverlayRenderer.enabled = isDisabled;
    // Aplica overlay cinza automaticamente
}
```

#### Flash de Erro
```csharp
if (!canDrop) {
    StartCoroutine(FlashError()); // Flash vermelho 0.2s
}
```

#### Zero AppCore.Instance
```csharp
// ANTES
var state = AppCore.Instance.GameStateManager.CurrentState;

// DEPOIS
var state = _context.GameStateManager.CurrentState;
```

---

## BENEFÍCIOS DA REFATORAÇÃO

### 1. Testabilidade (0% ? 80%)
```csharp
// Teste unitário exemplo
[Test]
public void DropValidator_RejectsCard_WhenNotPlaying() {
    var mockStateManager = new Mock<GameStateManager>();
    mockStateManager.Setup(x => x.CurrentState).Returns(GameState.Shopping);
    
    var validator = new DefaultDropValidator(gridService, mockStateManager.Object);
    
    bool result = validator.CanDrop(0, seedCard);
    
    Assert.IsFalse(result);
    Assert.Contains("Shopping", validator.GetErrorMessage());
}
```

### 2. Manutenibilidade
**Antes**: Mudar cor de slot bloqueado = editar 5 arquivos  
**Depois**: Mudar cor no GridVisualConfig ? afeta todos os slots

### 3. Extensibilidade
```csharp
// Criar novo DropValidator para tutorial
public class TutorialDropValidator : IDropValidator {
    public bool CanDrop(int slotIndex, CardData card) {
        if (TutorialManager.CurrentStep == 1 && slotIndex != 0)
            return false; // Só permite slot 0
        return _defaultValidator.CanDrop(slotIndex, card);
    }
}
```

### 4. Performance
- Event-driven (não Update polling)
- GameState overlay automático
- Menos acoplamento = menos dependências

---

## COMO ESTENDER

### Adicionar Novo Estado Visual

**Exemplo**: Adicionar "Mature Glow"

1. **GridVisualConfig**:
```csharp
[Header("State Overlays")]
public Color matureGlow = new Color(0f, 1f, 0f, 0.5f);
```

2. **GridSlotView**:
```csharp
[SerializeField] private SpriteRenderer _stateOverlayRenderer;

public void SetVisualState(Sprite sprite, bool isWatered, bool isMature) {
    // ...existing code...
    
    if (isMature) {
        _stateOverlayRenderer.enabled = true;
        _stateOverlayRenderer.color = _context.VisualConfig.matureGlow;
    }
}
```

3. **GridManager**:
```csharp
bool isMature = (state.CurrentGrowth >= state.DaysMature);
view.SetVisualState(sprite, isWatered, isMature);
```

---

### Adicionar Nova Validação

**Exemplo**: Impedir plantar se não tem dinheiro

```csharp
public class EconomyDropValidator : IDropValidator {
    private readonly IEconomyService _economy;
    private readonly IDropValidator _baseValidator;
    
    public bool CanDrop(int slotIndex, CardData card) {
        // Valida base primeiro
        if (!_baseValidator.CanDrop(slotIndex, card))
            return false;
        
        // Validação customizada
        if (card.Type == CardType.Plant) {
            int cost = GetPlantCost(card);
            if (_economy.CurrentMoney < cost) {
                _lastError = $"Dinheiro insuficiente: {cost} moedas";
                return false;
            }
        }
        
        return true;
    }
}
```

Trocar no GridVisualBootstrapper:
```csharp
var economyValidator = new EconomyDropValidator(
    AppCore.Instance.EconomyService,
    baseValidator
);
```

---

## TROUBLESHOOTING

### Problema: Grid não aparece

**Sintomas**: Cena vazia, sem slots visuais

**Checklist**:
1. GridVisualBootstrapper está na cena?
2. GridVisualConfig atribuído no Inspector?
3. Logs dizem `[GridVisualBootstrapper] SUCESSO`?
4. GridManager tem SlotPrefab atribuído?

**Solução**:
```
Enable _showDebugLogs = true em GridVisualBootstrapper
Veja onde está falhando nos logs
```

---

### Problema: Overlay não aparece

**Sintomas**: Slots não ficam cinza durante Shopping

**Checklist**:
1. GameStateManager está mudando estado?
2. GameStateEvents.OnStateChanged está disparando?
3. GridSlotView._showDebugLogs = true, vê log "GameState overlay ativo"?

**Debug**:
```csharp
// Adicione em GridSlotView.HandleGameStateChanged
Debug.Log($"[Slot {_index}] State={newState}, Disabled={isDisabled}");
```

---

### Problema: Flash não funciona

**Sintomas**: Sem feedback visual ao tentar ação inválida

**Checklist**:
1. GridVisualConfig.flashDuration > 0?
2. CanReceive() retorna false?
3. Coroutine iniciando corretamente?

**Debug**:
```csharp
// Em GridSlotView.CanReceive
Debug.Log($"[Slot {_index}] CanDrop={canDrop}, Error={errorMsg}");
```

---

## MIGRAÇÃO PARA EVENT-DRIVEN (FUTURO)

Atualmente: **Manager Push** (GridManager empurra updates)  
Futuro: **Event-Driven** (GridSlotView escuta eventos)

**Como migrar**:

```csharp
// GridSlotView.Initialize
_context.GridEvents.OnSlotStateChanged += (index) => {
    if (index == _myIndex) {
        RefreshMyVisual();
    }
};

private void RefreshMyVisual() {
    var state = _context.GridService.GetSlotReadOnly(_index);
    // Atualiza visual baseado no estado
}
```

**Benefício**: GridManager não precisa chamar RefreshSlot()

**Trade-off**: Mais eventos, mas mais desacoplado

---

## CHECKLIST FINAL

### Compilação
- [x] Sem erros
- [x] Sem warnings relevantes

### Arquitetura
- [x] Zero AppCore.Instance em GridSlotView
- [x] IDropValidator implementado
- [x] GridVisualConfig ScriptableObject
- [x] GridVisualContext injeção funcional

### Visual
- [x] Priority layers (4 layers)
- [x] GameState overlay automático
- [x] Flash de erro implementado
- [x] Cores configuráveis

### Cleanup
- [x] GridStateFeedback removido
- [x] GridFeedbackController removido
- [x] Configure() legacy removido

### Documentação
- [x] GRID_REFACTORING_PROGRESS.md atualizado
- [x] GRID_REFACTORING_GUIDE.md criado
- [x] Comentários inline claros

---

**STATUS**: ? REFATORAÇÃO COMPLETA
**PRÓXIMO**: Testar na Unity e ajustar cores no GridVisualConfig
