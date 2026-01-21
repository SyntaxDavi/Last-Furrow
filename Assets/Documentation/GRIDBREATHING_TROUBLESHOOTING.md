# ?? GRIDBREATHING TROUBLESHOOTING GUIDE

**Problema:** GridBreathing não está reagindo aos patterns detectados  
**Status:** Investigação com logs de debug adicionados

---

## ?? **DIAGNÓSTICO RÁPIDO**

### **Sintoma:**
Grid não "respira" quando patterns são detectados durante o Sleep.

### **Causas Possíveis (em ordem de probabilidade):**

1. ? **Controller não está na cena** (95% dos casos)
2. ?? **AppCore não está pronto no Start()** (ordem de inicialização)
3. ?? **Config está null ou com valores zerados**
4. ?? **Evento não está sendo disparado**

---

## ??? **SOLUÇÕES**

### **Solução 1: Adicionar Controller à Cena** ? (Mais Provável)

#### **Problema:**
O `GridBreathingController` existe como script, mas não está attachado a nenhum GameObject na cena.

#### **Como Verificar:**
1. Abrir cena do jogo (Game.unity ou similar)
2. Procurar por GameObject com componente `GridBreathingController`
3. Se não existir ? esse é o problema!

#### **Como Corrigir:**

**Opção A: Adicionar ao GridManager existente**
```
Hierarchy:
  ??? GridManager (GameObject)
      ??? Add Component ? GridBreathingController
```

**Opção B: Criar GameObject dedicado**
```
1. Hierarchy ? Create Empty ? Nome: "GridBreathingController"
2. Add Component ? GridBreathingController
3. Inspector:
   - Config: deixar vazio (vai carregar do Resources)
   - Grid Transform: arrastar o GridManager
```

#### **Configuração Recomendada:**
```
GridBreathingController Inspector:
??? Config: (None) ? carrega automaticamente
??? Grid Transform: GridManager Transform
    ??? Breathing Curve: EaseInOut (0,0) ? (1,1)
```

---

### **Solução 2: Mudar Start() para OnEnable()**

#### **Problema:**
Se o GridBreathing executa `Start()` antes do AppCore estar pronto, a subscrição do evento falha.

#### **Como Verificar:**
Procurar no Console:
```
[GridBreathing] ? Failed to subscribe: AppCore.Instance or Events.Pattern is null!
```

#### **Como Corrigir:**
Arquivo: `GridBreathingController.cs`

```csharp
// ANTES:
private void Start()
{
    Debug.Log("[GridBreathing] Starting breathing animation...");
    StartBreathing();
    SubscribeToEvents();
}

// DEPOIS:
private void OnEnable()
{
    Debug.Log("[GridBreathing] OnEnable called");
    StartCoroutine(DelayedInitialize());
}

private IEnumerator DelayedInitialize()
{
    // Aguardar 1 frame para AppCore estar pronto
    yield return null;
    
    StartBreathing();
    SubscribeToEvents();
}
```

---

### **Solução 3: Verificar PatternVisualConfig**

#### **Problema:**
Config não está em `Resources/Patterns/` ou tem valores zerados.

#### **Como Verificar:**
1. Project Window ? `Assets/Resources/Patterns/`
2. Verificar se existe `PatternVisualConfig.asset`
3. Abrir no Inspector e verificar:
   - `breathingSpeed`: deve ser > 0 (ex: 0.5)
   - `breathingAmount`: deve ser > 0 (ex: 0.05)
   - `patternReactionStrength`: deve ser > 0 (ex: 0.2)
   - `reactionDuration`: deve ser > 0 (ex: 0.5)

#### **Como Corrigir:**
Se não existe:
```
1. Assets/Resources ? Create Folder ? "Patterns"
2. Patterns ? Create ? Patterns ? Pattern Visual Config
3. Configurar valores:
   - breathingSpeed = 0.5
   - breathingAmount = 0.05
   - patternReactionStrength = 0.2
   - reactionDuration = 0.5
```

---

### **Solução 4: Verificar se Evento é Disparado**

#### **Problema:**
`AnalyzingPhaseController` não está disparando o evento.

#### **Como Verificar:**
Procurar no Console após Sleep:
```
[AnalyzingPhase] Breathing event dispatched: X patterns, Y points
```

Se **NÃO** aparecer ? problema no AnalyzingPhaseController.  
Se **APARECER** mas GridBreathing não reage ? problema na subscrição.

#### **Como Corrigir:**
Arquivo: `AnalyzingPhaseController.cs` (linha ~130)

Adicionar log temporário:
```csharp
// Disparar evento geral para breathing
if (foundPatterns.Count > 0)
{
    Debug.Log($"[AnalyzingPhase] BEFORE dispatch: {foundPatterns.Count} patterns");
    events.Pattern.TriggerPatternsDetected(foundPatterns, totalPoints);
    Debug.Log($"[AnalyzingPhase] AFTER dispatch");
    _config.DebugLog($"Breathing event dispatched: {foundPatterns.Count} patterns, {totalPoints} points");
}
```

