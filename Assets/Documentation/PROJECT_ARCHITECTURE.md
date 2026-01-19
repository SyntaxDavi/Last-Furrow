# Last Furrow - Project Architecture Documentation

## 1. Overview
This project uses a layered architecture separating **Data/Domain Logic** from **Unity Views (MonoBehaviours)**. It relies on **Dependency Injection** (manually handled in `AppCore` and Bootstrappers) to connect these layers.

## 2. Core Layers

### A. App / Entry Point
*   **`AppCore.cs`**: The Singleton that lives across scenes. It initializes all global services (`GameLibrary`, `EconomyService`, etc.) and injects dependencies into them.
*   **`GameplayBootstrapper.cs`**: Initializes scene-specific logic when the Game Scene loads (e.g., connecting the Grid to the Run).

### B. Domain (Business Logic)
*   *Rules of the game, independent of visuals.*
*   **`RunManager.cs`**: Manages the lifecycle of a "Run" (Production Week vs Weekend). It holds the `RunData`.
*   **`EconomyService.cs`**: Handles money transactions (`Earn`, `Spend`) and tracks history.
*   **`GridService.cs`**: Pure C# logic for the farming grid. Handles slot states (Empty, Planted, Watered). Grid size is configurable via `GridConfiguration`. **Only processes unlocked slots** during night cycle (locked slots don't participate in scoring/goals).
*   **`DailyHandSystem.cs`**: Logic for drawing cards each day. Handles "Overflow" (selling cards if hand is full).

### C. Data (State)
*   **`RunData.cs`**: The serializable state of the current game (Money, Day, Hand, GridSlots). Includes versioning metadata for grid compatibility.
*   **`GridConfiguration.cs`**: ScriptableObject defining grid structure (Rows, Columns, Initial Unlocked Slots). Changes to this invalidate old saves.
*   **`CardData.cs`**: ScriptableObject defining a card type (Name, Cost, Effect).
*   **`SaveManager.cs`**: Handles saving/loading `RunData` to disk/PlayerPrefs. Validates grid compatibility on load.

### D. Flow (Game Loop)
*   *The game moves through a sequence of "Steps" using Coroutines.*
*   **`IFlowStep`**: Interface for any game phase (e.g., "Draw Cards", "Advance Time", "Show Score").
*   **`DailyDrawStep.cs`**: Tells `DailyHandSystem` to draw cards.
*   **`AdvanceTimeStep.cs`**: Moves the day counter forward and triggers crop growth.
*   **`WeekendFlowController.cs`**: Manages the specific flow during the weekend phase (Shop, Goal Evaluation).

### E. Visuals / UI (Views)
*   *Listens to Events from Domain to update the screen.*
*   **`GridManager.cs`**: Spawns Prefabs for grid slots and updates them when `GridService` changes.
*   **`PlayerEvents.cs`**: A hub for events like "CardAdded", "MoneyChanged". UI listens to this.
*   **`HandOrganizer.cs`**: Manages the visual positioning of cards in the player's hand.

### F. Input Systems (Core/Input)
*   *Sistema modular de input separado por responsabilidade.*
*   **`PlayerInteraction.cs`**: Orquestrador magro que coordena os sistemas abaixo.
*   **`InteractionPolicy.cs`**: Decide O QUE pode ser feito baseado no GameState.
*   **`HoverSystem.cs`**: Detecta hover com prioridade e histerese (sticky hover).
*   **`DragDropSystem.cs`**: Gerencia arrastar e soltar com drop zones.
*   **`ClickSystem.cs`**: Gerencia cliques simples em objetos interativos.

### G. Debug Tools
**CheatManager** (F1 para toggle):
- **Economia**: Add Money (valor customizável)
- **Vidas**: +1 Vida, -1 Vida
- **Grid**: Desbloquear tudo, Limpar, Regar tudo, Amadurecer tudo
- **Cartas**: Spawn específica, Comprar 3 cartas
- **Save**: Deletar & Restart, Salvar agora
- **Arquitetura**: Lateral esquerda, scrollview, apenas Development builds
- **Integração**: Usa eventos para feedback visual automático

## 3. Key Concepts

### Dependency Injection
Dependencies are passed in constructors.
*   *Example*: `DailyHandSystem` asks for `IEconomyService` in its constructor. `AppCore` provides it.

### Event-Driven UI
The UI does not poll data every frame. It waits for events.
*   *Example*: `EconomyService` fires `OnMoneyChanged`. `MoneyView` listens to it and updates the text.

### The "Step" Pattern
The turn logic is not in `Update()`. It is a sequence of coroutines.
*   *Example*: `Start Day` -> `Draw Cards` -> `Player Input` -> `End Day` -> `Grow Crops` -> `Next Day`.

### Interaction Priority
Each `IInteractable` defines its own priority. Higher = "on top".
*   *Convention*: UI = 1000+, Cards = 100, Grid = 0

