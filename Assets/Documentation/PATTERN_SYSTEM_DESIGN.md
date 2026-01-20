# SISTEMA DE PADRÕES DE GRID - DESIGN DOCUMENT

## 🚀 **GUIA RÁPIDO DE IMPLEMENTAÇÃO**

### **📌 ANTES DE COMEÇAR:**
```sh
# 1. Criar branch da Onda
git checkout -b feature/pattern-system-wave-1

# 2. Ler seção "ONDA 1" por completo
# 3. Criar checklist de tarefas (pode usar papel!)
```

---

### **⏱️ WORKFLOW DE CADA TAREFA:**

```
🔨 IMPLEMENTAR
    ↓
📝 COMMIT (feature pequena)
    ↓
🧪 TESTAR manualmente
    ↓
✅ Se OK → Próxima tarefa
❌ Se bug → Corrigir → Commit fix → Testar novamente
    ↓
🔁 Repetir até completar ONDA
    ↓
🎯 VALIDAÇÃO COMPLETA (jogar 7 dias)
    ↓
🔀 MERGE na main
    ↓
🏷️ TAG (wave-N-complete)
```

---

### **💾 QUANDO FAZER COMMIT:**

**✅ FAÇA COMMIT quando:**
- [ ] Criar 1-2 arquivos novos que **compilam**
- [ ] Implementar 1 padrão completo (ex: `AdjacentPairPattern.cs`)
- [ ] Adicionar 1 método funcional no Calculator
- [ ] Integrar 1 componente no AppCore
- [ ] Corrigir 1 bug específico

**❌ NÃO FAÇA COMMIT quando:**
- [ ] Código não compila
- [ ] "Meio implementado" (termina primeiro!)
- [ ] Fim de dia (termine a feature atual)

**Mensagens sugeridas:**
```sh
# Features (80% dos commits)
git commit -m "feat(patterns): IGridPattern interface + PatternMatch DTO"
git commit -m "feat(patterns): AdjacentPairPattern implementado"
git commit -m "feat(patterns): PatternDetector com 5 padrões hardcoded"
git commit -m "feat(patterns): DetectPatternsStep integrado ao pipeline"
git commit -m "feat(patterns): PatternScoreCalculator com crop value"

# Fixes (quando quebrar algo)
git commit -m "fix(patterns): Trio detectando locked incorretamente"
git commit -m "fix(patterns): NullRef em PatternDetector.DetectAll"

# Tests (validações)
git commit -m "test(patterns): Onda 1 validada (5 padrões funcionando)"
git commit -m "test(patterns): Checklist Onda 1 completo"
```

---

### **🔍 QUANDO TESTAR:**

| O Que | Quando | Como |
|-------|--------|------|
| **Compilação** | Após cada commit | Build → Play (F5) |
| **Feature isolada** | Após implementar 1 padrão | Criar grid específico, testar detecção |
| **Integração** | Após adicionar ao pipeline | Jogar 1 dia completo |
| **Onda completa** | Antes de mergear | Jogar 7 dias in-game, 0 crashes |

**Teste Rápido (5 min):**
```sh
1. Play no Unity
2. Criar padrão específico no grid
3. Clicar "Sleep"
4. Verificar logs no Console
5. Se detectou → ✅ Próximo
   Se bugou → 🐛 Corrigir agora
```

---

### **🔀 QUANDO FAZER MERGE:**

**SOMENTE quando:**
- [x] Todos os checkboxes da Onda estão completos
- [x] Build compila sem erros
- [x] Jogar 7 dias sem crashes
- [x] Logs mostram detecção correta
- [x] Critérios de aceitação da Onda = OK

**Processo de Merge:**
```sh
# 1. Validação final
# Jogar 7 dias in-game, ver logs, verificar score

# 2. Commit de teste
git add Assets/Testing/PATTERN_WAVE_1_CHECKLIST.md
git commit -m "test: Onda 1 validada (7 dias playtest, 0 crashes)"

# 3. Merge
git checkout main
git merge feature/pattern-system-wave-1 --no-ff -m "feat: Pattern System - Onda 1 completa (MVP)"

# 4. Tag
git tag -a wave-1-complete -m "Onda 1: MVP com 5 padrões funcionando"
git push origin main --tags

# 5. Comemorar 🎉
```

---

### **⚠️ SE ALGO QUEBRAR:**

```sh
# Opção 1: Bug pequeno (correção rápida)
# Corrigir → Testar → Commit fix → Continuar

# Opção 2: Bug grande (quebrando tudo)
git log --oneline  # Ver últimos commits
git reset --soft HEAD~1  # Desfazer último commit (mantém código)
# Corrigir → Testar → Commit correto → Continuar

# Opção 3: Desastre total (começar de novo)
git checkout main
git branch -D feature/pattern-system-wave-1
git checkout -b feature/pattern-system-wave-1
# Recomeçar a onda
```

---

### **📊 RESUMO VISUAL:**

```
DIA 1-2 (Onda 1 - Parte 1):
├─ Implementar Core (IGridPattern, PatternMatch, etc)
├─ Commit: "feat(patterns): Core infrastructure"
├─ Testar compilação
├─ Implementar Par + Trio
├─ Commit: "feat(patterns): Par Adjacente + Trio"
├─ Testar cada padrão isoladamente
└─ ✅ Se OK → Próximo dia

DIA 3 (Onda 1 - Parte 2):
├─ Implementar 3 padrões restantes
├─ Commit: "feat(patterns): Cantinho, Linha, Cruz"
├─ Implementar DetectPatternsStep
├─ Commit: "feat(patterns): DetectPatternsStep integrado"
├─ Testar 1 dia completo
└─ ✅ Se OK → Finalizar Onda

DIA 4-5 (Onda 1 - Validação):
├─ Setup AppCore
├─ Commit: "feat(patterns): AppCore expõe Detector + Calculator"
├─ Jogar 7 dias in-game (validação completa)
├─ Preencher checklist de testes
├─ Commit: "test: Onda 1 validada"
├─ MERGE na main
├─ TAG wave-1-complete
└─ 🎉 Onda 1 completa!
```

---

### **🎯 REGRA DE OURO:**

> **"1 feature funcionando > 10 features pela metade"**

Termine o que começou. Teste antes de avançar. Commit quando compilar.

---

## 📊 **OVERVIEW DO SISTEMA**

**Nome**: Pattern Scoring System  
**Propósito**: Sistema de pontuação baseado em padrões no grid (tipo Poker)  
**Status**: Design completo, pronto para implementação  
**Prioridade**: Alta (core gameplay loop)

---

## 🎯 **FILOSOFIA DE DESIGN**

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

## 📐 **REGRAS FUNDAMENTAIS**

### **R1: Timing de Pontuação**
- Padrões são detectados **FIM DO DIA** (quando clica "Sleep")
- Momento: Durante `DailyResolutionSystem` → `AnalyzeGrid()`
- Acontece ANTES de withering, DEPOIS de crescimento

### **R2: Decay Temporal (ATUALIZADO V2)**
- Padrões pontuam **DIARIAMENTE**, mas com **decay progressivo**
- **Decay**: -10% de pontuação por dia consecutivo mantendo o mesmo padrão
- **Reset**: Decay reseta semanalmente OU quando padrão é quebrado e recriado
- **Bonus pós-reset**: Padrões recriados ganham +10% no primeiro dia
- **Filosofia**: Padrões são fortes no curto prazo (2-3 dias), mas estruturalmente instáveis no longo prazo

**Exemplo de Decay:**
```
DIA 1: Linha de Cenouras → 25 pts (100%)
DIA 2: Mesma linha → 22.5 pts (90%)
DIA 3: Mesma linha → 20 pts (80%)
DIA 4: 1 cenoura murcha → padrão quebrado
DIA 5: Nova linha de Milho → 30 pts (100% + 10% bonus) = 33 pts!
```

