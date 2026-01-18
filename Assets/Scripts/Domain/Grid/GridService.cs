using UnityEngine;
using System;

public class GridService : IGridService
{
    private readonly RunData _runData;
    private readonly IGameLibrary _library;
    private readonly GameStateManager _gameStateManager;
    private readonly GridConfiguration _config;

    public event Action<int> OnSlotStateChanged;
    public event Action OnDataDirty;
    public event Action<int, GridEventType> OnSlotUpdated;

    public int SlotCount => _runData?.GridSlots?.Length ?? 0;
    public GridConfiguration Config => _config;

    public GridService(RunData runData, IGameLibrary library, GameStateManager gameStateManager, GridConfiguration config)
    {
        _runData = runData;
        _library = library;
        _gameStateManager = gameStateManager;
        _config = config;

        // Validação vital
        if (_config == null)
        {
            Debug.LogError("[GridService] Configuração de Grid é NULA! Usando fallback perigoso.");
            // Criamos uma config dummy em runtime se necessário, ou lançamos exceção
        }

        EnsureGridInitialized();
    }


    public IReadOnlyCropState GetSlotReadOnly(int index)
    {
        if (IsValidIndex(index)) return _runData.GridSlots[index];
        return new CropState();
    }

    public CropState GetSlot(int index)
    {
         if (IsValidIndex(index)) return _runData.GridSlots[index];
         return null;
    }

    public GridSlotState GetSlotStateReadOnly(int index)
    {
        if (IsValidIndex(index))
        {
             if (_runData.SlotStates[index] == null) _runData.SlotStates[index] = new GridSlotState(false);
             return _runData.SlotStates[index];
        }
        return new GridSlotState(false);
    }


    // --- CORREÇÃO 1: Usando o nome correto e a lógica correta ---
    public bool CanReceiveCard(int index, CardData card)
    {
        if (!IsValidIndex(index)) return false;
        if (card == null) return false;

        // Regra de Bloqueio
        bool isUnlocked = IsSlotUnlocked(index);
        
        // Se bloqueado, só carta de Expansion pode interagir (para desbloquear)
        if (!isUnlocked && card.Type != CardType.Expansion) return false;

        var strategy = InteractionFactory.GetStrategy(card.Type);
        if (strategy == null) return false;

        return strategy.CanInteract(index, this, card);
    }

    public InteractionResult ApplyCard(int index, CardData card)
    {
        // 1. PROTEÇÃO DE ESTADO
        if (_gameStateManager.CurrentState != GameState.Playing)
        {
            return InteractionResult.Fail("Ação bloqueada: O jogo não está em fase de produção.");
        }

        // 2. Validações Padrão
        if (!IsValidIndex(index)) return InteractionResult.Fail("Slot inválido.");

        // 3. Estratégia de Interação
        ICardInteractionStrategy strategy = GetStrategyForCard(card);
        if (strategy == null) return InteractionResult.Fail("Carta sem efeito definido.");

        if (!strategy.CanInteract(index, this, card)) return InteractionResult.Fail("Interação inválida neste slot.");

        // 4. Execução
        var result = strategy.Execute(index, this, card);

        if (result.IsSuccess)
        {
            OnSlotStateChanged?.Invoke(index);

            OnSlotUpdated?.Invoke(index, result.EventType);
            OnDataDirty?.Invoke();
        }

        return result;
    }
    public float GetGridContaminationPercentage()
    {
        if (_runData.GridSlots == null || _runData.GridSlots.Length == 0) return 0f;

        int totalSlots = _runData.GridSlots.Length;
        int contaminatedSlots = 0;

        foreach (var slot in _runData.GridSlots)
        {
            // Contaminação = Planta Morta (Futuro: + Pragas)
            if (slot.IsWithered)
            {
                contaminatedSlots++;
            }
        }

        return (float)contaminatedSlots / totalSlots;
    }

    public void ProcessNightCycleForSlot(int index)
    {
        if (!IsValidIndex(index)) return;

        // ⭐ EARLY EXIT: Slots bloqueados não são processados
        // RAZÃO: 
        // - Bloqueados não participam da meta/score
        // - Bloqueados nunca têm plantas (estado garantido vazio)
        // - Bloqueados não podem ser molhados (sem estado residual)
        // - Evita processamento desnecessário e eventos inválidos
        if (!IsSlotUnlocked(index)) return;

        var slot = _runData.GridSlots[index];
        bool wasWatered = slot.IsWatered;

        // 1. Seca a terra
        slot.IsWatered = false;

        GridEventType eventToEmit = GridEventType.GenericUpdate;
        bool visualUpdateNeeded = wasWatered;

        if (wasWatered) eventToEmit = GridEventType.DryOut;

        // 2. Processa Biologia
        if (!slot.IsEmpty && _library.TryGetCrop(slot.CropID, out var data))
        {
            var result = CropLogic.ProcessNightlyGrowth(slot, data);

            if (result.EventType != GrowthEventType.None)
            {
                visualUpdateNeeded = true;
                switch (result.EventType)
                {
                    case GrowthEventType.Matured:
                        eventToEmit = GridEventType.Matured;
                        break;
                    case GrowthEventType.WitheredByAge:
                        eventToEmit = GridEventType.Withered;
                        break;
                    default:
                        if (eventToEmit == GridEventType.GenericUpdate)
                            eventToEmit = GridEventType.GenericUpdate;
                        break;
                }
            }
        }

        if (visualUpdateNeeded)
        {
            OnSlotStateChanged?.Invoke(index);
            OnSlotUpdated?.Invoke(index, eventToEmit);
            OnDataDirty?.Invoke();
        }
    }

