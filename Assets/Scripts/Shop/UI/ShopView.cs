using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class ShopView : UIView
{
    [Header("Estrutura")]
    [SerializeField] private Transform _itemsContainer;
    [SerializeField] private ShopItemView _itemPrefab;
    [SerializeField] private TextMeshProUGUI _shopTitleText;

    [Header("Ações Globais")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _buyButton;
    [SerializeField] private Button _sellButton;

    // --- ESTADO & CACHE ---
    // Injeção de dependência: A View não busca o serviço, ela RECEBE o serviço.
    private ShopService _shopService;

    // Readonly garante que a lista nunca seja substituída, apenas limpa/populada
    private readonly List<ShopItemView> _itemPool = new List<ShopItemView>();

    // Estado da Seleção Atual
    private IPurchasable _selectedItem;
    private ShopItemView _selectedView;

    public event Action OnExitRequested;

    protected override void Awake()
    {
        base.Awake();

        if (_closeButton) _closeButton.onClick.AddListener(HandleCloseClick);
        if (_buyButton) _buyButton.onClick.AddListener(HandleBuyClick);
        if (_sellButton)
        {
            _sellButton.onClick.AddListener(HandleSellClick);
            _sellButton.interactable = false; 
        }
    }

    /// <summary>
    /// Configura a View com suas dependências.
    /// Deve ser chamado pelo UIManager ou Bootstrapper.
    /// </summary>
    public void Initialize(ShopService shopService)
    {
        // Se já tinha um serviço antes, remove os listeners antigos para não duplicar
        if (_shopService != null)
        {
            _shopService.OnStockRefreshed -= RefreshUI;
        }

        _shopService = shopService;

        if (_shopService != null)
        {
            _shopService.OnStockRefreshed += RefreshUI;
        }
    }

    private void OnDestroy()
    {
        if (_shopService != null)
        {
            _shopService.OnStockRefreshed -= RefreshUI;
        }
    }

    public override void Show()
    {
        if (_shopService == null)
        {
            Debug.LogError("[ShopView] Tentativa de abrir loja sem inicializar o serviço!");
            return;
        }

        base.Show();
        RefreshUI();
    }

    private void RefreshUI()
    {
        DeselectAll();

        // Limpeza (Pooling)
        foreach (var item in _itemPool) item.gameObject.SetActive(false);

        if (_shopTitleText) _shopTitleText.text = _shopService.CurrentShopTitle;

        var stock = _shopService.CurrentStock;
        if (stock != null)
        {
            for (int i = 0; i < stock.Count; i++)
            {
                ShopItemView itemView = GetItemView(i);
                itemView.Setup(stock[i], HandleItemClicked);
                itemView.gameObject.SetActive(true);
            }
        }
    }

    // --- LÓGICA DE SELEÇÃO ---

    private void HandleItemClicked(ShopItemView clickedView)
    {
        if (_selectedView != null) _selectedView.SetSelected(false);

        _selectedView = clickedView;
        _selectedItem = clickedView.Data;

        if (_selectedView != null) _selectedView.SetSelected(true);

        UpdateButtonsState();
    }

    private void DeselectAll()
    {
        _selectedItem = null;
        if (_selectedView != null) _selectedView.SetSelected(false);
        _selectedView = null;
        UpdateButtonsState();
    }

    private void UpdateButtonsState()
    {
        if (_buyButton)
        {
            _buyButton.interactable = (_selectedItem != null);
        }

        // Futuro: Lógica de venda
    }

    // --- AÇÕES ---

    private void HandleBuyClick()
    {
        if (_selectedItem != null && _shopService != null)
        {
            _shopService.TryPurchase(_selectedItem);
        }
    }

    private void HandleSellClick()
    {
        Debug.Log("Funcionalidade de Venda em desenvolvimento.");
    }

    private void HandleCloseClick()
    {
        OnExitRequested?.Invoke();
    }

    // --- POOLING ---
    private ShopItemView GetItemView(int index)
    {
        if (index < _itemPool.Count) return _itemPool[index];
        var newItem = Instantiate(_itemPrefab, _itemsContainer);
        _itemPool.Add(newItem);
        return newItem;
    }
}