#### **⚠️ IDENTIDADE DE PADRÃO (CRÍTICO)**

**Definição Formal**: Um padrão é considerado o **"mesmo padrão"** para efeitos de decay SOMENTE se:

1. **PatternType** (classe) for idêntico (ex: `FullLinePattern`)
2. **Slots exatos** (índices) forem os mesmos (ex: Row 0 = [0,1,2,3,4])
3. **CropID** de todas as crops envolvidas for o mesmo (quando aplicável)

**Implicações Técnicas:**
```csharp
// Identidade única de padrão para tracking
PatternInstanceID = Hash(PatternType + SlotIndices + CropIDs);

// Exemplos de NOVO PADRÃO (reseta decay):
- Mudou 1 slot → NOVO PADRÃO
- Trocou crop de Cenoura pra Milho → NOVO PADRÃO  
- Colheu e replantou mesmo slot → NOVO PADRÃO (crescimento reinicia)
- Planta morreu e foi substituída → NOVO PADRÃO

// Exemplos de MESMO PADRÃO (decay continua):
- Apenas cresceu (young → mature) → MESMO PADRÃO
- Foi regada → MESMO PADRÃO
- Nada mudou → MESMO PADRÃO
```

**Por que isso importa:**
- Previne exploits de "replante gratuito"
- Legitima decay como mecânica de pressão temporal
- Simplifica implementação (sem lógica "criativa" de comparação)
- Torna save/load determinístico

### **R3: Sobreposição**
- Slots **PODEM** contar para múltiplos padrões
- Double/triple dipping é **PERMITIDO** e **INCENTIVADO**
- Exemplo válido:
```
[🥕][🥕][🥕][🥕][🥕]  ← Linha Horizontal (25 pts)
[🥕][  ][  ][  ][🥕]  ← Moldura (40 pts)
[🥕][  ][  ][  ][🥕]
[🥕][  ][  ][  ][🥕]
[🥕][🥕][🥕][🥕][🥕]
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
[🥕][🥕][🔒][🥕][🥕]  ← NÃO É "linha de 4" (locked quebra continuidade)
                         ← É "par + par" (2+2 separados, ambos válidos)
```

**Implicação técnica**: Padrões que dependem de continuidade (Linha, Diagonal, Moldura, Cruz) são quebrados por locked. Padrões locais (Par, Trio local, Cantinho) não são afetados.

### **R8: Grid 5x5**
- Tamanho fixo (feature, não limitação)
- 25 slots totais
- 5 crops base por estação (futuro: 10)

### **R9: Tempo de Crescimento como Pilar (CRÍTICO)**
- Plantas **NÃO** crescem instantaneamente
- **Crescimento lento** (3-4 dias até maturidade) cria **custo irrecuperável**
- **Implicação estratégica**: Colher = prejuízo de padrão nos próximos dias

**Por que isso é fundamental:**
```
SEM crescimento lento:
Dia 1: Padrão → Pontos → Colher tudo → Dinheiro
Dia 2: Replantar igual → Padrão → Pontos (exploit!)

COM crescimento lento:
Dia 1: Padrão → Pontos → Colher tudo → Dinheiro
Dia 2: Grid jovem → Padrão fraco ou inexistente
Dia 3: Grid meio maduro → Padrão parcial
Dia 4: Grid maduro → Padrão forte novamente
```

**Decisão com atraso de consequência:**
- Você colhe agora, mas prejuízo aparece depois
- Pattern System premia **paciência**
- Harvest cobra **juros** (tempo de recuperação)
- Grid raramente está "perfeito" (oscila naturalmente)

**Regra de coerência:**
```
Tempo para reconstruir padrão forte ≥ Tempo de decay relevante
Exemplo: Se decay dói no dia 3, crescimento total em 3-4 dias está coerente
```

**⚠️ Implicação arquitetural:**
- `slot.HasCrop` **NÃO** é critério suficiente para padrões
- Estado da planta (young/mature) é parte da **linguagem do padrão**
- Nunca simplificar isso no futuro (dívida técnica grave)

---

## 🎴 **TABELA DOS 10 PADRÕES BASE**

### **TIER 1: INICIANTE (5-15 pts) - "Sempre consegue"**

| # | Nome | Descrição | Pontos Base | Condição | Dificuldade |
|---|------|-----------|-------------|----------|-------------|
| **1** | **Par Adjacente** | 2 crops iguais lado a lado (H ou V) | **5 pts** | Mesma crop, adjacentes | ⭐ |
| **2** | **Trio em Linha** | 3 crops iguais em linha (H ou V) | **10 pts** | Mesma crop, linha/coluna | ⭐ |
| **3** | **Cantinho** | 3 crops iguais formando L | **8 pts** | Mesma crop, canto do grid | ⭐ |

**Filosofia Tier 1 (V2)**: Impossível NÃO conseguir pelo menos 1. Confiança inicial. **Early game dominante**, mas contribuição cai naturalmente com meta crescente (Tier 1 vira "ruído aceitável" no late game).

---

### **TIER 2: CASUAL (15-35 pts) - "Com pouco esforço"**

| # | Nome | Descrição | Pontos Base | Condição | Dificuldade |
|---|------|-----------|-------------|----------|-------------|
| **4** | **Linha Completa** | 5 crops iguais em linha inteira | **25 pts** | Mesma crop, linha/coluna completa | ⭐⭐ |
| **5** | **Xadrez 2x2** | 4 crops alternados em quadrado | **20 pts** | Padrão ABAB em 2x2 | ⭐⭐ |
| **6** | **Cruz Simples** | 5 crops formando + (centro + 4 adj) | **30 pts** | Mesma crop, centro + N/S/E/W | ⭐⭐ |

**Filosofia Tier 2**: Requer planejamento básico (1-2 dias). Recompensa consistência.

---

### **TIER 3: DEDICADO (35-60 pts) - "Precisa planejar"**

| # | Nome | Descrição | Pontos Base | Condição | Dificuldade |
|---|------|-----------|-------------|----------|-------------|
| **7** | **Diagonal** | 5 crops iguais em diagonal (\ ou /) | **40 pts** | Mesma crop, diagonal completa | ⭐⭐⭐ |
| **8** | **Moldura** | Bordas do grid mesma crop (16 slots) | **50 pts** | Mesma crop, todas as bordas | ⭐⭐⭐ |
| **9** | **Arco-íris** | Linha com crops DIFERENTES | **55 pts** | 3-5 tipos diferentes, linha/coluna | ⭐⭐⭐⭐ |

**Filosofia Tier 3**: Commitment de 3-5 dias. Alto risco/recompensa. Meta-changer.

---

### **TIER 4: MASTER (80-150 pts) - "Win condition"**

| # | Nome | Descrição | Pontos Base | Condição | Dificuldade |
|---|------|-----------|-------------|----------|-------------|
| **10** | **Grid Perfeito** | Todos os 25 slots plantados COM DIVERSIDADE | **150 pts** | 25 crops vivas + mínimo 4 tipos diferentes | ⭐⭐⭐⭐⭐ |

**Filosofia Tier 4 (V2)**: Mais comum do mid-game em diante. Raro por escolha (diversidade), não por dificuldade técnica. **High-investment late-game strategy** entre várias possíveis. Game changer, mas não win condition automática.

---

## 🧮 **FÓRMULAS DE CÁLCULO**

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

## 🏗️ **ARQUITETURA TÉCNICA**

