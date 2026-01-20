# SISTEMA DE PADRÕES DE GRID - DESIGN DOCUMENT

## ?? **OVERVIEW DO SISTEMA**

**Nome**: Pattern Scoring System  
**Propósito**: Sistema de pontuação baseado em padrões no grid (tipo Poker)  
**Status**: Design completo, pronto para implementação  
**Prioridade**: Alta (core gameplay loop)

---

## ?? **FILOSOFIA DE DESIGN**

### **Conceito Central**
```
"Deixar plantado = Pontos | Colher = Dinheiro"
```

### **Pilares (V2 - Decay Update)**
1. **Descoberta Emergente** - Plantar e descobrir padrões por acaso
2. **Satisfação Imediata** - Completar padrão = dopamina
3. **Pressão Temporal** - Padrões são fortes mas instáveis (decay forçado)
4. **Adaptação Forçada** - Meta sobe, padrões decaem, jogador renova
5. **Sinergias Controladas** - Múltiplos padrões = melhor, mas com soft cap

**Conceito V2:**
```
Patrões existem para alguns dias, não para sempre
Grid "resolvido" é raro por escolha, não inevitável
Early game é permissivo, mid/late exige intenção
Sistema conversa com withering, cartas limitadas, meta obrigatória
```

### **Inspiração**
- **Poker** (mãos reconhecíveis, combos fixos)
- **Balatro** (multiplicadores, sinergias)
- **Stardew Valley** (grid farming)

---

## ?? **REGRAS FUNDAMENTAIS**

### **R1: Timing de Pontuação**
- Padrões são detectados **FIM DO DIA** (quando clica "Sleep")
- Momento: Durante `DailyResolutionSystem` ? `AnalyzeGrid()`
- Acontece ANTES de withering, DEPOIS de crescimento

### **R2: Decay Temporal (ATUALIZADO V2)**
- Padrões pontuam **DIARIAMENTE**, mas com **decay progressivo**
- **Decay**: -10% de pontuação por dia consecutivo mantendo o mesmo padrão
- **Reset**: Decay reseta semanalmente OU quando padrão é quebrado e recriado
- **Bonus pós-reset**: Padrões recriados ganham +10% no primeiro dia
- **Filosofia**: Padrões são fortes no curto prazo (2-3 dias), mas estruturalmente instáveis no longo prazo

**Exemplo de Decay:**
```
DIA 1: Linha de Cenouras ? 25 pts (100%)
DIA 2: Mesma linha ? 22.5 pts (90%)
DIA 3: Mesma linha ? 20 pts (80%)
DIA 4: 1 cenoura murcha ? padrão quebrado
DIA 5: Nova linha de Milho ? 30 pts (100% + 10% bonus) = 33 pts!
```

### **R3: Sobreposição**
- Slots **PODEM** contar para múltiplos padrões
- Double/triple dipping é **PERMITIDO** e **INCENTIVADO**
- Exemplo válido:
```
[??][??][??][??][??]  ? Linha Horizontal (25 pts)
[??][  ][  ][  ][??]  ? Moldura (40 pts)
[??][  ][  ][  ][??]
[??][  ][  ][  ][??]
[??][??][??][??][??]
= 65 pontos (sinergia!)
```

### **R4: Valor de Crops**
- **Base**: Cada crop tem valor diferente (Carrot < Corn < Pepper)
- **Raridade**: Carta rara da mesma crop vale MAIS
  - Carrot Comum (val: 5) < Corn Comum (val: 8)
  - Carrot Rara (val: 10) > Corn Comum (val: 8)
- **Padrão**: Pontos = BaseScore × (Avg Crop Value / 5)

### **R5: Maturidade**
- Plantas **MADURAS** dão **+50% bonus**
- Padrão com 3 maduras + 2 jovens = bonus parcial
- Formula: `finalScore = baseScore × (1 + 0.5 × ratioMature)`

### **R6: Plantas Mortas**
- Withered crops **QUEBRAM** o padrão
- Linha com 1 morta no meio = NÃO CONTA
- Ignora mortas, mas padrão precisa ser contíguo

### **R7: Slots Bloqueados**
- Locked slots são **IGNORADOS**
- Padrão pula locked e conta só os válidos
- **IMPORTANTE**: Slots bloqueados **interrompem continuidade geométrica**, mas não invalidam padrões menores adjacentes
- Exemplo:
```
[??][??][??][??][??]  ? NÃO É "linha de 4" (locked quebra continuidade)
                         ? É "par + par" (2+2 separados, ambos válidos)
```