### Grid Versioning & Save Compatibility
**CRITICAL SYSTEM**: Ensures saves are never corrupted by grid changes.

#### How it Works:
1. **GridConfiguration** generates a structural hash (`GetVersionHash()`) based on:
   - Rows, Columns (dimensions)
   - DefaultUnlockedCoordinates (initial layout)
   - **Excludes**: Visual settings, sprites, effects

2. **RunData** stores `GridConfigVersion` when created
   - This hash is saved with the player's progress
   - Acts as a "fingerprint" of the grid structure used
   - ? **Validation**: CardID creation validates non-empty strings before conversion

3. **SaveManager** validates compatibility on load:
   - Compares `RunData.GridConfigVersion` with current `GridConfiguration.GetVersionHash()`
   - If they don't match: **SAVE IS REJECTED** (no migration)
   - Player must start a new game
   - ? **Architecture**: GridConfiguration is **INJECTED** via `Initialize(GridConfiguration)`, not accessed via `AppCore.Instance`

#### Design Decisions:
- **Policy**: REJECT incompatible saves, no migration
  - *Reason*: Grid changes are structural. Attempting to migrate risks silent corruption.
  - *Trade-off*: Players lose progress if grid changes, but game never breaks.

- **Versioning Strategy**: Automatic hash-based detection
  - *Reason*: Detects changes without requiring manual version increments
  - *Trade-off*: ANY structural change invalidates saves (by design)

- **Dependency Injection**: SaveManager does NOT depend on AppCore.Instance
  - *Reason*: Makes SaveManager testable, reusable, and pure
  - *Implementation*: AppCore injects GridConfiguration during initialization
  - *Benefit*: SaveManager can be unit tested without UnityEngine context

#### When Saves Are Invalidated:
? **SAFE** (Does NOT break saves):
- Changing grid visual sprites
- Adjusting hover feedback
- Modifying card effects
- Balancing changes

? **BREAKS SAVES** (Invalidates old saves):
- Changing grid dimensions (3x3 ? 5x5)
- Modifying default unlocked coordinates
- Adding/removing initial slots

#### GridService Lifecycle:
1. `AppCore.Initialize()`: Creates global services (without GridService)
   - ? SaveManager receives GridConfiguration via dependency injection
2. `CardInteractionBootstrapper.Initialize()`: Sets up card strategies (GridService = null initially)
3. `GameplayBootstrapper.Awake()`: Creates `GridService` for current scene
4. `AppCore.RegisterGridService()`: Injects GridService into runtime context
5. `CardInteractionBootstrapper.SetGridService()`: **EARLY FAIL** validation
   - Validates all strategies can be resolved
   - If validation fails, throws exception BEFORE gameplay starts
   - Prevents NullReferenceException during player actions

#### Night Cycle Processing (End of Day):
**POLICY**: Only **unlocked slots** are processed during night cycle.

**Reasons:**
1. **Scoring/Goals**: Locked slots don't contribute to meta/score calculation
2. **State Guarantee**: Locked slots are always empty (no plants, no water)
3. **Performance**: Avoids processing inactive slots
4. **Visual Feedback**: Players only see analysis of active slots