### **Diagrama de Componentes**
```
┌─────────────────────────────────────────┐
│  IGridPattern (interface)               │
│    ├─ AdjacentPairPattern               │
│    ├─ LinePattern (Trio, Full, Rainbow) │
│    ├─ CornerPattern                     │
│    ├─ CheckerPattern                    │
│    ├─ CrossPattern                      │
│    ├─ DiagonalPattern                   │
│    ├─ FramePattern                      │
│    └─ PerfectGridPattern                │
├─────────────────────────────────────────┤
│  PatternLibrary (ScriptableObject)      │
│    └─ Lista dos 10 padrões base         │
├─────────────────────────────────────────┤
│  PatternDetector (service)              │
│    └─ DetectAll(IGridService)           │
├─────────────────────────────────────────┤
│  PatternScoreCalculator (service)       │
│    └─ Calculate(matches, context)       │
├─────────────────────────────────────────┤
│  PatternEvents (event bus)              │
│    └─ OnPatternDetected                 │
│    └─ OnScoreCalculated                 │
└─────────────────────────────────────────┘
```

---

## 🎯 **RESPONSABILIDADES DOS COMPONENTES**

### **PatternDetector - O Orquestrador Burro**

**RESPONSABILIDADE ÚNICA:**
- Percorrer o grid linha por linha
- Delegar detecção para cada `IGridPattern.TryDetect()`
- Coletar resultados em lista de `PatternMatch`
- Emitir evento `OnPatternDetected`

**EXPLICITAMENTE FORA DO ESCOPO:**
- ❌ Cálculo de score
- ❌ Aplicação de decay
- ❌ Cálculo de sinergia
- ❌ Priorização de padrões
- ❌ Agrupamento de matches
- ❌ Lógica de coordenação entre padrões

**Filosofia:** Detector é **stateless**. Não guarda histórico, não decide valor, não modifica estado.

**Regra de ouro:**
```csharp
// ✅ BOM (detector apenas coleta)
foreach (var pattern in _patterns) {
    if (pattern.TryDetect(grid, out match)) {
        matches.Add(match);
    }
}

// ❌ RUIM (detector decidindo complexidade)
if (match.PatternType == "FullLine" && season == Spring) {
    match.BaseScore *= 1.5f; // NÃO! Isso é PatternScoreCalculator
}
```

---

### **PatternScoreCalculator - Autoridade Única de Pontuação**

**RESPONSABILIDADE ÚNICA:**
- Toda matemática de pontuação do sistema
- Aplicar fórmula base (CropValue, Maturity, Decay)
- Calcular sinergia global
- Lidar com casos especiais (Arco-íris, Grid Perfeito)
- Retornar score final

**REGRA CRÍTICA:**
```
NENHUM IGridPattern pode conter lógica matemática 
além do BaseScore (inteiro fixo).
```

**Por que isso importa:**
- Balanceamento centralizado
- Fácil tuning (1 único arquivo)
- Tradições futuras modificam Calculator, não Patterns
- Logs de debug consistentes

**Exemplo de responsabilidade correta:**
```csharp
// ✅ IGridPattern (apenas define critério)
public class FullLinePattern : IGridPattern {
    public int BaseScore => 25; // Valor fixo, sem lógica
    public bool TryDetect(...) { /* lógica geométrica */ }
}

// ✅ PatternScoreCalculator (toda matemática)
public int Calculate(PatternMatch match) {
    float score = match.BaseScore;
    score *= GetCropMultiplier(match.Slots);
    score *= GetMaturityBonus(match.Slots);
    score *= GetDecayMultiplier(match.DaysActive);
    return Mathf.RoundToInt(score);
}
```

**⚠️ Proteção futura:**
- Se precisar modificar pontuação → vá ao Calculator
- Se precisar adicionar modificador → vá ao Calculator
- Se IGridPattern começar a ter `if/else` de score → REFATORE

---

### **IGridPattern - Contrato de Detecção**

**RESPONSABILIDADE:**
- Definir geometria do padrão
- Validar slots (locked, withered, continuidade)
- Retornar `PatternMatch` se válido
- Declarar `BaseScore` (valor fixo)

**NÃO DEVE:**
- Calcular score final
- Conhecer outros padrões
- Depender de estado global (exceto IGridService)
- Conter lógica de negócio além de geometria

**Princípio:**
> "Padrões dizem 'sou válido?', não 'quanto valho?'"

---

### **PatternMatch - DTO Puro**

**Função:** Transportar resultado de detecção

**Campos essenciais:**
```csharp
public class PatternMatch {
    public string PatternID;        // ID estável (não nome exibido)
    public PatternType Type;        // Enum ou classe
    public List<int> SlotIndices;   // Posições exatas
    public int BaseScore;           // Vindo do Pattern
    public int DaysActive;          // Para decay (futuro)
    // Metadados opcionais para UI/analytics
}
```

**⚠️ Risco de volume:**
- Sistema permite sobreposição livre
- Grid complexo pode gerar 30-40 matches
- **Mitigação futura:** Agrupar matches por tipo para UI (não agora)

**Nota sobre limite prático:**
```
O sistema permite múltiplos PatternMatch sobrepostos.
Se o volume crescer excessivamente no futuro, 
resultados podem ser agregados APÓS detecção,
sem alterar lógica dos padrões.
```

---

## 📂 **ESTRUTURA DE ARQUIVOS**

### **Novos arquivos a criar:**
```
Assets/Scripts/Domain/Patterns/
├── Core/
│   ├── IGridPattern.cs                    ← Interface base
│   ├── PatternMatch.cs                    ← DTO de resultado
│   ├── PatternDetector.cs                 ← Serviço principal
│   └── PatternScoreCalculator.cs          ← Cálculo de pontos
│
├── Implementations/
│   ├── AdjacentPairPattern.cs             ← Padrão #1
│   ├── TrioLinePattern.cs                 ← Padrão #2
│   ├── CornerPattern.cs                   ← Padrão #3
│   ├── FullLinePattern.cs                 ← Padrão #4
│   ├── CheckerPattern.cs                  ← Padrão #5
│   ├── CrossPattern.cs                    ← Padrão #6
│   ├── DiagonalPattern.cs                 ← Padrão #7
│   ├── FramePattern.cs                    ← Padrão #8
│   ├── RainbowLinePattern.cs              ← Padrão #9
│   └── PerfectGridPattern.cs              ← Padrão #10
│
├── Data/
│   └── PatternLibrary.cs                  ← ScriptableObject
│
└── Events/
    └── PatternEvents.cs                   ← Event bus

Assets/Scripts/Infrastructure/Events/
└── PatternEvents.cs                       ← Adicionar a GameEvents

Assets/Documentation/
└── PATTERN_SYSTEM_DESIGN.md              ← Este arquivo
```

---

## 🔧 **SCRIPTS A MODIFICAR**

### **1. GameEvents.cs**
```csharp
public class GameEvents
{
    public GridEvents Grid { get; private set; }
    public PatternEvents Pattern { get; private set; } // ← NOVO
    // ... resto
    
    public GameEvents()
    {
        Grid = new GridEvents();
        Pattern = new PatternEvents(); // ← NOVO
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
    
    // ⚠️ CRÍTICO: Usa PatternID estável (não nome exibido)
    // PatternID = identificador único definido no PatternLibrary
    // Exemplos: "FULL_LINE", "FRAME", "PERFECT_GRID"
    // Strings humanas (ex: "Linha Completa") ficam só para UI
    public Dictionary<string, int> PatternCompletionCount; 
    
    // FUTURO: Tracking de decay (opcional)
    // public Dictionary<string, PatternInstanceData> ActivePatterns;
}
```

**⚠️ Nota Arquitetural - SaveData:**

Nunca use strings "humanas" ou nomes de classe diretamente.

**❌ ERRADO:**
```csharp
PatternCompletionCount["Linha Completa"]++; // Nome exibido (muda com i18n)
PatternCompletionCount["FullLinePattern"]++; // Nome de classe (refactor quebra)
```

**✅ CORRETO:**
```csharp
PatternCompletionCount["FULL_LINE"]++; // ID estável do ScriptableObject
```

**Benefícios:**
- Refactor-safe (renomear classe não quebra save)
- Localização-safe (traduzir nome não quebra)
- Debug-friendly (IDs legíveis)

---

## 📊 **DADOS DE EXEMPLO**