**Implicação técnica**: Padrões que dependem de continuidade (Linha, Diagonal, Moldura, Cruz) são quebrados por locked. Padrões locais (Par, Trio local, Cantinho) não são afetados.

### **R8: Grid 5x5**
- Tamanho fixo (feature, não limitação)
- 25 slots totais
- 5 crops base por estação (futuro: 10)

---

## ?? **TABELA DOS 10 PADRÕES BASE**

### **TIER 1: INICIANTE (5-15 pts) - "Sempre consegue"**

| # | Nome | Descrição | Pontos Base | Condição | Dificuldade |
|---|------|-----------|-------------|----------|-------------|
| **1** | **Par Adjacente** | 2 crops iguais lado a lado (H ou V) | **5 pts** | Mesma crop, adjacentes | ? |
| **2** | **Trio em Linha** | 3 crops iguais em linha (H ou V) | **10 pts** | Mesma crop, linha/coluna | ? |
| **3** | **Cantinho** | 3 crops iguais formando L | **8 pts** | Mesma crop, canto do grid | ? |

**Filosofia Tier 1 (V2)**: Impossível NÃO conseguir pelo menos 1. Confiança inicial. **Early game dominante**, mas contribuição cai naturalmente com meta crescente (Tier 1 vira "ruído aceitável" no late game).

---

### **TIER 2: CASUAL (15-35 pts) - "Com pouco esforço"**

| # | Nome | Descrição | Pontos Base | Condição | Dificuldade |
|---|------|-----------|-------------|----------|-------------|
| **4** | **Linha Completa** | 5 crops iguais em linha inteira | **25 pts** | Mesma crop, linha/coluna completa | ?? |
| **5** | **Xadrez 2x2** | 4 crops alternados em quadrado | **20 pts** | Padrão ABAB em 2x2 | ?? |
| **6** | **Cruz Simples** | 5 crops formando + (centro + 4 adj) | **30 pts** | Mesma crop, centro + N/S/E/W | ?? |

**Filosofia Tier 2**: Requer planejamento básico (1-2 dias). Recompensa consistência.

---

### **TIER 3: DEDICADO (35-60 pts) - "Precisa planejar"**

| # | Nome | Descrição | Pontos Base | Condição | Dificuldade |
|---|------|-----------|-------------|----------|-------------|
| **7** | **Diagonal** | 5 crops iguais em diagonal (\ ou /) | **40 pts** | Mesma crop, diagonal completa | ??? |
| **8** | **Moldura** | Bordas do grid mesma crop (16 slots) | **50 pts** | Mesma crop, todas as bordas | ??? |
| **9** | **Arco-íris** | Linha com crops DIFERENTES | **55 pts** | 3-5 tipos diferentes, linha/coluna | ???? |

**Filosofia Tier 3**: Commitment de 3-5 dias. Alto risco/recompensa. Meta-changer.

---

### **TIER 4: MASTER (80-150 pts) - "Win condition"**

| # | Nome | Descrição | Pontos Base | Condição | Dificuldade |
|---|------|-----------|-------------|----------|-------------|
| **10** | **Grid Perfeito** | Todos os 25 slots plantados COM DIVERSIDADE | **150 pts** | 25 crops vivas + mínimo 4 tipos diferentes | ????? |

**Filosofia Tier 4 (V2)**: Mais comum do mid-game em diante. Raro por escolha (diversidade), não por dificuldade técnica. **High-investment late-game strategy** entre várias possíveis. Game changer, mas não win condition automática.

---

## ?? **FÓRMULAS DE CÁLCULO**

### **Formula Base**
```csharp
baseScore = pattern.BaseScore;
cropValue = AverageCropValue(slots);
maturityRatio = CountMature(slots) / slots.Count;
daysActive = pattern.DaysActive; // NOVO: tracking de decay
decayMultiplier = Mathf.Pow(0.9f, daysActive - 1); // -10% por dia

finalScore = baseScore 
           × (cropValue / 5.0f)          // Crop multiplier
           × (1 + 0.5f × maturityRatio)  // Maturity bonus
           × decayMultiplier;            // NOVO: Decay temporal

// Exemplo com Decay:
// Linha de 5 Carrots (val: 5), 3 maduras, 3º dia consecutivo
// = 25 × (5/5) × (1 + 0.5 × 0.6) × (0.9^2)
// = 25 × 1.0 × 1.3 × 0.81
// = 26.3 pts (era 32.5 sem decay)

// NOTA: Para padrões especiais como Arco-íris e Grid Perfeito,
// usar fórmulas customizadas (ver seções específicas)
```

