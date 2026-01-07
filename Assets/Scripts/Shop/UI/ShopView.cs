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
    private ShopService _shopService;
    private List<ShopItemView> _itemPool = new List<ShopItemView>();

    // Estado da Seleção Atual
    private IPurchasable _selectedItem;
    private ShopItemView _selectedView;

    public event Action OnExitRequested;

    protected override void Awake()
    {
        base.Awake();

        if (_closeButton) _closeButton.onClick.AddListener(HandleCloseClick);
        if (_buyButton) _buyButton.onClick.AddListener(HandleBuyClick);
        if (_sellButton) _sellButton.onClick.AddListener(HandleSellClick);
    }

    private void Start()
    {
        if (AppCore.Instance != null) _shopService = AppCore.Instance.ShopService;
    }

    private void OnEnable()
    {
        if (AppCore.Instance != null)
            AppCore.Instance.ShopService.OnStockRefreshed += RefreshUI;
    }

    private void OnDisable()
    {
        if (AppCore.Instance != null)
            AppCore.Instance.ShopService.OnStockRefreshed -= RefreshUI;
    }

    public override void Show()
    {
        base.Show();
        if (_shopService == null && AppCore.Instance != null)
            _shopService = AppCore.Instance.ShopService;

        RefreshUI();
    }

    private void RefreshUI()
    {
        // Reseta seleção ao abrir ou atualizar estoque
        DeselectAll();

        if (_shopService == null) return;

        // Limpeza (Pooling)
        foreach (var item in _itemPool) item.gameObject.SetActive(false);

        if (_shopTitleText) _shopTitleText.text = _shopService.CurrentShopTitle;

        var stock = _shopService.CurrentStock;
        if (stock != null)
        {
            for (int i = 0; i < stock.Count; i++)
            {
                ShopItemView itemView = GetItemView(i);

                // Passa o método HandleItemSelected como callback
                itemView.Setup(stock[i], HandleItemSelected);
                itemView.gameObject.SetActive(true);
            }
        }
    }

    // --- LÓGICA DE SELEÇÃO ---

    private void HandleItemSelected(ShopItemView view, IPurchasable item)
    {
        // 1. Remove destaque do anterior
        if (_selectedView != null) _selectedView.SetSelected(false);

        // 2. Atualiza estado
        _selectedItem = item;
        _selectedView = view;

        // 3. Destaca novo
        if (_selectedView != null) _selectedView.SetSelected(true);

        // 4. Habilita o botão de comprar
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
            // Só pode clicar em comprar se tiver algo selecionado
            _buyButton.interactable = (_selectedItem != null);
        }

        // Lógica do botão vender (Implementar futuramente)
        if (_sellButton)
        {
            _sellButton.interactable = true; // Ou lógica específica de venda
        }
    }

    // --- AÇÕES ---

    private void HandleBuyClick()
    {
        if (_selectedItem != null && _shopService != null)
        {
            _shopService.TryPurchase(_selectedItem);
            // Se a compra for sucesso, o evento OnStockRefreshed vai rodar o RefreshUI e limpar a seleção
        }
    }

    private void HandleSellClick()
    {
        Debug.Log("Lógica de Venda Global ainda não implementada.");
        // Futuro: Abrir um painel lateral de venda ou mudar o modo de interação
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