### **Exemplo 1: Grid Casual**
```
DIA 3:
[🥕][🥕][🥕][  ][  ]  ← Trio em Linha (10 pts)
[🥕][  ][  ][  ][  ]
[  ][  ][  ][  ][  ]
[🌽][🌽][  ][  ][  ]  ← Par Adjacente (5 pts)
[  ][  ][  ][  ][  ]

PADRÕES DETECTADOS:
✅ Trio em Linha (Carrots) → 10 pts
✅ Par Adjacente (Corn) → 5 pts

TOTAL: 15 pontos de padrões
```

### **Exemplo 2: Grid Otimizado**
```
DIA 7:
[🥕][🥕][🥕][🥕][🥕]  ← Linha Completa (25 pts)
[🥕][  ][  ][  ][🥕]  ← Moldura (50 pts)
[🥕][  ][  ][  ][🥕]
[🥕][  ][  ][  ][🥕]
[🥕][🥕][🥕][🥕][🥕]

PADRÕES DETECTADOS:
✅ Linha Completa (Row 0) → 25 pts
✅ Linha Completa (Row 4) → 25 pts
✅ Moldura → 50 pts

SINERGIA: 3 padrões = 1.2x multiplier
TOTAL: 100 × 1.2 = 120 pontos!
```

### **Exemplo 3: Grid Perfeito**
```
DIA 14 (Boss):
[🥕][🌽][🥕][🌽][🥕]
[🌽][🥕][🌽][🥕][🌽]
[🥕][🌽][🥕][🌽][🥕]
[🌽][🥕][🌽][🥕][🌽]
[🥕][🌽][🥕][🌽][🥕]

PADRÕES DETECTADOS:
✅ Xadrez 2x2 (múltiplos) → 80 pts
✅ Grid Perfeito → 150 pts
✅ Linha Arco-íris (várias) → 165 pts

SINERGIA: 10+ padrões = 1.9x
TOTAL: 395 × 1.9 = 750 pontos!!!
```

---

## 🎮 **INTEGRAÇÃO COM GAMEPLAY**

### **Papel do Pattern System no DailyResolution**

**🎯 CONCEITO ARQUITETURAL FUNDAMENTAL:**

O Pattern System representa o **resultado principal** da resolução diária.
Sistemas como crescimento, murchamento e eventos **preparam o estado do grid** para avaliação de padrões.

**Hierarquia conceitual:**
```
DailyResolutionSystem
 ├─ PrepareGrid        (grow, wither, eventos)  ← Prepara o palco
 ├─ EvaluatePatterns   (detect + calculate)     ← ⭐ PROTAGONISTA
 ├─ ApplyConsequences  (meta, score, lives)     ← Aplica resultados
 └─ EmitResults        (eventos, UI, save)      ← Feedback
```

**Por que Pattern é protagonista:**
- Pontuação principal vem de padrões
- Harvest é **alavanca** (dinheiro), não reward principal
- Crescimento lento torna padrões o foco natural
- Meta diária é batida primariamente via padrões

**⚠️ Implicação para features futuras:**
```
Eventos aleatórios → devem afetar PADRÕES (não harvest direto)
Clima/Estações → modificam detecção ou score de padrões
Buffs/Tradições → amplificam padrões, não substituem
```

**Filosofia de design:**
> "Outros sistemas são satélites orbitando Pattern System."

Mas atenção: Pattern System **não pode engolir o jogo inteiro** (ver seção de Riscos).

---

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
- Preciso dinheiro? → Colho tudo
- Preciso pontos? → Deixo grid perfeito
- Mix? → Colho harvest, deixo padrões
```

**⚠️ Risco de balanceamento:**

Se harvest ficar "trivial" ou "só quando preciso", o jogo perde tensão.

**O que mantém harvest relevante:**
- Não pontos → mas **acesso** (cartas, desbloqueios, emergências)
- Harvest é **alavanca**, não reward
- Dinheiro permite **correção de erro** (comprar carta que faltava)
- Harvest é **válvula de alívio** quando padrões desabam

**Equilíbrio saudável:**
```
Padrões = estratégia de longo prazo (dias)
Harvest = tática de curto prazo (dinheiro agora)
Ambos necessários, nenhum dominante sozinho
```

---

## ⚠️ **ANTI-PATTERNS E RISCOS ARQUITETURAIS**

### **🚫 ANTI-PATTERNS A EVITAR (Lista de Proteção)**

Estas práticas **NUNCA** devem ser permitidas no sistema:

1. **❌ Padrões contendo lógica de pontuação complexa**
   ```csharp
   // ERRADO - Pattern calculando score
   public int GetScore() {
       return IsMature ? 50 : 25;
   }
   
   // CERTO - Pattern apenas declara base
   public int BaseScore => 25;
   ```

2. **❌ PatternDetector decidindo decay ou sinergia**
   ```csharp
   // ERRADO - Detector virando Deus Objeto
   if (match.DaysActive > 3) match.Score *= 0.7f;
   
   // CERTO - Detector apenas detecta
   return matches; // Calculator lida com decay
   ```

3. **❌ Harvest restaurando padrões no mesmo dia**
   ```csharp
   // ERRADO - Exploit de replante grátis
   OnHarvest() { ReplantSameSpot(); CountAsOldPattern(); }
   
   // CERTO - Colheita quebra padrão
   OnHarvest() { slot.Clear(); /* novo padrão só amanhã */ }
   ```

4. **❌ Dependência de strings humanas em SaveData**
   ```csharp
   // ERRADO - Nome exibido ou classe
   data["Linha Completa"]++; // muda com localização
   data["FullLinePattern"]++; // quebra com refactor
   
   // CERTO - ID estável
   data["FULL_LINE"]++; // definido no ScriptableObject
   ```

5. **❌ Eventos dirigindo lógica de jogo**
   ```csharp
   // ERRADO - Decisão depende de listener
   OnPatternDetected += (p) => { GameLogic.DoSomething(); }
   
   // CERTO - Eventos apenas observam
   OnPatternDetected += (p) => { UI.ShowPopup(); }
   ```

6. **❌ IGridPattern conhecendo outros padrões**
   ```csharp
   // ERRADO - Acoplamento entre padrões
   if (grid.HasFramePattern()) this.Bonus *= 2;
   
   // CERTO - Padrões são independentes
   return TryDetect() ? new Match() : null;
   ```

7. **❌ PatternMatch contendo lógica**
   ```csharp
   // ERRADO - DTO com comportamento
   public int CalculateFinalScore() { /* lógica */ }
   
   // CERTO - DTO puro
   public int BaseScore { get; set; } // apenas dados
   ```

---

### **⚠️ RISCOS ARQUITETURAIS (Monitoramento Contínuo)**

#### **🔴 RISCO CRÍTICO: Identidade de Padrão Mal Definida**

**Onde quebra:** Tracking de `DaysActive`, reset semanal, recriação

**Sintoma:** Decay não reseta quando deveria, ou reseta quando não deveria

**Mitigação aplicada:**
- Definição formal: PatternInstanceID = Hash(Type + Slots + Crops)
- Qualquer mudança = novo padrão (sem lógica "criativa")
- Implementação determinística obrigatória

**Monitorar:**
- Se surgir "padrão similar" ou "quase igual"
- Se houver tentação de "reusar" padrão parcialmente

---

#### **🟡 RISCO MÉDIO: PatternDetector vira Deus Objeto**

**Onde quebra:** Quando começar a adicionar padrões condicionais, tradições

**Sintoma:** Detector com 500+ linhas, múltiplas responsabilidades

**Mitigação aplicada:**
- Detector é stateless e burro
- Cada IGridPattern é independente
- Coordenação futura = PatternPostProcessor separado

**Monitorar:**
- Linhas de código no Detector
- Se surgir `if (season == X)` dentro do Detector
- Se Detector começar a "decidir" ao invés de "coletar"

---

#### **🟡 RISCO MÉDIO: Explosão Combinatória (Sobreposição Livre)**

**Onde quebra:** Grid complexo gerando 30-40 matches, UI poluída

**Sintoma:** Logs imensos, balance tuning impossível, performance

**Mitigação aplicada:**
- Soft cap logarítmico na sinergia
- Sobreposição livre permanece (é feature)
- Porta aberta para agrupamento futuro

**Monitorar:**
- Média de matches por dia (analytics)
- Reclamações de UI "poluída"
- Se tuning virar "jogo de adivinhação"

**Solução futura (não agora):**
```csharp
// Agrupar matches do mesmo tipo para exibição
PatternGroupResult = List<PatternMatch>.GroupBy(m => m.Type);
```

---

#### **🟡 RISCO MÉDIO: Fórmula de Score Espalhada**

**Onde quebra:** Ninguém sabe mais onde mexer para balancear

**Sintoma:** "Arco-íris está fraco, mas onde mexo?"

**Mitigação aplicada:**
- TODA matemática no PatternScoreCalculator
- Patterns só têm BaseScore (int fixo)
- Casos especiais documentados explicitamente

**Monitorar:**
- Se IGridPattern começar a ter `if/else` de score
- Se surgir "mini-calculadora" dentro de Pattern
- Se tuning exigir mexer em múltiplos arquivos

---

#### **🟢 RISCO BAIXO: Eventos Demais Cedo Demais**

**Onde quebra:** UI, Analytics, Achievements, Debug tools todos acoplados

**Sintoma:** "Não posso mudar isso porque quebra três sistemas"

**Mitigação aplicada:**
- Eventos apenas para observação
- Lógica de jogo nunca depende de evento ter sido ouvido
- Nenhuma decisão crítica via eventos

**Monitorar:**
- Se surgir `if (eventFired)` em lógica de jogo
- Se evento começar a "orquestrar" fluxo
- Se remover listener quebrar funcionalidade

---

#### **🟢 RISCO BAIXO: DailyResolution Pesado Demais**

**Onde quebra:** Arquivo com 1000+ linhas, responsabilidades cruzadas

**Sintoma:** Difícil testar, difícil debugar, bugs em cascata

**Mitigação aplicada:**
- Arquitetura pipeline (etapas isoladas)
- Pattern é protagonista, mas não engole tudo
- Cada etapa claramente demarcada

**Monitorar:**
- Linhas de código no arquivo
- Se etapas começarem a "conversar" diretamente
- Se adicionar feature exigir mexer em múltiplas etapas

---

#### **🔴 RISCO CONCEITUAL: Sistema Bom Demais**

**Onde quebra:** Pattern System engole o resto do jogo

**Sintoma:** Harvest vira irrelevante, outros sistemas "orbitam" padrões

**Mitigação aplicada:**
- Harvest é alavanca (acesso), não reward
- Crescimento lento mantém tensão
- Meta = padrões + harvest (balanceado)

**Monitorar:**
- Taxa de uso de Harvest (analytics)
- Feedback: "Só jogo para padrões"
- Se outros sistemas começarem a depender de Pattern

**Filosofia de proteção:**
> "Pattern System é core isolado. Nenhum sistema ASSUME que ele existe. Ele soma pontos, não define vitória sozinho."

---

### **📋 CHECKLIST DE PROTEÇÃO (Code Review)**

Ao revisar código do Pattern System, sempre checar:

- [ ] Patterns contêm apenas geometria + BaseScore fixo?
- [ ] Calculator centraliza TODA matemática?
- [ ] Detector é stateless e burro?
- [ ] SaveData usa IDs estáveis (não strings humanas)?
- [ ] Eventos são observação, não orquestração?
- [ ] Identidade de padrão é determinística?
- [ ] Crescimento lento está preservado?
- [ ] Harvest permanece relevante (não trivial)?

---

## 🧪 **CASOS DE TESTE**

### **Teste 1: Par Simples**
```
Input:
[🥕][🥕][  ][  ][  ]