### **Bonus de Sinergia (ATUALIZADO V2 - Soft Cap)**
```csharp
// Soft cap logarítmico - evita explosão numérica
float synergyBonus = 1.0f + 0.2f * Mathf.Log(patternCount, 2);

// Exemplos:
// 2 padrões = 1.0 + 0.2 * 1.0 = 1.2x (+20%)
// 4 padrões = 1.0 + 0.2 * 2.0 = 1.4x (+40%)
// 8 padrões = 1.0 + 0.2 * 3.0 = 1.6x (+60%)
// 16 padrões = 1.0 + 0.2 * 4.0 = 1.8x (+80%) [praticamente impossível]

// Filosofia: Sinergia cresce rápido no início, desacelera depois
// Números absurdos são aceitos, mas não infinitos
// Jogador sente que "quebrou o jogo", mas sistema mantém controle
```

---

## ??? **ARQUITETURA TÉCNICA**

### **Diagrama de Componentes**
```
???????????????????????????????????????????
?  IGridPattern (interface)               ?
?    ?? AdjacentPairPattern               ?
?    ?? LinePattern (Trio, Full, Rainbow) ?
?    ?? CornerPattern                     ?
?    ?? CheckerPattern                    ?
?    ?? CrossPattern                      ?
?    ?? DiagonalPattern                   ?
?    ?? FramePattern                      ?
?    ?? PerfectGridPattern                ?
???????????????????????????????????????????
?  PatternLibrary (ScriptableObject)      ?
?    ?? Lista dos 10 padrões base         ?
???????????????????????????????????????????
?  PatternDetector (service)              ?
?    ?? DetectAll(IGridService)           ?
???????????????????????????????????????????
?  PatternScoreCalculator (service)       ?
?    ?? Calculate(matches, context)       ?
???????????????????????????????????????????
?  PatternEvents (event bus)              ?
?    ?? OnPatternDetected                 ?
?    ?? OnScoreCalculated                 ?
???????????????????????????????????????????
```

---

## ?? **ESTRUTURA DE ARQUIVOS**

### **Novos arquivos a criar:**
```
Assets/Scripts/Domain/Patterns/
??? Core/
?   ??? IGridPattern.cs                    ? Interface base
?   ??? PatternMatch.cs                    ? DTO de resultado
?   ??? PatternDetector.cs                 ? Serviço principal
?   ??? PatternScoreCalculator.cs          ? Cálculo de pontos
?
??? Implementations/
?   ??? AdjacentPairPattern.cs             ? Padrão #1
?   ??? TrioLinePattern.cs                 ? Padrão #2
?   ??? CornerPattern.cs                   ? Padrão #3
?   ??? FullLinePattern.cs                 ? Padrão #4
?   ??? CheckerPattern.cs                  ? Padrão #5
?   ??? CrossPattern.cs                    ? Padrão #6
?   ??? DiagonalPattern.cs                 ? Padrão #7
?   ??? FramePattern.cs                    ? Padrão #8
?   ??? RainbowLinePattern.cs              ? Padrão #9
?   ??? PerfectGridPattern.cs              ? Padrão #10
?
??? Data/
?   ??? PatternLibrary.cs                  ? ScriptableObject
?
??? Events/
    ??? PatternEvents.cs                   ? Event bus

Assets/Scripts/Infrastructure/Events/
??? PatternEvents.cs                       ? Adicionar a GameEvents

Assets/Documentation/
??? PATTERN_SYSTEM_DESIGN.md              ? Este arquivo
```

---

## ?? **SCRIPTS A MODIFICAR**

### **1. GameEvents.cs**
```csharp
public class GameEvents
{
    public GridEvents Grid { get; private set; }
    public PatternEvents Pattern { get; private set; } // ? NOVO
    // ... resto
    
    public GameEvents()
    {
        Grid = new GridEvents();
        Pattern = new PatternEvents(); // ? NOVO
        // ...
    }
}
```

### **2. DailyResolutionSystem.cs**
```csharp
private IEnumerator AnalyzeGrid()
{
    // ... código existente de grow/wither ...
    
    // NOVO: Detectar padrões
    var patternDetector = AppCore.Instance.PatternDetector;
    var matches = patternDetector.DetectAll(_gridService);
    
    yield return new WaitForSeconds(0.3f);
    
    // NOVO: Calcular pontos
    var calculator = AppCore.Instance.PatternCalculator;
    int bonusPoints = calculator.CalculateTotal(matches);
    
    // NOVO: Adicionar aos pontos do dia
    _currentDayScore += bonusPoints;
    
    Debug.Log($"[Patterns] {matches.Count} padrões = +{bonusPoints} pts!");
    
    // ... resto da análise ...
}
```