    public bool IsSlotUnlocked(int index)
    {
        if (!IsValidIndex(index)) return false;
        if (_runData.SlotStates[index] == null) _runData.SlotStates[index] = new GridSlotState(false);
        return _runData.SlotStates[index].IsUnlocked;
    }

    public bool CanUnlockSlot(int index)
    {
        if (!IsValidIndex(index)) return false;
        if (IsSlotUnlocked(index)) return false;
        return IsAdjacentToUnlocked(index);
    }

    public bool TryUnlockSlot(int index)

    {
        if (!IsValidIndex(index)) return false;
        if (IsSlotUnlocked(index)) return false; 

        if (IsAdjacentToUnlocked(index))
        {
            _runData.SlotStates[index].IsUnlocked = true;
            OnSlotStateChanged?.Invoke(index);
            OnDataDirty?.Invoke();
            return true;
        }
        return false;
    }

    private bool IsAdjacentToUnlocked(int index)
    {
        if (_config == null) return false;

        int width = _config.Columns;
        int height = _config.Rows;
        
        // Verifica consistência
        if (width * height != SlotCount) 
        {
             // Fallback para grid quadrado se a config não bater com dados (ex: migração incompleta)
             width = (int)Mathf.Sqrt(SlotCount);
        }

        int r = index / width;
        int c = index % width;

        int[] dr = {-1, 1, 0, 0};
        int[] dc = {0, 0, -1, 1};

        for(int i=0; i<4; i++)
        {
            int nr = r + dr[i];
            int nc = c + dc[i];
            if (nr >= 0 && nr < height && nc >= 0 && nc < width)
            {
                int nIndex = nr * width + nc;
                if (IsSlotUnlocked(nIndex)) return true;
            }
        }
        return false;
    }

    private void EnsureGridInitialized()
    {
        if (_config == null) return;

        int targetSize = _config.TotalSlots;

        // 1. Migração de Tamanho
        if (_runData.GridSlots == null || _runData.GridSlots.Length != targetSize)
        {
            Debug.Log($"[GridService] Ajustando Grid para {targetSize} slots ({_config.Columns}x{_config.Rows})...");
            var oldSlots = _runData.GridSlots ?? new CropState[0];
            _runData.GridSlots = new CropState[targetSize];
            
            for(int i=0; i<_runData.GridSlots.Length; i++)
            {
                if (i < oldSlots.Length) 
                    _runData.GridSlots[i] = oldSlots[i];
                else 
                    _runData.GridSlots[i] = new CropState();
            }
        }

        // 2. Migração de SlotStates
        if (_runData.SlotStates == null || _runData.SlotStates.Length != targetSize)
        {
             var oldStates = _runData.SlotStates ?? new GridSlotState[0];
             _runData.SlotStates = new GridSlotState[targetSize];

             for(int i=0; i<_runData.SlotStates.Length; i++)
             {
                 if (i < oldStates.Length)
                     _runData.SlotStates[i] = oldStates[i];
                 else
                     _runData.SlotStates[i] = new GridSlotState(false);
             }
        }

        // 3. Inicialização de Gameplay (Desbloqueio Inicial)
        // Só roda se NENHUM slot estiver desbloqueado (Run nova ou zerada)
        bool hasAnyUnlocked = false;
        foreach(var s in _runData.SlotStates) if (s != null && s.IsUnlocked) hasAnyUnlocked = true;

        if (!hasAnyUnlocked && _config.DefaultUnlockedCoordinates != null)
        {
            foreach(var coord in _config.DefaultUnlockedCoordinates)
            {
                // Coords do ScriptableObject são X,Y (Col, Row). Convertemos para Index.
                // Mas cuidado: SO usa 1-based ou 0-based? O arquivo que criei parecia usar 1-based nos comentários mas valores 1,2,3 para 5x5 center.
                // Vamos assumir 0-based no código interno para facilitar (0..4).
                // Revisando o arquivo criado: "3x3 center... indices 1,2,3". Isso é 1-based relative to 0? Or 0-based indices 1,2,3 corresponds do 2nd, 3rd, 4th col. 
                // Se Columns=5, indices são 0,1,2,3,4. Center 3x3 é 1,2,3. Isso bate.
                
                int c = coord.x;
                int r = coord.y;
                
                if (c >= 0 && c < _config.Columns && r >= 0 && r < _config.Rows)
                {
                    int index = r * _config.Columns + c;
                    if (IsValidIndex(index))
                    {
                        _runData.SlotStates[index].IsUnlocked = true;
                    }
                }
            }
        }
    }

    private ICardInteractionStrategy GetStrategyForCard(CardData card)

    {
        if (card == null) return null;
        return InteractionFactory.GetStrategy(card.Type);
    }

    // --- HELPER UNIFICADO ---
    // Retorna TRUE se o índice for BOM.
    private bool IsValidIndex(int index) => index >= 0 && index < _runData.GridSlots.Length;
}