Expected:
✅ AdjacentPairPattern detected
   Slots: [0, 1]
   Score: 5 pts
```

### **Teste 2: Linha com Locked**
```
Input:
[🥕][🥕][🔒][🥕][🥕]

Expected:
✅ TrioLinePattern detected (slots 0,1 ignoram locked)
❌ NÃO é FullLinePattern (locked quebra)
```

### **Teste 3: Linha com Morta**
```
Input:
[🥕][🥕][💀][🥕][🥕]

Expected:
❌ NENHUM padrão (morta quebra tudo)
```

### **Teste 4: Sobreposição**
```
Input:
[🥕][🥕][🥕]
[🥕][  ][  ]
[🥕][  ][  ]

Expected:
✅ TrioLinePattern (row 0) → 10 pts
✅ TrioLinePattern (col 0) → 10 pts
✅ CornerPattern (L shape) → 8 pts
TOTAL: 28 pts (sobreposição OK!)
```

---

## 🌊 **PLANO DE IMPLEMENTAÇÃO EM ONDAS**

### **📋 FILOSOFIA: Sprint Jogável End-to-End**

Cada onda representa um **sprint completo e funcional**. Se precisar parar, o sistema está **sempre jogável**.

**Regra de Ouro:**
> "Nunca termine uma onda com o jogo quebrado. Cada onda adiciona features, não conserta features."

---

## 🌊 **ONDA 1: MVP FUNCIONAL (Sprint 1 - ~3-5 dias)**

### **🏷️ STATUS: ✅ COMPLETA (2025-01-XX)**

**Branch:** `feature/pattern-system-wave-1`  
**Tag:** `wave-1-complete`  
**Commit:** `feat(patterns): Core infrastructure + 5 padrões (Onda 1)`

### **🎯 Objetivo:**
Sistema de detecção básico funcionando **sem decay**, com **5 padrões Tier 1-2**, integrado ao pipeline de resolução diária.

### **✅ Entregáveis:**

#### **1.1 - Core Infrastructure**
- [x] `IGridPattern.cs` (interface base)
- [x] `PatternMatch.cs` (DTO simples, sem DaysActive ainda)
- [x] `PatternDetector.cs` (stateless, hardcoded patterns)
- [x] `PatternScoreCalculator.cs` (sem decay, fórmula básica)
- [x] `PatternEvents.cs` (event bus)
- [x] `PatternHelper.cs` (utilitários de navegação 2D) ← ADICIONADO

#### **1.2 - GameEvents Integration**
- [x] Adicionar `PatternEvents` em `GameEvents.cs`
- [x] Testar evento `OnPatternDetected`

#### **1.3 - 5 Padrões Essenciais (Tier 1-2)**
- [x] `AdjacentPairPattern.cs` (Par Adjacente - 5 pts)
- [x] `TrioLinePattern.cs` (Trio em Linha - 10 pts)
- [x] `GridCornerPattern.cs` (Cantinho - 8 pts) ← Renomeado para evitar conflito
- [x] `FullLinePattern.cs` (Linha Completa - 25 pts)
- [x] `GridCrossPattern.cs` (Cruz Simples - 30 pts) ← Renomeado para evitar conflito

**NOTA:** `CornerPattern` e `CrossPattern` foram renomeados para `GridCornerPattern` e `GridCrossPattern` para evitar conflito com classes existentes em `UnlockPatterns/`.

**Por que esses 5?**
- Cobrem range de dificuldade (fácil → médio)
- Testam geometrias diferentes (adjacência, linha, cruz)
- Permitem validar sobreposição
- Score variado (5-30 pts) para testar cálculo

#### **1.4 - DailyResolution Integration (CRÍTICO)**
- [x] Criar `DetectPatternsStep.cs` (novo IFlowStep)
- [x] Adicionar ao pipeline APÓS `GrowGridStep`
- [x] Testar ordem: Grow → Detect → Score → Advance

#### **1.5 - AppCore Setup**
- [x] Adicionar `PatternDetector` e `PatternScoreCalculator` ao AppCore
- [x] Propriedades públicas para acesso

#### **1.6 - Logs de Debug**
- [x] Log cada padrão detectado (tipo, slots, score)
- [x] Log score final com breakdown
- [x] Log resumo agrupado por tipo

#### **1.7 - BONUS (Adiantado da Onda 2)**
- [x] Crop Value multiplier implementado
- [x] Maturity bonus implementado
- [x] Soft Cap de Sinergia logarítmica implementado

### **🧪 Resultados do Playtest (10 dias):**
```
[PatternScoreCalculator] Sinergia (27 padrões): 1,95x
[PatternScoreCalculator] === TOTAL: 728 pontos de padrões ===
[DetectPatternsStep] Score semanal: 871 + 728 = 1599