### **3. AppCore.cs**
```csharp
public class AppCore : MonoBehaviour
{
    // ... campos existentes ...
    
    private PatternDetector _patternDetector;
    private PatternScoreCalculator _patternCalculator;
    
    public PatternDetector PatternDetector => _patternDetector;
    public PatternScoreCalculator PatternCalculator => _patternCalculator;
    
    [SerializeField] private PatternLibrary _patternLibrary;
    public PatternLibrary PatternLibrary => _patternLibrary;
    
    private void InitializeGlobalServices()
    {
        // ... código existente ...
        
        // NOVO: Pattern System
        _patternDetector = new PatternDetector(_patternLibrary, _events.Pattern);
        _patternCalculator = new PatternScoreCalculator();
        
        Debug.Log("[AppCore] Pattern System inicializado");
    }
}
```

### **4. RunData.cs (SaveData)**
```csharp
[System.Serializable]
public class RunData
{
    // ... campos existentes ...
    
    // NOVO: Tracking de padrões
    public int TotalPatternsCompleted;
    public int HighestDailyPatternScore;
    public Dictionary<string, int> PatternCompletionCount;
}
```

---

## ?? **DADOS DE EXEMPLO**

### **Exemplo 1: Grid Casual**
```
DIA 3:
[??][??][??][  ][  ]  ? Trio em Linha (10 pts)
[??][  ][  ][  ][  ]
[  ][  ][  ][  ][  ]
[??][??][  ][  ][  ]  ? Par Adjacente (5 pts)
[  ][  ][  ][  ][  ]

PADRÕES DETECTADOS:
? Trio em Linha (Carrots) ? 10 pts
? Par Adjacente (Corn) ? 5 pts

TOTAL: 15 pontos de padrões
```

### **Exemplo 2: Grid Otimizado**
```
DIA 7:
[??][??][??][??][??]  ? Linha Completa (25 pts)
[??][  ][  ][  ][??]  ? Moldura (50 pts)
[??][  ][  ][  ][??]
[??][  ][  ][  ][??]
[??][??][??][??][??]

PADRÕES DETECTADOS:
? Linha Completa (Row 0) ? 25 pts
? Linha Completa (Row 4) ? 25 pts
? Moldura ? 50 pts

SINERGIA: 3 padrões = 1.2x multiplier
TOTAL: 100 × 1.2 = 120 pontos!
```

### **Exemplo 3: Grid Perfeito**
```
DIA 14 (Boss):
[??][??][??][??][??]
[??][??][??][??][??]
[??][??][??][??][??]
[??][??][??][??][??]
[??][??][??][??][??]

PADRÕES DETECTADOS:
? Xadrez 2x2 (múltiplos) ? 80 pts
? Grid Perfeito ? 150 pts
? Linha Arco-íris (várias) ? 165 pts

SINERGIA: 10+ padrões = 1.9x
TOTAL: 395 × 1.9 = 750 pontos!!!
```

---

## ?? **INTEGRAÇÃO COM GAMEPLAY**

### **Meta Diária**
```
Meta padrão: 100 pontos de harvest
Meta nova: 100 pontos (harvest + padrões)

DIA NORMAL:
- 50 pts harvest (vendeu crops)
- 50 pts padrões (deixou plantado)
= 100 pts (bateu meta!)

DIA OTIMIZADO:
- 20 pts harvest (vendeu pouco)
- 150 pts padrões (grid estratégico)
= 170 pts (OVERACHIEVER!)
```

### **Trade-off Core**
```
COLHER = Dinheiro (comprar cartas)
DEIXAR PLANTADO = Pontos (bater meta)

Decisão estratégica:
- Preciso dinheiro? ? Colho tudo
- Preciso pontos? ? Deixo grid perfeito
- Mix? ? Colho harvest, deixo padrões
```

---

## ?? **CASOS DE TESTE**

### **Teste 1: Par Simples**
```
Input:
[??][??][  ][  ][  ]

Expected:
? AdjacentPairPattern detected
   Slots: [0, 1]
   Score: 5 pts
```

### **Teste 2: Linha com Locked**
```
Input:
[??][??][??][??][??]

Expected:
? TrioLinePattern detected (slots 0,1 ignoram locked)
? NÃO é FullLinePattern (locked quebra)
```

### **Teste 3: Linha com Morta**
```
Input:
[??][??][??][??][??]

Expected:
? NENHUM padrão (morta quebra tudo)
```