Se aparecer "BEFORE" e "AFTER" mas GridBreathing não reage:
? Subscrição falhou (voltar à Solução 2)

---

## ?? **CHECKLIST DE VERIFICAÇÃO**

Executar em ordem:

### **Passo 1: Verificar Cena**
- [ ] Abrir cena do jogo
- [ ] Procurar GameObject com `GridBreathingController`
- [ ] Se não existe ? **Solução 1** (adicionar à cena)

### **Passo 2: Verificar Logs de Inicialização**
Rodar o jogo e procurar:
- [ ] `[GridBreathing] Initialized. Original scale: ...`
- [ ] `[GridBreathing] Starting breathing animation...`
- [ ] `[GridBreathing] ? Subscribed to OnPatternsDetected event`

Se aparecer "? Failed to subscribe":
? **Solução 2** (OnEnable + DelayedInitialize)

### **Passo 3: Verificar Config**
- [ ] `Assets/Resources/Patterns/PatternVisualConfig.asset` existe?
- [ ] `breathingSpeed` > 0?
- [ ] `breathingAmount` > 0?

Se não:
? **Solução 3** (criar/configurar asset)

### **Passo 4: Testar Padrão**
1. Play
2. Plantar 2 crops adjacentes
3. Clicar Sleep
4. Verificar Console:
   - [ ] `[AnalyzingPhase] Pattern found: Par Adjacente at slot X`
   - [ ] `[AnalyzingPhase] Breathing event dispatched: 1 patterns, 5 points`
   - [ ] `[GridBreathing] ?? OnPatternsDetected called! Matches: 1, Points: 5`

Se "OnPatternsDetected" **NÃO** aparece:
? **Solução 4** (verificar evento)

---

## ?? **TESTE COMPLETO**

### **Setup:**
```
1. Garantir GridBreathingController está na cena
2. Garantir PatternVisualConfig existe com valores > 0
3. Build ? Play
```

### **Teste 1: Breathing Idle**
```
Esperado: Grid respira suavemente (scale oscila)
Se não respira: Config tem valores zerados
```

### **Teste 2: Pattern Reaction**
```
1. Plantar 2 Wheats adjacentes
2. Sleep
3. Esperado: 
   - Grid pulsa (scale aumenta rapidamente)
   - Volta ao breathing normal
```

### **Logs Esperados:**
```
[GridBreathing] Initialized. Original scale: (1, 1, 1)
[GridBreathing] Starting breathing animation...
[GridBreathing] ? Subscribed to OnPatternsDetected event
[AnalyzingPhase] Pattern found: Par Adjacente at slot 0
[AnalyzingPhase] Breathing event dispatched: 1 patterns, 5 points
[GridBreathing] ?? OnPatternsDetected called! Matches: 1, Points: 5
[GridBreathing] Starting pattern reaction animation...
```

---

## ?? **TROUBLESHOOTING AVANÇADO**

### **Se Nada Funcionar:**

#### **Reset Completo:**
```csharp
// GridBreathingController.cs
private void OnEnable()
{
    Debug.Log("=== GRIDBREATHING DEBUG START ===");
    Debug.Log($"AppCore.Instance: {AppCore.Instance != null}");
    Debug.Log($"AppCore.Events: {AppCore.Instance?.Events != null}");
    Debug.Log($"AppCore.Events.Pattern: {AppCore.Instance?.Events?.Pattern != null}");
    Debug.Log($"Config: {_config != null}");
    Debug.Log($"GridTransform: {_gridTransform != null}");
    Debug.Log("=== GRIDBREATHING DEBUG END ===");
    
    StartCoroutine(DelayedInitialize());
}

private IEnumerator DelayedInitialize()
{
    yield return new WaitForSeconds(0.5f); // 500ms delay
    
    if (_config == null)
    {
        _config = Resources.Load<PatternVisualConfig>("Patterns/PatternVisualConfig");
        Debug.Log($"Config loaded: {_config != null}");
    }
    
    if (_gridTransform == null)
    {
        _gridTransform = transform;
    }
    
    _originalScale = _gridTransform.localScale;
    
    StartBreathing();
    SubscribeToEvents();
}
```

---

## ? **SOLUÇÃO MAIS PROVÁVEL**

90% dos casos: **GridBreathingController não está na cena**.

**Fix Rápido:**
1. Hierarchy ? GridManager ? Add Component ? GridBreathingController
2. Play
3. Testar Sleep com 2 crops adjacentes
4. Deve funcionar!

---

**Status:** Pronto para testes em runtime com logs detalhados