RESUMO DE PADRÕES:
  ✓ 15x Par Adjacentes
  ✓ 8x Trio em Linhas
  ✓ 1x Cantinho
  ✓ 2x Linha Completas
  ✓ 1x Cruz Simples
```

### **📁 Arquivos Criados:**
```
Assets/Scripts/Domain/Patterns/
├── Core/
│   ├── IGridPattern.cs
│   ├── PatternMatch.cs
│   ├── PatternHelper.cs
│   ├── PatternDetector.cs
│   └── PatternScoreCalculator.cs
├── Implementations/
│   ├── AdjacentPairPattern.cs
│   ├── TrioLinePattern.cs
│   ├── GridCornerPattern.cs (CornerPattern.cs)
│   ├── FullLinePattern.cs
│   └── GridCrossPattern.cs (CrossPattern.cs)
└── Events/
    └── PatternEvents.cs

Assets/Scripts/Flow/Steps/
└── DetectPatternsStep.cs
```

### **📁 Arquivos Modificados:**
- `Assets/Scripts/Infrastructure/Events/GameEvents.cs` - Adicionado PatternEvents
- `Assets/Scripts/App/AppCore.cs` - Adicionado PatternDetector + PatternCalculator
- `Assets/Scripts/Progression/DailyResolutionSystem.cs` - Adicionado DetectPatternsStep ao pipeline

---

**Detalhes do DetectPatternsStep:**
```csharp
/// <summary>
/// IFlowStep que detecta padrões no grid e adiciona pontos à meta semanal.
/// 
/// POSIÇÃO NO PIPELINE:
/// 1. GrowGridStep (plantas crescem/murcham)
/// 2. DetectPatternsStep ← AQUI (avalia grid final)
/// 3. CalculateScoreStep (aplica meta + patterns)
/// 4. AdvanceTimeStep
/// 5. DailyDrawStep
/// 
/// RESPONSABILIDADES:
/// - Chamar PatternDetector.DetectAll()
/// - Chamar PatternScoreCalculator.CalculateTotal()
/// - Adicionar pontos ao RunData.CurrentWeeklyScore
/// - Emitir evento OnPatternDetected
/// - Logs de debug verbosos
/// </summary>
public class DetectPatternsStep : IFlowStep
{
    private readonly IGridService _gridService;
    private readonly PatternDetector _detector;
    private readonly PatternScoreCalculator _calculator;
    private readonly RunData _runData;
    private readonly GameEvents _events;

    public IEnumerator Execute(FlowControl control)
    {
        Debug.Log("[DetectPatternsStep] Iniciando detecção de padrões...");
        
        // 1. Detectar padrões
        var matches = _detector.DetectAll(_gridService);
        Debug.Log($"[DetectPatternsStep] {matches.Count} padrões detectados");
        
        // 2. Calcular pontos
        int points = _calculator.CalculateTotal(matches, _gridService);
        Debug.Log($"[DetectPatternsStep] Total de pontos: {points}");
        
        // 3. Adicionar à meta
        _runData.CurrentWeeklyScore += points;
        
        // 4. Emitir evento (UI pode reagir)
        _events.Pattern.TriggerPatternsDetected(matches, points);
        
        // 5. Delay visual (opcional)
        yield return new WaitForSeconds(0.3f);
    }
}
```

#### **1.5 - AppCore Setup**
- [ ] Adicionar `PatternDetector` e `PatternScoreCalculator` ao AppCore
- [ ] **NÃO adicionar PatternLibrary SO ainda** (hardcode patterns)
- [ ] Propriedades públicas para acesso

#### **1.6 - Logs de Debug**
- [ ] Log cada padrão detectado (tipo, slots, score)
- [ ] Log score final com breakdown
- [ ] Log de erro se grid inválido

### **🧪 Critérios de Aceitação (Onda 1):**
```
✅ Jogar 1 dia completo sem erros
✅ Ver logs de padrões detectados no Console
✅ Score aumenta corretamente após "Sleep"
✅ 5 padrões funcionando (testar cada um)
✅ Sobreposição funciona (ex: Trio + Linha)
✅ Locked/withered quebram padrões corretamente
✅ Nenhuma exceção no Console
```

### **📊 Resultado Esperado:**
```
Dia 1:
[🥕][🥕][🥕][  ][  ]  ← Trio (10 pts)
[🥕][  ][  ][  ][  ]  ← Par vertical (5 pts)
[  ][  ][  ][  ][  ]