### **Teste 4: Sobreposição**
```
Input:
[??][??][??]
[??][  ][  ]
[??][  ][  ]

Expected:
? TrioLinePattern (row 0) ? 10 pts
? TrioLinePattern (col 0) ? 10 pts
? CornerPattern (L shape) ? 8 pts
TOTAL: 28 pts (sobreposição OK!)
```

---

## ?? **CHECKLIST DE IMPLEMENTAÇÃO**

### **Fase 1: Core (Dia 1)**
- [ ] Criar `IGridPattern.cs`
- [ ] Criar `PatternMatch.cs`
- [ ] Criar `PatternDetector.cs`
- [ ] Criar `PatternScoreCalculator.cs`
- [ ] Criar `PatternEvents.cs`
- [ ] Integrar em `GameEvents.cs`

### **Fase 2: Padrões Tier 1-2 (Dia 2)**
- [ ] `AdjacentPairPattern.cs`
- [ ] `TrioLinePattern.cs`
- [ ] `CornerPattern.cs`
- [ ] `FullLinePattern.cs`
- [ ] `CheckerPattern.cs`
- [ ] `CrossPattern.cs`

### **Fase 3: Padrões Tier 3-4 (Dia 3)**
- [ ] `DiagonalPattern.cs`
- [ ] `FramePattern.cs`
- [ ] `RainbowLinePattern.cs`
- [ ] `PerfectGridPattern.cs`

### **Fase 4: ScriptableObject (Dia 3)**
- [ ] Criar `PatternLibrary.cs`
- [ ] Criar asset `PatternLibrary.asset` na Unity
- [ ] Configurar 10 padrões no Inspector

### **Fase 5: Integração (Dia 4)**
- [ ] Modificar `AppCore.cs`
- [ ] Modificar `DailyResolutionSystem.cs`
- [ ] Modificar `RunData.cs`
- [ ] Adicionar logs de debug

### **Fase 6: Testes (Dia 5)**
- [ ] Testar cada padrão individualmente
- [ ] Testar sobreposição
- [ ] Testar casos edge (locked, withered)
- [ ] Testar formula de pontuação
- [ ] Tunning de valores

---

## ?? **PRÓXIMOS PASSOS (Fase 2 - Futuro)**

### **UI de Padrões**
- Popup mostrando padrões detectados
- Tabela in-game com todos os 10 padrões
- Highlight visual dos slots que formam padrão
- Animação de "Pattern Completed!"

### **Tradições (Modificadores)**
- Sistema de buffs estilo Balatro Jokers
- "Mestre da Moldura" ? Molduras valem 2x
- "Arco-íris Divino" ? Linha diversa +100%
- Escolher 3 tradições ativas por run

### **Stats & Meta**
- Tracking de padrões completados
- Achievement "Complete todos os 10 em 1 dia"
- Daily challenge "Complete 5 Molduras"

---

## ?? **NOTAS PARA O PRÓXIMO CHAT (Claude Opus)**

### **Contexto do Projeto**
- Farming game roguelike
- Grid 5x5
- Sistema de cartas (plantar/regar/colher)
- Ciclo dia/noite
- Meta diária de pontos

### **Refatoração Recente (IMPORTANTE!)**
- Grid visual foi COMPLETAMENTE refatorado
- Usa Dependency Injection (GridVisualContext)
- IDropValidator para validação
- Event-driven architecture
- Ver: `Assets/Documentation/GRID_REFACTORING_SUMMARY.md`

### **Estilo de Código**
- C# 9.0, .NET Framework 4.7.1
- SOLID principles
- Interfaces para tudo que é testável
- ScriptableObjects para configuração
- Event-driven onde possível
- Comentários inline extensivos

### **Sistema de Eventos**
- `AppCore.Instance.Events.Pattern` ? Novo event bus
- Eventos: `OnPatternDetected`, `OnScoreCalculated`
- Sempre disparar eventos para UI reagir

### **Integração com Existente**
- `DailyResolutionSystem` já existe e funciona
- `GridService` expõe `GetSlotReadOnly(index)`
- `IGridService.Config` tem Rows/Columns
- `GameLibrary` tem `TryGetCrop(cropID, out data)`

### **O que NÃO mudar**
- Grid visual (acabamos de refatorar!)
- Sistema de cartas (funciona perfeitamente)
- DailyResolution flow (só adicionar detecção no meio)

### **Prioridade**
1. Implementar detecção funcional (sem UI)
2. Integrar com DailyResolution
3. Testar com logs
4. UI depois (próxima feature)

---

## ? **APROVAÇÃO DO DESIGN**

**Designer**: Davi  
**Status**: ? APROVADO  
**Data**: 2024  
**Próximo**: Implementação no Claude Opus

---

**FIM DO DOCUMENTO**