**Implementation:**
- `GrowGridStep`: Skips locked slots in main loop (doesn't trigger events)
- `GridService.ProcessNightCycleForSlot()`: Early exit if slot is locked (defense in depth)
- Visual feedback (`TriggerAnalyzeSlot`) only fires for unlocked slots

**Example:**
```
Grid 5x5 (25 slots total)
9 unlocked (3x3 center)
16 locked (border)

Night Cycle Processes: 9 slots (36% of total)
Night Cycle Skips: 16 slots (64% of total)
```

#### Unlock Pattern System (Initial Grid Setup):
**ARCHITECTURE**: Extensible pattern-based unlock using Strategy Pattern + Factory.

**Core Components:**
1. **IUnlockPattern** (Interface):
   - Defines contract for generating unlock coordinates
   - Each pattern implements `Generate(width, height, slotCount, rng)`
   - Open/Closed Principle: Add new patterns without modifying existing code

2. **8 Built-in Patterns:**
   - `CrossPattern`: Cruz (+) centralizada
   - `LinePattern`: Linha horizontal ou vertical
   - `DiagonalXPattern`: X diagonal
   - `LShapePattern`: L em um dos 4 cantos
   - `TShapePattern`: T com 4 rotações possíveis
   - `ClusterPattern`: Crescimento orgânico conectado
   - `CornerPattern`: Bloco compacto em canto
   - `ScatterPattern`: Ilhas dispersas (não conectadas)

3. **PatternWeightConfig** (ScriptableObject):
   - Configura probabilidades de cada padrão
   - Customizável no Inspector (sem recompilar)
   - Pode criar múltiplas configs (Easy/Normal/Hard)

4. **UnlockPatternGenerator** (Factory):
   - Seleciona padrão baseado em pesos
   - Usa seed para determinismo (reproduzibilidade)
   - Encapsula lógica de criação

**Design Decisions:**
- **5 slots iniciais** (não mais 9) para maior desafio
- **Seed-based randomness** ? Runs podem ser reproduzidas com mesmo seed
- **Weighted probabilities** ? Padrões "legais" (Cruz, Linha) têm 15% cada, Cluster tem 25%
- **Extensibilidade** ? Adicionar novo padrão:
  1. Criar classe que implementa `IUnlockPattern`
  2. Adicionar enum em `PatternWeightConfig.PatternType`
  3. Adicionar case no `UnlockPatternGenerator.CreatePattern()`
  4. Configurar peso no Inspector

**Example:**
```
New Run ? Seed: 12345678 ? Generator selects "Cross" ? 5 slots unlocked in + shape
New Run ? Seed: 87654321 ? Generator selects "Cluster" ? 5 slots form organic blob
```

#### Camera System (Pixel Art - SOLID):
**ARCHITECTURE**: Strategy Pattern for flexible, testable camera framing.

**Core Components:**
1. **CameraFramingConfig** (ScriptableObject):
   - Customizable padding per-axis (art composition)
   - Pixel perfect snap settings (multiple of 4 for PPU=32)
   - Debug visualization (grid bounds + camera bounds)

2. **ICameraFitStrategy** (Interface):
   - Calculates required bounds from grid data
   - Testable, extensible without modifying camera code
   - Default implementation: `PaddedGridFitStrategy`

3. **GameCameraController** (MonoBehaviour):
   - Uses strategy to calculate bounds
   - Applies pixel perfect sizing (snap to multiple of 4)
   - No zoom system (simplified per requirements)
   - Smooth panning with pixel snapping

**Design Philosophy:**
- **"World is protagonist"** ? Generous padding for background art/ambiance
- **"Grid is structure"** ? Camera protects grid, doesn't compensate UI
- **"Artistic space"** ? Padding is aesthetic decision, not technical residue
- **"UI as inhabitant"** ? World space UI competes with objects, not treated as HUD

**Pixel Perfect:**
- Converts world units ? pixels
- Rounds UP to next multiple of 4 (no clipping)
- Converts back ? orthographic size
- Ensures zero bleeding/artifacts with PPU=32

**Example Configuration:**
```
Grid: 5×5 (7.5×5.5 world units)
Padding: (3f, 2.5f) horizontal/vertical
Total Bounds: 13.5×10.5 world units
Pixels: 432×336 ? Snapped: 432×336 (already multiple of 4)
Orthographic Size: 5.25
```

## 4. Why these scripts exist?

| Script | Purpose |
| :--- | :--- |
| **`DailyHandSystem.cs`** | Decouples the logic of Drawing/Selling cards from the visual animation of the card. |
| **`CardInteractionBootstrapper.cs`** | Connects the mouse input (Drag/Drop) to the actual Game Logic strategies. Manages RunIdentityContext (immutable) and RunRuntimeContext (scene-dependent). |
| **`InteractionFactory.cs`** | Decides which strategy to run based on the card type (Watering vs Planting). |
| **`ShopService.cs`** | Handles logic for buying items, ensuring money is deducted and items added. |
| **`InteractionPolicy.cs`** | Separates "can I do this?" rules from "how do I do this?" mechanics. |
| **`GridConfiguration.cs`** | Defines grid structure (dimensions, initial state). Changes invalidate old saves via hash versioning. |
| **`RunRuntimeContext.cs`** | Holds scene-dependent services (GridService). Can be updated when scenes load. |
| **`RunIdentityContext.cs`** | Holds immutable run-wide services (Economy, Library, SaveManager). Never changes during a run. |
| **`CameraFramingConfig.cs`** | Defines camera padding and pixel perfect settings. Customizable per-axis for art composition. |
| **`ICameraFitStrategy.cs`** | Strategy Pattern for camera framing. Calculates required bounds without coupling to camera implementation. |
| **`GameCameraController.cs`** | Controls camera positioning and pixel-perfect sizing. Uses Strategy Pattern, no zoom, snap to multiple of 4 (PPU=32). |
| **`IUnlockPattern.cs`** | Interface for grid unlock patterns. Extensible system for different unlock layouts (Cross, Line, X, L, T, Cluster, Corner, Scatter). |
| **`UnlockPatternGenerator.cs`** | Factory for creating unlock patterns. Uses weighted random selection with seed support for determinism. |
| **`PatternWeightConfig.cs`** | ScriptableObject to configure pattern probabilities without code changes. Customizable per level/difficulty. |
| **`IUnlockPattern.cs`** | Interface for grid unlock patterns. Extensible system for different unlock layouts (Cross, Line, X, L, T, Cluster, Corner, Scatter). |
| **`UnlockPatternGenerator.cs`** | Factory for creating unlock patterns. Uses weighted random selection with seed support for determinism. |
| **`PatternWeightConfig.cs`** | ScriptableObject to configure pattern probabilities without code changes. Customizable per level/difficulty. |

