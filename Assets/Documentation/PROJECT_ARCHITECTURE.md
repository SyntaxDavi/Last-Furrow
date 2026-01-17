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
*   **`GridService.cs`**: Pure C# logic for the 3x3 farming grid. Handles slot states (Empty, Planted, Watered).
*   **`DailyHandSystem.cs`**: Logic for drawing cards each day. Handles "Overflow" (selling cards if hand is full).

### C. Data (State)
*   **`RunData.cs`**: The serializable state of the current game (Money, Day, Hand, GridSlots).
*   **`CardData.cs`**: ScriptableObject defining a card type (Name, Cost, Effect).
*   **`SaveManager.cs`**: Handles saving/loading `RunData` to disk/PlayerPrefs.

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

## 4. Why these scripts exist?

| Script | Purpose |
| :--- | :--- |
| **`DailyHandSystem.cs`** | Decouples the logic of Drawing/Selling cards from the visual animation of the card. |
| **`CardInteractionBootstrapper.cs`** | Connects the mouse input (Drag/Drop) to the actual Game Logic strategies. |
| **`InteractionFactory.cs`** | Decides which strategy to run based on the card type (Watering vs Planting). |
| **`ShopService.cs`** | Handles logic for buying items, ensuring money is deducted and items added. |
| **`InteractionPolicy.cs`** | Separates "can I do this?" rules from "how do I do this?" mechanics. |