Console:
[DetectPatternsStep] 2 padrões detectados
- Trio em Linha (Row 0) → 10 pts
- Par Adjacente (Col 0) → 5 pts
[DetectPatternsStep] Total: 15 pts
```

---

## 🌊 **ONDA 2: FEEDBACK VISUAL + TIER 2 COMPLETO (Sprint 2 - ~2-3 dias)**

### **🏷️ STATUS: ⏳ EM PROGRESSO**

### **🎯 Objetivo:**
Adicionar **padrões restantes Tier 2** + **UI básica** (logs visuais, toast notification)

### **✅ Entregáveis:**

#### **2.1 - Padrões Tier 2 Restantes**
- [ ] `CheckerPattern.cs` (Xadrez 2x2 - 20 pts)

#### **2.2 - UI Básica (Sem PopUp Complexo)**
- [ ] Toast notification "Pattern Detected!" (fade out)
- [ ] Adicionar score de patterns no HUD (separado de harvest)
- [ ] Highlight temporário de slots (opcional)

**⚠️ Nota sobre UI:**
> UI complexa (popup de padrões, tabela completa, animações) será documentada em **arquivo separado** (`PATTERN_UI_DESIGN.md`). Por enquanto, apenas feedback mínimo.

#### **2.3 - Refinamento de Score** ✅ JÁ IMPLEMENTADO NA ONDA 1
- [x] Implementar formula de Crop Value ← Feito na Onda 1
- [x] Implementar bonus de Maturity ← Feito na Onda 1
- [x] Implementar Soft Cap de Sinergia ← Feito na Onda 1

### **🧪 Critérios de Aceitação (Onda 2):**
```
✅ 6 padrões funcionando (Tier 1 + Tier 2 completo)
✅ UI mostra "15 pts de padrões!" após Sleep
✅ Fórmula de score com crop value/maturity funciona
✅ Sinergia logarítmica aplicada corretamente
✅ Jogar 3 dias consecutivos sem bugs
```

---

## 🌊 **ONDA 3: PADRÕES AVANÇADOS (Sprint 3 - ~2-3 dias)**

### **🎯 Objetivo:**
Adicionar **Tier 3 e Tier 4** (padrões complexos)

### **✅ Entregáveis:**

#### **3.1 - Tier 3**
- [ ] `DiagonalPattern.cs` (Diagonal - 40 pts)
- [ ] `FramePattern.cs` (Moldura - 50 pts)
- [ ] `RainbowLinePattern.cs` (Arco-íris - 55 pts)

#### **3.2 - Tier 4**
- [ ] `PerfectGridPattern.cs` (Grid Perfeito - 150 pts)

#### **3.3 - Casos Especiais**
- [ ] Fórmula custom para Arco-íris (diversidade)
- [ ] Fórmula custom para Grid Perfeito
- [ ] Validação de mínimo 4 tipos diferentes (Grid Perfeito)

### **🧪 Critérios de Aceitação (Onda 3):**
```
✅ 10 padrões funcionando (todos os tiers)
✅ Arco-íris detecta diversidade corretamente
✅ Grid Perfeito valida 25 slots + 4 tipos
✅ Score de 500+ pontos é possível (teste stress)
✅ Nenhuma explosão numérica (soft cap funciona)
```

---

## 🌊 **ONDA 4: PERSISTÊNCIA + DECAY (Sprint 4 - ~3-4 dias)**

### **🎯 Objetivo:**
Adicionar **tracking de padrões** + **sistema de decay**

### **✅ Entregáveis:**

#### **4.1 - PatternInstanceID**
- [ ] Implementar Hash(Type + Slots + Crops)
- [ ] Comparação determinística

#### **4.2 - RunData Tracking**
- [ ] `TotalPatternsCompleted`
- [ ] `HighestDailyPatternScore`
- [ ] `Dictionary<string, int> PatternCompletionCount`
- [ ] `Dictionary<string, PatternInstanceData> ActivePatterns` (novo)

#### **4.3 - Decay System**
- [ ] Campo `DaysActive` em `PatternMatch`
- [ ] Aplicar decay no Calculator (-10% por dia)
- [ ] Reset semanal (lógica em `AdvanceTimeStep` ou novo step)
- [ ] Bonus pós-reset (+10% no primeiro dia)

#### **4.4 - SaveData Persistence**
- [ ] Salvar padrões ativos entre sessões
- [ ] Carregar e restaurar decay state
- [ ] Validar compatibilidade de save

### **🧪 Critérios de Aceitação (Onda 4):**
```
✅ Padrão mantido por 3 dias decai corretamente (100% → 90% → 80%)
✅ Colher/replantar reseta decay (novo padrão)
✅ Reset semanal funciona
✅ Bonus pós-reset aplicado
✅ Save/Load preserva decay state
✅ PatternInstanceID é único e determinístico
```

---

## 🌊 **ONDA 5: POLISH + SCRIPTABLE OBJECT (Sprint 5 - ~2 dias)**

### **🎯 Objetivo:**
Migrar patterns hardcoded para **ScriptableObject** + polish final

### **✅ Entregáveis:**

#### **5.1 - PatternLibrary SO**
- [ ] Criar `PatternLibrary.cs` (ScriptableObject)
- [ ] Migrar 10 padrões para asset
- [ ] Configurar IDs estáveis (ex: "FULL_LINE")
- [ ] Configurar nomes exibidos (para UI)

#### **5.2 - AppCore Integration**
- [ ] Injetar `PatternLibrary` no `PatternDetector`
- [ ] Remover patterns hardcoded

#### **5.3 - Polish**
- [ ] Tuning de valores (playtesting)
- [ ] Ajustar soft cap se necessário
- [ ] Refinar logs de debug
- [ ] Adicionar comentários inline faltantes

### **🧪 Critérios de Aceitação (Onda 5):**
```
✅ PatternLibrary asset configurado no Inspector
✅ Renomear pattern via SO não quebra código
✅ Adicionar novo padrão = só criar SO entry
✅ Balance de pontos está coerente
✅ Code review com checklist de Anti-Patterns
✅ Documentação inline completa
```

---

## 📊 **RESUMO DAS ONDAS**

| Onda | Duração | Entregável Principal | Status no Final |
|------|---------|---------------------|-----------------|
| **1** | 3-5 dias | MVP com 5 padrões, sem decay, sem UI | ✅ Jogável |
| **2** | 2-3 dias | 6 padrões + UI básica + score final | ✅ Jogável |
| **3** | 2-3 dias | 10 padrões (todos os tiers) | ✅ Jogável |
| **4** | 3-4 dias | Decay + persistência | ✅ Jogável |
| **5** | 2 dias | ScriptableObject + polish | ✅ Completo |

**Total estimado:** 12-17 dias (2-3 semanas)

---

## 🚨 **REGRAS DE TRANSIÇÃO ENTRE ONDAS**

### **Antes de avançar para próxima onda:**

1. ✅ **Build compila sem erros**
2. ✅ **Jogar 1 run completo (7 dias) sem crashes**
3. ✅ **Logs não mostram exceções**
4. ✅ **Critérios de aceitação da onda atual = OK**
5. ✅ **Commit no Git** (`feat: Onda X completa`)

**Se algo quebrar na Onda N, NUNCA corrija na Onda N+1.**  
Volte, conserte, valide, e só então avance.

---

## 📝 **CHECKLIST COMPLETO (Referência Detalhada)**

### **Onda 1: MVP Funcional**
- [ ] `IGridPattern.cs`
- [ ] `PatternMatch.cs` (sem DaysActive)
- [ ] `PatternDetector.cs` (hardcoded, stateless)
- [ ] `PatternScoreCalculator.cs` (sem decay)
- [ ] `PatternEvents.cs`
- [ ] Integrar `PatternEvents` em `GameEvents.cs`
- [ ] `AdjacentPairPattern.cs`
- [ ] `TrioLinePattern.cs`
- [ ] `CornerPattern.cs`
- [ ] `FullLinePattern.cs`
- [ ] `CrossPattern.cs`
- [ ] `DetectPatternsStep.cs` (IFlowStep)
- [ ] Adicionar step ao pipeline em `DailyResolutionSystem`
- [ ] Modificar `AppCore.cs` (adicionar Detector + Calculator)
- [ ] Logs de debug verbosos
- [ ] Testar 5 padrões individualmente
- [ ] Testar sobreposição
- [ ] Testar locked/withered
- [ ] Jogar 1 dia completo sem erros

### **Onda 2: Feedback + Tier 2**
- [ ] `CheckerPattern.cs`
- [ ] Toast notification "Pattern Detected!"
- [ ] HUD mostra score de patterns separado
- [ ] Implementar Crop Value multiplier
- [ ] Implementar Maturity bonus
- [ ] Implementar Soft Cap de Sinergia
- [ ] Testar fórmula de score completa
- [ ] Jogar 3 dias consecutivos

### **Onda 3: Padrões Avançados**
- [ ] `DiagonalPattern.cs`
- [ ] `FramePattern.cs`
- [ ] `RainbowLinePattern.cs`
- [ ] `PerfectGridPattern.cs`
- [ ] Fórmula custom para Arco-íris
- [ ] Fórmula custom para Grid Perfeito
- [ ] Validação de 4 tipos (Grid Perfeito)
- [ ] Teste stress (500+ pontos)

### **Onda 4: Decay + Persistência**
- [ ] PatternInstanceID (Hash)
- [ ] Campo `DaysActive` em PatternMatch
- [ ] Tracking em RunData
- [ ] Decay aplicado no Calculator
- [ ] Reset semanal
- [ ] Bonus pós-reset
- [ ] Save/Load de decay state
- [ ] Testar decay por 3 dias
- [ ] Testar reset semanal

### **Onda 5: ScriptableObject + Polish**
- [ ] `PatternLibrary.cs` (SO)
- [ ] Criar asset `PatternLibrary.asset`
- [ ] Configurar 10 padrões
- [ ] IDs estáveis (ex: "FULL_LINE")
- [ ] Injetar no PatternDetector
- [ ] Remover hardcode
- [ ] Tuning de valores
- [ ] Code review com checklist
- [ ] Documentação inline completa

---

## 🚀 **PRÓXIMOS PASSOS (APÓS ONDA 5 - Futuro)**

### **UI de Padrões Avançada (Sprint 6 - Separado)**

**⚠️ IMPORTANTE:** UI complexa será documentada em arquivo separado.

**Criar:** `Assets/Documentation/PATTERN_UI_DESIGN.md`

**Conteúdo sugerido:**
- Popup detalhado mostrando padrões detectados
- Tabela in-game com todos os 10 padrões + progresso
- Highlight visual animado dos slots que formam padrão
- Animação "Pattern Completed!" com juicy effects
- Particle effects por tier (bronze/prata/ouro/diamante)
- Breakdown de score (tooltip mostrando cálculo)
- História de padrões (últimos 7 dias)
- Indicador visual de decay (barra amarela → vermelha)

**Mockups/References:**
- Balatro (popup de mãos de poker)
- Slay the Spire (card rewards screen)
- Inscryption (pattern recognition feedback)

**Razão da separação:**
- UI não afeta lógica de jogo
- Pode ser iterada independentemente
- Designer (você do futuro) pode prototipar sem quebrar backend
- Sprint de UI pode ser mais longo (polish visual demora)

---

### **Tradições (Modificadores) - Sprint 7+**
- Sistema de buffs estilo Balatro Jokers
- "Mestre da Moldura" → Molduras valem 2x
- "Arco-íris Divino" → Linha diversa +100%
- "Agricultor Paciente" → Decay -5% ao invés de -10%
- Escolher 3 tradições ativas por run
- Tradições desbloqueadas por achievements

---

### **Stats & Meta - Sprint 8+**
- Tracking de padrões completados
- Achievement "Complete todos os 10 em 1 dia"
- Daily challenge "Complete 5 Molduras"
- Leaderboard (padrões mais raros)
- Stats screen no menu

---

## 💬 **NOTAS PARA O PRÓXIMO CHAT (Claude Opus)**

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
- `AppCore.Instance.Events.Pattern` → Novo event bus
- Eventos: `OnPatternDetected`, `OnScoreCalculated`
- Sempre disparar eventos para UI reagir
- **REGRA:** Eventos são observação, não orquestração

### **Integração com Existente**
- `DailyResolutionSystem` já existe e funciona
- `GridService` expõe `GetSlotReadOnly(index)`
- `IGridService.Config` tem Rows/Columns
- `GameLibrary` tem `TryGetCrop(cropID, out data)`
- **CropLogic** tem crescimento lento (3-4 dias até maturidade)

### **O que NÃO mudar**
- Grid visual (acabamos de refatorar!)
- Sistema de cartas (funciona perfeitamente)
- DailyResolution flow (só adicionar detecção no meio)
- **Crescimento lento de plantas** (pilar do Pattern System)

### **⚠️ DIRETRIZES CRÍTICAS DE IMPLEMENTAÇÃO**

1. **Identidade de Padrão:**
   - PatternInstanceID = Hash(PatternType + SlotIndices + CropIDs)
   - Qualquer mudança = novo padrão (sem lógica criativa)

2. **Responsabilidades Fixas:**
   - **PatternDetector:** stateless, apenas coleta matches
   - **PatternScoreCalculator:** TODA matemática de pontuação
   - **IGridPattern:** geometria + BaseScore fixo (sem lógica complexa)

3. **SaveData:**
   - Usar IDs estáveis (ex: "FULL_LINE")
   - NUNCA usar nomes exibidos ou nomes de classe

4. **Eventos:**
   - Apenas para observação (UI, analytics)
   - Lógica de jogo NUNCA depende de eventos

5. **Proteção contra "Sistema Bom Demais":**
   - Pattern System é core, mas não engole o jogo
   - Harvest permanece relevante (alavanca de acesso)
   - Outros sistemas não devem ASSUMIR que Pattern existe

### **Prioridade de Implementação (ATUALIZADO)**

**🌊 ONDA 1 - MVP Funcional (Sprint 1):**
1. Core infrastructure (IGridPattern, PatternMatch, Detector, Calculator)
2. 5 padrões essenciais (Par, Trio, Cantinho, Linha, Cruz)
3. **DetectPatternsStep.cs** (novo IFlowStep - ver Onda 1 para detalhes)
4. Integração com DailyResolution pipeline
5. Logs de debug extensivos
6. **SEM DECAY** (adicionar na Onda 4)
7. **SEM UI complexa** (apenas logs, toast simples na Onda 2)

**🌊 ONDA 2 - Feedback + Tier 2 (Sprint 2):**
1. Completar Tier 2 (Xadrez)
2. UI básica (toast notification, HUD score)
3. Score formula completa (crop value, maturity, sinergia)

**🌊 ONDA 3 - Padrões Avançados (Sprint 3):**
1. Tier 3 (Diagonal, Moldura, Arco-íris)
2. Tier 4 (Grid Perfeito)
3. Fórmulas customizadas

**🌊 ONDA 4 - Decay + Persistência (Sprint 4):**
1. PatternInstanceID (Hash)
2. Tracking de padrões ativos
3. Sistema de decay (-10% por dia)
4. Reset semanal + bonus pós-reset
5. Save/Load de decay state

**🌊 ONDA 5 - ScriptableObject + Polish (Sprint 5):**
1. PatternLibrary SO
2. Migrar patterns hardcoded
3. Tuning de valores
4. Code review final

**📋 Critérios para avançar de onda:**
- ✅ Build compila sem erros
- ✅ Jogar 1 run (7 dias) sem crashes
- ✅ Critérios de aceitação da onda = OK
- ✅ Commit no Git

---

### **🚨 RED FLAGS - Pare e Revise Se:**
- IGridPattern começar a calcular score
- PatternDetector tiver mais de 200 linhas
- Surgir `if (season == X)` dentro de Detector
- SaveData usar strings "Linha Completa"
- Eventos orquestrando lógica de jogo
- Harvest virar "trivial" ou "sempre ignorado"

---

## ✅ **APROVAÇÃO DO DESIGN**

**Designer**: Davi  
**Status**: ✅ APROVADO (Versão 2.0 - Implementação em Ondas)  
**Data**: 2026 
**Próximo**: Implementação no Claude Opus (Onda por Onda)

---

## 📌 **NOTAS FINAIS PARA O IMPLEMENTADOR (Claude Opus)**

### **🎯 Mindset Correto:**

Este documento foi escrito **com carinho** pelo designer. Cada detalhe importa.

**Regras de Ouro:**
1. **Nunca pule ondas** - Cada onda é testável end-to-end
2. **Nunca comprometa a arquitetura** - Atalhos viram dívida técnica
3. **Logs verbosos sempre** - Debug é parte do MVP
4. **UI complexa é Fase 2** - Foco em lógica funcional primeiro
5. **Decay vem depois** - MVP sem decay está OK

---

### **📞 Se Algo Der Errado:**

**Problema:** "Não sei onde adicionar DetectPatternsStep"  
**Solução:** Ver seção "Onda 1 → 1.4 DailyResolution Integration" (código completo fornecido)

**Problema:** "PatternDetector está ficando grande"  
**Solução:** Revise seção "Anti-Patterns" - Detector deve ter <200 linhas, apenas coletar matches

**Problema:** "Padrões não detectam corretamente"  
**Solução:** Teste cada padrão isoladamente antes de integrar. Use logs verbosos em cada TryDetect()

**Problema:** "Score parece errado"  
**Solução:** Logs devem mostrar breakdown completo (base × crop × maturity × sinergia)

**Problema:** "Não sei se posso avançar de onda"  
**Solução:** Critérios de aceitação no final de cada onda são **obrigatórios**. Se não passar, corrija antes de avançar.

**Problema:** "Quero adicionar feature X que não está no documento"  
**Solução:** Documente primeiro, implemente depois. Nunca adicione features "nas coxas".

---

### **💚 Mensagem do Designer:**

Esse sistema foi pensado para ser o **coração do jogo**. Implemente com paciência.

Cada onda é um marco. Comemora quando completar. 🎉

Se precisar adaptar algo, tudo bem - mas **documente o porquê** nos comentários inline.

O documento tem 1500+ linhas porque eu me importei. Espero que você também se importe. 💚

Boa sorte, eu do futuro (ou Claude Opus). Você consegue! 🚀

---

**FIM DO DOCUMENTO**
