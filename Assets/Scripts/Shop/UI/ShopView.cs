using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class ShopView : UIView
{
    private enum ShopUIState
    {
        Browsing, // Navegando na loja (padrão)
        Selling   // Loja minimizada, selecionando cartas na mão
        // Futuro: Confirming, Inspecting...
    }

    [Header("Estrutura")]
    [SerializeField] private Transform _itemsContainer;
    [SerializeField] private ShopItemView _itemPrefab;
    [SerializeField] private TextMeshProUGUI _shopTitleText;

    [Header("Ações Globais")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _buyButton;
    [SerializeField] private Button _sellButton;

    [Header("Modo Venda")]
    [SerializeField] private CanvasGroup _mainShopGroup;
    [SerializeField] private GameObject _sellModePanel;
    [SerializeField] private Button _cancelSellButton;

    // --- ESTADO & CACHE ---
    private ShopService _shopService;
    private readonly List<ShopItemView> _itemPool = new List<ShopItemView>();

    // Estado Lógico
    private ShopUIState _currentState = ShopUIState.Browsing; 
    private IPurchasable _selectedItem;
    private ShopItemView _selectedView;

    public event Action OnExitRequested;

    protected override void Awake()
    {
        base.Awake();

        if (_buyButton) _buyButton.onClick.AddListener(HandleBuyClick);
        if (_closeButton) _closeButton.onClick.AddListener(HandleCloseClick);

        if (_sellButton)
        {
            _sellButton.interactable = true;
            // Agora chamamos SetState em vez de funções soltas
            _sellButton.onClick.AddListener(() => SetState(ShopUIState.Selling));
        }

        if (_cancelSellButton)
            _cancelSellButton.onClick.AddListener(() => SetState(ShopUIState.Browsing));

        if (_sellModePanel) _sellModePanel.SetActive(false);

        // Estado inicial visual
        _currentState = ShopUIState.Browsing;
        SetState(ShopUIState.Browsing);
    }

    public void Initialize(ShopService shopService)
    {
        if (_shopService != null) _shopService.OnStockRefreshed -= RefreshUI;
        _shopService = shopService;
        if (_shopService != null) _shopService.OnStockRefreshed += RefreshUI;
    }

    private void OnDisable()
    {
        if (_shopService != null) _shopService.OnStockRefreshed -= RefreshUI;

        // Garante reset total ao desligar
        FullReset();
    }

    public override void Show()
    {
        if (_shopService == null)
        {
            Debug.LogError("[ShopView] Erro: Serviço não inicializado.");
            return;
        }
        base.Show();

        // Garante que a loja abra sempre no estado padrão
        SetState(ShopUIState.Browsing);
        RefreshUI();
    }

    public override void Hide()
    {
        base.Hide();
        FullReset();
    }

    // --- STATE MACHINE CORE (O Coração da mudança) ---

    private void SetState(ShopUIState newState)
    {
        if (_currentState == newState) return; // Evita reentrada

        // 1. Saída do estado anterior (Cleanup)
        if (_currentState == ShopUIState.Selling)
        {
            CardView.OnCardClickedGlobal -= HandleCardClickedToSell;
        }

        _currentState = newState;

        // 2. Entrada no novo estado (Setup)
        if (_currentState == ShopUIState.Selling)
        {
            CardView.OnCardClickedGlobal += HandleCardClickedToSell;
        }

        // 3. Atualização Visual
        UpdateVisualState(newState);

        // 4. Atualização de Botões (Ex: desativar comprar se estiver vendendo)
        UpdateButtonsState();
    }

    private void UpdateVisualState(ShopUIState state)
    {
        bool isSelling = (state == ShopUIState.Selling);

        // Configura Painel Principal
        if (_mainShopGroup)
        {
            _mainShopGroup.alpha = isSelling ? 0 : 1;
            _mainShopGroup.blocksRaycasts = !isSelling;
        }

        // Configura Painel de Venda
        if (_sellModePanel) _sellModePanel.SetActive(isSelling);
    }

    private void FullReset()
    {
        // Força volta para Browsing (que limpa eventos de venda)
        if (_sellModePanel) _sellModePanel.SetActive(false);

        SetState(ShopUIState.Browsing);
        DeselectAll();
    }

    // --- LÓGICA DE EVENTOS ---

    private void HandleCardClickedToSell(CardView cardView)
    {
        // Validação explícita de estado
        if (_currentState != ShopUIState.Selling || _shopService == null) return;

        Debug.Log($"[ShopView] Vendendo: {cardView.Data.Name}");
        _shopService.SellCard(cardView.Instance);
    }

    // --- REFRESH E SELEÇÃO ---

    private void RefreshUI()
    {
        // Se estamos vendendo, não reseta o estado para Browsing, 
        // mas precisamos limpar a seleção de compra.
        DeselectAll();

        foreach (var item in _itemPool) item.gameObject.SetActive(false);

        if (_shopTitleText && _shopService != null)
            _shopTitleText.text = _shopService.CurrentShopTitle;

        if (_shopService != null && _shopService.CurrentStock != null)
        {
            var stock = _shopService.CurrentStock;
            for (int i = 0; i < stock.Count; i++)
            {
                ShopItemView itemView = GetItemView(i);
                itemView.Setup(stock[i], HandleItemClicked);
                itemView.gameObject.SetActive(true);
            }
        }
    }

    private void HandleItemClicked(ShopItemView clickedView)
    {
        // Só permite selecionar itens se estivermos no modo Browsing
        if (_currentState != ShopUIState.Browsing) return;

        if (_selectedView != null) _selectedView.SetSelected(false);

        _selectedView = clickedView;
        _selectedItem = clickedView.Data;

        if (_selectedView != null) _selectedView.SetSelected(true);

        UpdateButtonsState();
    }

    private void DeselectAll()
    {
        if (_selectedView != null) _selectedView.SetSelected(false);
        _selectedItem = null;
        _selectedView = null;
        UpdateButtonsState();
    }

    private void UpdateButtonsState()
    {
        // Só pode comprar se estiver navegando E tiver item
        bool canBuy = (_currentState == ShopUIState.Browsing) && (_selectedItem != null);

        if (_buyButton) _buyButton.interactable = canBuy;

        // Só pode ir para vender se estiver navegando
        if (_sellButton) _sellButton.interactable = (_currentState == ShopUIState.Browsing);
    }

    private void HandleBuyClick()
    {
        if (_currentState == ShopUIState.Browsing && _selectedItem != null && _shopService != null)
        {
            _shopService.TryPurchase(_selectedItem);
        }
    }

    private void HandleCloseClick() => OnExitRequested?.Invoke();

    private ShopItemView GetItemView(int index)
    {
        if (index < _itemPool.Count) return _itemPool[index];
        var newItem = Instantiate(_itemPrefab, _itemsContainer);
        _itemPool.Add(newItem);
        return newItem;
    }
}