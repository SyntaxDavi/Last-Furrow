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

    [Header("Modo Venda")]
    [SerializeField] private CanvasGroup _mainShopGroup; // O painel principal da loja (Cards, Botões)
    [SerializeField] private GameObject _sellModePanel;  // Painel pequeno: "Selecione carta / Cancelar"
    [SerializeField] private Button _cancelSellButton;

    private bool _isSellMode = false;

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
        base.Awake(); // Configura CanvasGroup do pai

        // Configura Botão Comprar
        if (_buyButton)
            _buyButton.onClick.AddListener(HandleBuyClick);

        // Configura Botão Fechar
        if (_closeButton)
            _closeButton.onClick.AddListener(HandleCloseClick);

        // Configura Botão Vender 
        if (_sellButton)
        {
            _sellButton.interactable = true; // Garante que começa ativo
            _sellButton.onClick.AddListener(EnterSellMode); // Chama o modo minimizar
        }

        // Configura Botão Cancelar Venda (Voltar pra loja)
        if (_cancelSellButton)
            _cancelSellButton.onClick.AddListener(ExitSellMode);

        // Garante que o painelzinho de "Selecione carta" comece escondido
        if (_sellModePanel)
            _sellModePanel.SetActive(false);
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
    private void OnDisable()
    {
        if (AppCore.Instance != null && AppCore.Instance.ShopService != null)
            AppCore.Instance.ShopService.OnStockRefreshed -= RefreshUI;

        // 2. LIMPEZA DO MODO VENDA (O Fix)
        // Garante que paramos de escutar cliques nas cartas
        CardView.OnCardClickedGlobal -= HandleCardClickedToSell;

        // Reseta a flag lógica
        _isSellMode = false;

        // Esconde a barra de "Escolha sua carta" na marra
        if (_sellModePanel) _sellModePanel.SetActive(false);

        // Reseta a opacidade da loja principal para a próxima vez que abrir
        if (_mainShopGroup)
        {
            _mainShopGroup.alpha = 1;
            _mainShopGroup.blocksRaycasts = true;
        }
        ExitSellMode();
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
    public override void Hide()
    {
        base.Hide(); 

        // FORÇA A SAÍDA DO MODO VENDA
        if (_isSellMode)
        {
            ExitSellMode();
        }

        // Garante que o painelzinho suma mesmo se não estivesse no modo venda (segurança)
        if (_sellModePanel) _sellModePanel.SetActive(false);
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
    private void EnterSellMode()
    {
        _isSellMode = true;

        // 1. Esconde a Loja (Fade Out rápido) but keep ShopView active
        // Usamos o CanvasGroup interno do painel principal, não o da Window inteira
        if (_mainShopGroup)
        {
            _mainShopGroup.alpha = 0;
            _mainShopGroup.blocksRaycasts = false;
        }

        // 2. Mostra barra de instrução
        if (_sellModePanel) _sellModePanel.SetActive(true);

        // 3. Escuta cliques nas cartas
        CardView.OnCardClickedGlobal += HandleCardClickedToSell;
    }
    private void ExitSellMode()
    {
        _isSellMode = false;

        // 1. Mostra a Loja
        if (_mainShopGroup)
        {
            _mainShopGroup.alpha = 1;
            _mainShopGroup.blocksRaycasts = true;
        }

        // 2. Esconde instrução
        if (_sellModePanel) _sellModePanel.SetActive(false);

        // 3. Para de escutar
        CardView.OnCardClickedGlobal -= HandleCardClickedToSell;
    }

    private void HandleCardClickedToSell(CardView cardView)
    {
        // DEBUG NOVO
        Debug.Log($"[ShopView] Recebeu clique na carta: {cardView.Data.Name}. Modo Venda: {_isSellMode}");

        if (!_isSellMode) return;

        // Lógica de Venda
        _shopService.SellCard(cardView.Instance);
    }
    private void HandleBuyClick()
    {
        if (_selectedItem != null && _shopService != null)
        {
            _shopService.TryPurchase(_selectedItem);
        }
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